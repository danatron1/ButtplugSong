using ButtplugManaged;
using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using ButtplugSong.Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.Network;

internal class NetworkSettings : GUISection, IPresetLoadable
{
    private static NetworkSettings? instance = null;
    protected static PlugManager Plug => Vibe.plug;

    private static string defaultServerAddress = "localhost";
    private static int defaultPort = 12345;
    private static int defaultRetryAttempts = 5;
    private static float defaultUpdateFrequency = 0.125f;

    protected readonly Button _reconnectButton;
    protected readonly TextField _serverAddress;
    protected readonly IntegerField _port;
    protected readonly IntegerField _retryAttempts;
    protected readonly FloatField _updateFrequency;

    public static string ServerAddress
    {
        get
        {
            if (instance == null || instance._serverAddress == null || string.IsNullOrWhiteSpace(instance._serverAddress.text)) return defaultServerAddress;
            return instance._serverAddress.text;
        }
    }
    public static int Port
    {
        get
        {
            if (instance == null || instance._port == null) return defaultPort;
            return instance._port.value;
        }
    }
    public static int RetryAttempts
    {
        get
        {
            if (instance == null || instance._retryAttempts == null) return defaultRetryAttempts;
            return instance._retryAttempts.value;
        }
    }
    public static float UpdateFrequency
    {
        get
        {
            if (instance == null || instance._updateFrequency == null) return defaultUpdateFrequency;
            return instance._updateFrequency.value;
        }
    }

    protected readonly Label _debuggingHelpLabel;

    private Dictionary<ButtplugClientDevice, DeviceUI> Devices = new();
    public NetworkSettings() : base("Network")
    {
        instance = this;

        _reconnectButton = Get<Button>("ReconnectButton");
        _serverAddress = Get<TextField>("ServerAddress");
        _serverAddress.SetupSaving();
        _port = Get<IntegerField>("ServerPort");
        _port.SetupSaving().SetupValueClamping(ushort.MinValue, ushort.MaxValue);
        _retryAttempts = Get<IntegerField>("RetryAttempts");
        _retryAttempts.SetupSaving().SetupValueClamping(1, 999);
        _updateFrequency = Get<FloatField>("UpdateFrequency");
        _updateFrequency.SetupSaving().SetupValueClamping(0.017f, 60);

        _debuggingHelpLabel = Get<Label>("DebuggingHelp");

        _reconnectButton.clicked += Vibe.ReconnectPlug;
        Vibe.PlugReconnectEstablished += RefreshDeviceList;
        PlugManager.DeviceConnected += AddDevice;
        PlugManager.DeviceDisconnected += RemoveDevice;
        PlugManager.DisconnectedFromServer += RemoveAllDevices;
        Vibe.NeedsUpdate += Update;
    }
    public void SetToPreset(Preset preset)
    {
        _serverAddress.Load(preset, friendlyName: false);
        _port.Load(preset);
        _retryAttempts.Load(preset);
    }
    private void Update(float realTime, float timerTime)
    {
        foreach (var device in Devices.Values.ToArray()) device.Update(realTime);
    }
    public void AddDevice(ButtplugClientDevice device)
    {
        if (device is null || Devices.ContainsKey(device)) return;
        Devices[device] = new DeviceUI(device, this);
        _debuggingHelpLabel.AddToClassList("hide");
    }
    public void RemoveDevice(ButtplugClientDevice device)
    {
        if (device is null || !Devices.TryGetValue(device, out DeviceUI? deviceUI)) return;
        deviceUI.Unload();
        Devices.Remove(device);
        if (Devices.Count == 0) _debuggingHelpLabel.RemoveFromClassList("hide");
    }
    public void RemoveAllDevices()
    {
        foreach (var device in Devices.Values) device.Unload();
        Devices.Clear();
        _debuggingHelpLabel.RemoveFromClassList("hide");
    }
    public void RefreshDeviceList()
    {
        RemoveAllDevices();
        if (Plug is null) return;
        foreach (var device in Plug.GetDevices()) AddDevice(device);
    }
    internal void AddToListView(VisualElement element) => _debuggingHelpLabel.parent.Add(element);
}
