using ButtplugManaged;
using ButtplugSong.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class PlugManager
{
    public enum PlugManagerStatus
    {
        //stable
        Uninitialized = 0,
        ClientSetUp = 1, //initialized, not connected to server
        ConnectedToServer = 2, //connected, not scanned
        ScannedNoDevices = 3, //scanned, no devices
        DeviceConnected = 4,

        //in progress
        Initializing = 10,
        SettingUpClient = 11,
        ConnectingToServer = 12,
        ScanningForDevices = 13,

        //stopped
        ShutDown = 100
    }

    private PlugManagerStatus _status = PlugManagerStatus.Uninitialized;
    private PlugManagerStatus Status
    {
        get => _status;
        set
        {
            //if (_status != value) Log($"Status change: {_status} -> {value}");
            _status = value;
        }
    }
    public static event Action<ButtplugClientDevice>? DeviceConnected;
    public static event Action<ButtplugClientDevice>? DeviceDisconnected;
    public static event Action<float, bool>? UpdateDevicePower;

    //ButtplugManaged
    public ButtplugClient Client { get; private set; }
    private ButtplugWebsocketConnectorOptions _connector;

    //Network settings
    public string ServerAddress { get; private set; }
    public int Port { get; private set; }
    public int RetryAttempts { get; private set; }

    //current manager settings
    internal float currentPower = 0;
    internal bool rotationEnabled = false;
    internal bool rotateClockwise = true; //anticlockwise if false

    public PlugManager(Action<string>? logger = null, string serverAddress = "localhost", int port = 12345, int retryAttempts = 5)
    {
        _logger = logger;
        ServerAddress = serverAddress;
        Port = port;
        RetryAttempts = retryAttempts;
    }
    private Action<string>? _logger;
    private void Log(string s) => _logger?.Invoke(s);

    internal bool allowedToInitialize = true;
    public async Task<bool> Initialize()
    {
        if (Status == PlugManagerStatus.ShutDown || !allowedToInitialize) return false;
        allowedToInitialize = false;

        Status = PlugManagerStatus.Initializing;

        _connector = new ButtplugWebsocketConnectorOptions(new Uri($"ws://{ServerAddress}:{Port}/buttplug"));
        SetupClient();

        var success = await TryConnect();
        if (!success)
        {
            Log("Could not connect to the server.");
            return false;
        }

        return true;
    }
    internal void ShutDown()
    {
        Status = PlugManagerStatus.ShutDown;
        Disconnect();
    }
    internal void Disconnect()
    {
        if (Client is null) return;
        Log("Disconnecting old client.");
        Client.DeviceAdded -= OnDeviceAdded;
        Client.DeviceRemoved -= OnDeviceRemoved;
        Client.ServerDisconnect -= ClientOnServerDisconnect;
        Client.ErrorReceived -= ClientOnErrorReceived;
        Client.PingTimeout -= ClientOnPingTimeout;

        Client.DisconnectAsync();
        Client = null;
    }
    private void SetupClient()
    {
        if (Status == PlugManagerStatus.ShutDown) return;
        Status = PlugManagerStatus.SettingUpClient;
        if (Client != null)
        {
            Log("Existing client detected.");
            Disconnect();
        }
        Client = new ButtplugClient("Plug Control");
        Client.DeviceAdded += OnDeviceAdded;
        Client.DeviceRemoved += OnDeviceRemoved;
        Client.ServerDisconnect += ClientOnServerDisconnect;
        Client.ErrorReceived += ClientOnErrorReceived;
        Client.PingTimeout += ClientOnPingTimeout;
        Log($"Client setup complete.");
        //_triedToInitialize = false;
        allowedToInitialize = true;
        Status = PlugManagerStatus.ClientSetUp;
    }
    private async Task<bool> TryConnect()
    {
        if (Status == PlugManagerStatus.ShutDown) return false;
        if (Status == PlugManagerStatus.Uninitialized) SetupClient();
        Status = PlugManagerStatus.ConnectingToServer;
        try
        {
            Log("Connecting to the server...");
            await Client.ConnectAsync(_connector);
            if (Status != PlugManagerStatus.DeviceConnected) Status = PlugManagerStatus.ConnectedToServer;
            Log("Connected to server.");
            _tryingToReconnect = false;
            allowedToInitialize = true;
            return await TryScanning();
        }
        catch (ButtplugConnectorException e)
        {
            Log($"Could not connect to the server: {e.InnerException?.Message}");
        }
        catch (ButtplugHandshakeException e)
        {
            Log($"There was an error performing the handshake with the server: {e.InnerException?.Message}");
        }
        catch (SocketException e)
        {
            Log($"Target machine refused the connection. {e.InnerException?.Message}");
        }
        //if it reaches here, it failed to connect.
        Status = PlugManagerStatus.ClientSetUp;
        return false;
    }
    private DateTime? _scanStartTime = null;
    async Task<bool> TryScanning()
    {
        if (Status == PlugManagerStatus.ShutDown) return false;
        if (Status != PlugManagerStatus.DeviceConnected) Status = PlugManagerStatus.ScanningForDevices;
        Log("Starting to scan for devices.");
        try
        {
            await Client.StartScanningAsync();
            allowedToInitialize = true;
            _scanStartTime = DateTime.UtcNow;
        }
        catch (ButtplugException ex)
        {
            Log($"Failed to start scanning for devices: {ex.InnerException?.Message}");
            Status = PlugManagerStatus.ConnectedToServer;
            return false;
        }
        catch (SocketException ex)
        {
            Log($"Target machine refused the connection. {ex.InnerException?.Message}");
            Status = PlugManagerStatus.ConnectedToServer;
            return false;
        }
        return true;
    }
    private void OnDeviceAdded(object sender, DeviceAddedEventArgs e)
    {
        Log($"Device Connected: {e.Device.Name}");
        Status = PlugManagerStatus.DeviceConnected;
        DeviceConnected?.Invoke(e.Device);
    }

    private void OnDeviceRemoved(object sender, DeviceRemovedEventArgs e)
    {
        Log($"Device Disconnected: {e.Device.Name}");
        if (!GetDevices().Any()) Status = PlugManagerStatus.ConnectedToServer;
        DeviceDisconnected?.Invoke(e.Device);
    }

    private void ClientOnErrorReceived(object sender, ButtplugExceptionEventArgs e)
    {
        Log($"Received an error: {e.Exception.Message}");
    }

    private void ClientOnPingTimeout(object sender, EventArgs e)
    {
        Log("Server ping timed out.");
    }

    bool _tryingToReconnect = false;
    private async void ClientOnServerDisconnect(object sender, EventArgs e)
    {
        if (Status == PlugManagerStatus.ShutDown) return;
        if (_tryingToReconnect) return;
        Log("Disconnected from server.");
        _tryingToReconnect = true;
        for (int _retries = 1; _retries <= RetryAttempts; _retries++)
        {
            Log($"Reconnecting... (Attempt {_retries} of {RetryAttempts})");
            SetupClient();
            bool success = await TryConnect();
            if (success) return;
            Log("Trying again in 5 seconds.");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        _tryingToReconnect = false;
        Log("Could not reconnect to server.");
    }
    private async Task EnsureClientExists()
    {
        if ((int)Status >= 10)
        {
            if (Status == PlugManagerStatus.ShutDown) return;
            if (Status == PlugManagerStatus.ScanningForDevices)
            {
                if (_scanStartTime.HasValue && DateTime.UtcNow - _scanStartTime.Value > TimeSpan.FromSeconds(10))
                {
                    await Initialize(); //Connection dropped, start over
                }
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            if ((int)Status >= 10) return;
        }
        if (Client == null)
        {
            Log($"Intiface Client is null. Trying to setup.");
            if (!allowedToInitialize) SetupClient();
            if (!await Initialize()) return;
        }
        if (!Client!.Connected)
        {
            Log($"Intiface Client is disconnected - Is the server running?");
            if (!await TryConnect())
            {
                Log($"Failed to connect. Resetting client to reinitialize");
                SetupClient();
                if (await Initialize()) Log("Reinitialized!");
                else return;
            }
            else Log("Reconnected!");
        }
        else if (Client.Devices.Length == 0)
        {
            Log($"Intiface Client connected, but no devices are connected.");
            if (Status == PlugManagerStatus.ConnectedToServer) await TryScanning();
        }
    }
    private async Task UpdatePowerLevels(bool routineUpdate)
    {
        await EnsureClientExists();
        if (UpdateDevicePower != null)
        {
            UpdateDevicePower.Invoke(currentPower, routineUpdate);
            return;
        }
        //only continue here if not handled by UpdateDevicePower
        foreach (ButtplugClientDevice plug in GetDevices())
        {
            //if (_duplicateUpdates == 0) Log($"Setting power on {plug.Name} to {currentPower * 100}%");
            await plug.SendVibrateCmd(currentPower);
            if (rotationEnabled)
            {
                if (!routineUpdate) rotateClockwise = !rotateClockwise; //if this is a fresh (non duplicate) signal, swap direction.
                try
                { //try adding rotation
                    plug?.SendRotateCmd(currentPower, rotateClockwise);
                }
                catch { }
            }
        }
    }
    public IEnumerable<ButtplugClientDevice> GetDevices()
    {
        if (Client is not null && Client.Devices is not null)
        {
            foreach (ButtplugClientDevice? plug in Client.Devices)
            {
                if (plug is not null) yield return plug;
            }
        }
    }
    private int _duplicateUpdates;
    public void SetPowerLevel(float level, bool routineUpdate)
    {
        if (level == currentPower) _duplicateUpdates++;
        else _duplicateUpdates = 0;
        if (_duplicateUpdates >= 3) return; //allow for some duplicate updates as sometimes commands do get lost.

        currentPower = level.Clamp(0, 1);
        //UpdatePowerLevels();
        Task.Factory.StartNew(() => UpdatePowerLevels(routineUpdate).FireAndForget(Log));
    }
    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }
}