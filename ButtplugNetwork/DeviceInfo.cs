using ButtplugManaged;
using ButtplugSong.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class DeviceInfo
{
    internal readonly ButtplugClientDevice Device;
    private readonly Action<string> Log;

    public string Name => Device.Name;
    public uint Id => Device.Index;
    public double? Battery { get; private set; }
    public bool IsEnabled { get; set; } = true;

    //specific feature settings;
    public bool AlternateRotation { get; internal set; } = true;
    public bool RotateClockwise { get; set; } = true;
    public float NeutralTemperature { get; set; } = 0f;
    public float MoveDuration { get; set; } = 1.5f;

    public Dictionary<FeatureType, DeviceFeature> Features { get; } = new Dictionary<FeatureType, DeviceFeature>();

    private float _testTimeRemaining;
    private bool _isTesting;

    public DeviceInfo(ButtplugClientDevice device, Action<string> log)
    {
        Device = device;
        Log = log;
        PopulateFeatures();

        PlugManager.UpdateDevicePower += ActivateFeatures;
    }

    private void PopulateFeatures()
    {
        foreach (string cmdKey in DeviceFeature.AllCommandKeys)
        {
            var type = DeviceFeature.CommandKeyToType(cmdKey);
            bool supported = Device.AllowedMessages.TryGetValue(cmdKey, out var deviceMessageDetails);
            uint? stepCount = null;

            if (supported && deviceMessageDetails.StepCount != null && deviceMessageDetails.StepCount.Count > 0) stepCount = deviceMessageDetails.StepCount[0];

            Features[type] = new DeviceFeature(this, type, supported, stepCount);
        }
    }
    public async Task<bool> TryRefreshBattery()
    {
        if (!Device.AllowedMessages.ContainsKey("BatteryLevelCmd")) return false;
        try
        {
            Battery = await Device.SendBatteryLevelCmd() * 100;
            return true;
        }
        catch (Exception ex)
        {
            Log($"Battery read failed for {Name}: {ex.Message}");
            return false;
        }
    }

    public void Test(float power, float duration)
    {
        if (!IsEnabled) return;
        _testTimeRemaining = duration;
        _isTesting = true;
        ActivateFeatures(Math.Max(0f, Math.Min(1f, power)), false);
    }

    public void Update(float deltaTime)
    {
        if (!_isTesting) return;
        _testTimeRemaining -= deltaTime;
        if (_testTimeRemaining > 0f) return;

        _isTesting = false;
        _testTimeRemaining = 0f;
        Device.SendStopDeviceCmd().FireAndForget(Log);
    }

    private void ActivateFeatures(float power, bool routineUpdate)
    {
        foreach (var featureType in DeviceFeature.Implemented)
        {
            if (!Features.TryGetValue(featureType, out DeviceFeature feature)) continue;
            if (!feature.IsSupported || !feature.IsEnabled) continue;
            switch (feature.Type)
            {
                case FeatureType.Vibrate:
                    Device.SendVibrateCmd(power).FireAndForget(Log);
                    break;
                case FeatureType.Rotate:
                    if (AlternateRotation) RotateClockwise = !RotateClockwise;
                    Device.SendRotateCmd(power, RotateClockwise).FireAndForget(Log);
                    break;
                case FeatureType.Position:
                    uint durationMs = (uint)(MoveDuration * 1000f);
                    Device.SendLinearCmd(durationMs, power).FireAndForget(Log);
                    break;
                    //other features not implemented yet - see DeviceFeature.Implemented
            }
        }
    }
    public void Unload()
    {
        PlugManager.UpdateDevicePower -= ActivateFeatures;
    }
}