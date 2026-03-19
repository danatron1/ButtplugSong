using ButtplugSong.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class PlugManager
{
    public enum PlugManagerStatus
    {
        Uninitialized = 0,
        ClientSetUp = 1,
        ConnectedToServer = 2,
        ScannedNoDevices = 3,
        DeviceConnected = 4,

        Initializing = 10,
        SettingUpClient = 11,
        ConnectingToServer = 12,
        ScanningForDevices = 13,

        ShutDown = 100
    }

    private PlugManagerStatus _status = PlugManagerStatus.Uninitialized;
    private PlugManagerStatus Status
    {
        get => _status;
        set => _status = value;
    }

    public static event Action<ButtplugDevice>? DeviceConnected;
    public static event Action<ButtplugDevice>? DeviceDisconnected;
    public static event Action? DisconnectedFromServer;
    public static event Action<float, bool>? UpdateDevicePower;

    public ButtplugRawClient Client { get; private set; }

    public string ServerAddress { get; private set; }
    public int Port { get; private set; }
    public int RetryAttempts { get; private set; }
    private int tryConnectAttempts = 0;

    internal float currentPower = 0;
    internal bool rotationEnabled = false;
    internal bool rotateClockwise = true;

    public PlugManager(Action<string> logger = null, string serverAddress = "localhost", int port = 12345, int retryAttempts = 5)
    {
        _logger = logger;
        ServerAddress = serverAddress;
        Port = port;
        RetryAttempts = retryAttempts;
    }

    private Action<string> _logger;
    private void Log(string s) => _logger?.Invoke(s);

    internal bool allowedToInitialize = true;
    public async Task<bool> Initialize()
    {
        if (Status == PlugManagerStatus.ShutDown || !allowedToInitialize) return false;
        allowedToInitialize = false;

        Status = PlugManagerStatus.Initializing;

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
        if (Client == null) return;
        Log("Disconnecting old client.");
        Client.DeviceAdded -= OnDeviceAdded;
        Client.DeviceRemoved -= OnDeviceRemoved;
        Client.ServerDisconnect -= ClientOnServerDisconnect;
        Client.ErrorReceived -= ClientOnErrorReceived;

        try { Client.Disconnect(); }
        catch { }
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
        Client = new ButtplugRawClient(Log);
        Client.DeviceAdded += OnDeviceAdded;
        Client.DeviceRemoved += OnDeviceRemoved;
        Client.ServerDisconnect += ClientOnServerDisconnect;
        Client.ErrorReceived += ClientOnErrorReceived;
        Log("Client setup complete.");
        allowedToInitialize = true;
        Status = PlugManagerStatus.ClientSetUp;
    }

    private async Task<bool> TryConnect()
    {
        if (Status == PlugManagerStatus.ShutDown) return false;
        if (Status == PlugManagerStatus.Uninitialized) SetupClient();
        if (tryConnectAttempts >= RetryAttempts) return false; //retry attempt limit reached. 
        tryConnectAttempts++;
        Status = PlugManagerStatus.ConnectingToServer;
        try
        {
            Log("Connecting to the server...");
            await Client.ConnectAsync(ServerAddress, Port);
            if (Status != PlugManagerStatus.DeviceConnected) Status = PlugManagerStatus.ConnectedToServer;
            Log("Connected to server.");
            _tryingToReconnect = false;
            allowedToInitialize = true;
            return await TryScanning();
        }
        catch (TimeoutException)
        {
            Log("Connection timed out.");
        }
        catch (Exception e)
        {
            Log($"Could not connect to the server: {e.Message}");
        }
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
            Client.StartScanning();
            Client.RequestDeviceList();
            allowedToInitialize = true;
            _scanStartTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Log($"Failed to start scanning for devices: {ex.Message}");
            Status = PlugManagerStatus.ConnectedToServer;
            return false;
        }
        return true;
    }

    private void OnDeviceAdded(ButtplugDevice device)
    {
        Log($"Device Connected: {device.Name}");
        Status = PlugManagerStatus.DeviceConnected;
        DeviceConnected?.Invoke(device);
    }

    private void OnDeviceRemoved(ButtplugDevice device)
    {
        Log($"Device Disconnected: {device.Name}");
        if (!GetDevices().Any()) Status = PlugManagerStatus.ConnectedToServer;
        DeviceDisconnected?.Invoke(device);
    }

    private void ClientOnErrorReceived(string error)
    {
        Log($"Received an error: {error}");
    }

    bool _tryingToReconnect = false;
    private async void ClientOnServerDisconnect()
    {
        if (Status == PlugManagerStatus.ShutDown) return;
        if (_tryingToReconnect) return;
        Log("Disconnected from server.");
        DisconnectedFromServer?.Invoke();
        _tryingToReconnect = true; 
        SetupClient();

        while (tryConnectAttempts <= RetryAttempts)
        {
            Log($"Reconnecting after disconnect...");

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
                    await Initialize();
                }
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            if ((int)Status >= 10) return;
        }
        if (Client == null)
        {
            Log("Intiface Client is null. Trying to setup.");
            if (!allowedToInitialize) SetupClient();
            if (!await Initialize()) return;
        }
        if (!Client.Connected)
        {
            Log("Intiface Client is disconnected - Is the server running?");
            if (!await TryConnect())
            {
                Log("Failed to connect. Resetting client to reinitialize");
                SetupClient();
                if (await Initialize()) Log("Reinitialized!");
                else return;
            }
            else Log("Reconnected!");
        }
        else if (!Client.Devices.Any())
        {
            Log("Intiface Client connected, but no devices are connected.");
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
        foreach (ButtplugDevice plug in GetDevices())
        {
            plug.SendVibrateCmd(currentPower);
            if (rotationEnabled)
            {
                if (!routineUpdate) rotateClockwise = !rotateClockwise;
                try { plug.SendRotateCmd(currentPower, rotateClockwise); }
                catch { }
            }
        }
    }

    public IEnumerable<ButtplugDevice> GetDevices()
    {
        if (Client != null && Client.Devices != null)
        {
            foreach (ButtplugDevice device in Client.Devices)
            {
                if (device != null) yield return device;
            }
        }
    }

    private int _duplicateUpdates;
    public void SetPowerLevel(float level, bool routineUpdate)
    {
        if (level == currentPower) _duplicateUpdates++;
        else _duplicateUpdates = 0;
        if (_duplicateUpdates >= 3) return;

        currentPower = level.Clamp(0, 1);
        Task.Factory.StartNew(() => UpdatePowerLevels(routineUpdate).FireAndForget(Log));
    }

    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }
}