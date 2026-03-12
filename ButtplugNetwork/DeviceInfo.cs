using Buttplug.Client;
using Buttplug.Core.Messages;
using ButtplugSong.Helper;
using System;
using System.Collections.Generic;
using System.Threading;
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
        foreach (FeatureType type in DeviceFeature.AllFeatureTypes)
        {
            var outputType = DeviceFeature.FeatureToOutputType(type);
            bool supported = outputType.HasValue && Device.HasOutput(outputType.Value);
            uint? stepCount = null;

            if (supported)
            {
                foreach (var feature in Device.GetFeaturesWithOutput(outputType!.Value))
                {
                    if (feature.TryGetOutputRange(outputType.Value, out int min, out int max))
                    {
                        stepCount = (uint)max;
                        break;
                    }
                }
            }
            Features[type] = new DeviceFeature(this, type, supported, stepCount);
        }
    }
    public async Task<bool> TryRefreshBattery()
    {
        if (!Device.HasInput(InputType.Battery)) return false;
        try
        {
            using var cts = new CancellationTokenSource(5000);
            Battery = await Device.BatteryAsync(null, cts.Token) * 100;
            return true;
        }
        catch (Exception ex)
        {
            Log($"Battery read failed for {Name}: {ex.Message}");
            return false;
        }
    }
    internal void StopDevice()
    {
        foreach (var featureType in DeviceFeature.Implemented)
        {
            if (!Features.TryGetValue(featureType, out DeviceFeature feature)) continue;
            if (!feature.IsSupported) continue;
            var outputType = DeviceFeature.FeatureToOutputType(feature.Type);
            if (!outputType.HasValue) continue;

            double stopValue = featureType == FeatureType.Temperature ? NeutralTemperature : 0;
            var cmd = new DeviceOutputCommand(outputType.Value, PercentOrSteps.FromPercent(stopValue), null);
            Device.RunOutputAsync(cmd, CancellationToken.None).RunTask();
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
        StopDevice();
    }

    private void ActivateFeatures(float power, bool routineUpdate)
    {
        foreach (var featureType in DeviceFeature.Implemented)
        {
            if (!Features.TryGetValue(featureType, out DeviceFeature feature)) continue;
            if (!feature.IsSupported || !feature.IsEnabled) continue;
            var outputType = DeviceFeature.FeatureToOutputType(feature.Type);
            if (!outputType.HasValue) continue;

            switch (feature.Type)
            {
                case FeatureType.Vibrate:
                case FeatureType.Oscillate:
                case FeatureType.Constrict:
                case FeatureType.Spray:
                case FeatureType.Led:
                    var vibCmd = new DeviceOutputCommand(outputType.Value, PercentOrSteps.FromPercent(power), null);
                    Device.RunOutputAsync(vibCmd, CancellationToken.None).RunTask();
                    break;
                case FeatureType.Temperature:
                    //may have issues, but needs further testing to confirm, and I lack the hardware for this.
                    //In other words, I'll wait for a bug report.
                    double tempValue = NeutralTemperature + power * (1.0 - NeutralTemperature);
                    var tempCmd = new DeviceOutputCommand(outputType.Value, PercentOrSteps.FromPercent(tempValue), null);
                    Device.RunOutputAsync(tempCmd, CancellationToken.None).RunTask();
                    break;
                case FeatureType.Rotate:
                    if (AlternateRotation && !routineUpdate) RotateClockwise = !RotateClockwise;
                    double rotatePower = RotateClockwise ? power : -power;
                    var rotCmd = new DeviceOutputCommand(outputType.Value, PercentOrSteps.FromPercent(rotatePower), null);
                    Device.RunOutputAsync(rotCmd, CancellationToken.None).RunTask();
                    break;
                case FeatureType.Position:
                    uint durationMs = (uint)(MoveDuration * 1000f);
                    var posCmd = new DeviceOutputCommand(outputType.Value, PercentOrSteps.FromPercent(power), durationMs);
                    Device.RunOutputAsync(posCmd, CancellationToken.None).RunTask();
                    break;
                    //other features not implemented yet - see DeviceFeature.Implemented
            }
        }
    }
    public void Unload()
    {
        StopDevice();
        PlugManager.UpdateDevicePower -= ActivateFeatures;
    }
}