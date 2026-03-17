using ButtplugSong.Helper;
using ButtplugSong.Network;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.Network;

internal class DeviceUI : GUISection
{
    VisualElement root;

    private readonly NetworkSettings _networkSettings;
    internal readonly DeviceInfo DeviceInfo;

    internal readonly Toggle _enabled;
    private readonly Label _batteryLabel;
    private readonly Dictionary<FeatureType, FeatureUI> _features;
    private readonly Button _testButton;

    private readonly List<string> batteryColourStyles = ["red-text", "orange-text", "yellow-text", "grellow-text", "green-text"];
    private int workingBatterySensor = 5;
    private float timeSinceLastBatteryUpdate = 55;

    public DeviceUI(ButtplugDevice device, NetworkSettings parent) : base("Device")
    {
        DeviceInfo = new DeviceInfo(device, Log);

        _networkSettings = parent;
        root = Vibe.UI.Device.Instantiate();

        _enabled = root.Q<Toggle>("DeviceEnabled");
        _enabled.text = DeviceInfo.Name;
        _enabled.value = true;
        _enabled.RegisterValueChangedCallback(EnabledToggleChanged);

        Label fullName = root.Q<Label>("FullNameLabel");
        fullName.text = $"Full name: {DeviceInfo.Name}";
        fullName.SetClassListIf("hide", x => DeviceInfo.Name.Length <= 20);

        _batteryLabel = root.Q<Label>("BatteryPercentage");
        Task.Run(UpdateBatteryDisplay);

        Label idLabel = root.Q<Label>("IDLabel");
        idLabel.text = $"ID: {DeviceInfo.Id}";

        _features = new();
        foreach (var feature in DeviceInfo.Features)
        {
            _features.Add(feature.Key, GetFeatureUI(feature.Key, feature.Value));
        }

        _testButton = root.Q<Button>("TestDeviceButton");
        _testButton.clicked += TestButtonClicked;

        parent.AddToListView(root);

        FeatureUI GetFeatureUI(FeatureType key, DeviceFeature feature)
        {
            return key switch
            {
                FeatureType.Rotate => new RotateFeatureUI(this, root, feature),
                FeatureType.Temperature => new TemperatureFeatureUI(this, root, feature),
                FeatureType.Position => new PositionFeatureUI(this, root, feature),
                _ => new FeatureUI(this, root, key, feature),
            };
        }
    }
    private void TestButtonClicked()
    {
        DeviceInfo.Test(0.25f, 0.5f);
    }

    private void EnabledToggleChanged(ChangeEvent<bool> evt)
    {
        DeviceInfo.IsEnabled = evt.newValue;
        _testButton.enabledSelf = evt.newValue;
        if (!evt.newValue)
        {
            DeviceInfo.Device.SendStopCmd();
        }
    }

    public void Update(float realTime)
    {
        DeviceInfo.Update(realTime);
        timeSinceLastBatteryUpdate += realTime;
        if (timeSinceLastBatteryUpdate > 60)
        {
            timeSinceLastBatteryUpdate -= 60;
            Task.Run(UpdateBatteryDisplay);
        }
    }
    private async Task UpdateBatteryDisplay()
    {
        if (workingBatterySensor <= 0) return;
        workingBatterySensor -= 1;
        if (await DeviceInfo.TryRefreshBattery() && DeviceInfo.Battery.HasValue)
        {
            workingBatterySensor = 10;
            _batteryLabel.text = $"Battery: {DeviceInfo.Battery:0}%";
            SetBatteryColour(DeviceInfo.Battery.Value);
        }
        else
        {
            _batteryLabel.text = "Battery unknown";
            SetBatteryColour(0);
            timeSinceLastBatteryUpdate = 50;
        }
        void SetBatteryColour(double battery)
        {
            foreach (string style in batteryColourStyles) _batteryLabel.RemoveFromClassList(style);
            string colourBand = battery switch
            {
                > 85 => "green-text",
                > 50 => "grellow-text",
                > 15 => "yellow-text",
                > 0 => "orange-text",
                _ => "red-text"
            };
            _batteryLabel.AddToClassList(colourBand);
        }
    }

    internal void Unload()
    {
        DeviceInfo.Unload();
        root.parent.Remove(root);
    }
}
