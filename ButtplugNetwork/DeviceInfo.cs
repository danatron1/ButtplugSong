using ButtplugSong.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class DeviceInfo
{
    internal readonly ButtplugDevice Device;
    private readonly Action<string> Log;

    public string Name => Device.Name;
    public uint Id => Device.Index;
    public double? Battery { get; private set; }
    public bool IsEnabled { get; set; } = true;

    public bool AlternateRotation { get; internal set; } = true;
    public bool RotateClockwise { get; set; } = true;
    public float NeutralTemperature { get; set; } = 0f;
    public float MoveDuration { get; set; } = 1.5f;

    public Dictionary<FeatureType, DeviceFeature> Features { get; } = new Dictionary<FeatureType, DeviceFeature>();

    private float _testTimeRemaining;
    private bool _isTesting;

    public DeviceInfo(ButtplugDevice device, Action<string> log)
    {
        Device = device;
        Log = log;
        PopulateFeatures();

        PlugManager.UpdateDevicePower += ActivateFeatures;
    }

    private void PopulateFeatures()
    {
        foreach (var rawFeature in Device.Features)
        {
            FeatureType type = ActuatorTypeToFeatureType(rawFeature.ActuatorType);
            uint stepCount = (uint)(rawFeature.StepCount ?? 0);

            if (!Features.ContainsKey(type))
                Features[type] = new DeviceFeature(this, type, true, stepCount);
        }
    }

    private static FeatureType ActuatorTypeToFeatureType(string actuatorType)
    {
        return actuatorType switch
        {
            "Vibrate" => FeatureType.Vibrate,
            "Rotate" => FeatureType.Rotate,
            "Oscillate" => FeatureType.Oscillate,
            "Constrict" => FeatureType.Constrict,
            "Spray" => FeatureType.Spray,
            "Position" => FeatureType.Position,
            _ => FeatureType.Vibrate,
        };
    }

    public async Task<bool> TryRefreshBattery()
    {
        try
        {
            double? level = await Device.ReadBatteryAsync();
            if (level.HasValue)
            {
                Battery = level.Value;
                return true;
            }
            return false;
        }
        catch
        {
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
        Device.SendStopCmd();
    }

    private void ActivateFeatures(float power, bool routineUpdate)
    {
        if (!IsEnabled) return;
        foreach (var featureType in DeviceFeature.Implemented)
        {
            if (!Features.TryGetValue(featureType, out DeviceFeature feature)) continue;
            if (!feature.IsSupported || !feature.IsEnabled) continue;

            switch (feature.Type)
            {
                case FeatureType.Vibrate:
                    Device.SendVibrateCmd(power);
                    break;
                case FeatureType.Rotate:
                    if (AlternateRotation) RotateClockwise = !RotateClockwise;
                    Device.SendRotateCmd(power, RotateClockwise);
                    break;
                case FeatureType.Position:
                    uint durationMs = (uint)(MoveDuration * 1000f);
                    Device.SendLinearCmd(durationMs, power);
                    break;
            }
        }
    }

    public void Unload()
    {
        PlugManager.UpdateDevicePower -= ActivateFeatures;
    }
}