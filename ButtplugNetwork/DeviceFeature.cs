using Buttplug.Core.Messages;
using System;
using System.Collections.Generic;

namespace ButtplugSong.Network;

public enum FeatureType
{
    Vibrate,
    Rotate,
    Oscillate,
    Constrict,
    Spray,
    Temperature,
    Led,
    Position,
    Shock,
    //^ Shock is here for if I ever decide to add pishock/openshock support in the future. It is not currently planned.
}
public class DeviceFeature
{
    public static HashSet<FeatureType> Implemented = new()
    {
        FeatureType.Vibrate,
        FeatureType.Rotate,
        FeatureType.Oscillate,
        FeatureType.Constrict,
        FeatureType.Spray,
        FeatureType.Temperature,
        FeatureType.Led,
        FeatureType.Position
    };

    public FeatureType Type { get; }
    public bool IsSupported { get; }
    public uint? StepCount { get; }
    public DeviceInfo DeviceInfo { get; }
    public bool IsEnabled { get; set; }

    public DeviceFeature(DeviceInfo device, FeatureType type, bool isSupported, uint? stepCount)
    {
        DeviceInfo = device;
        Type = type;
        IsSupported = isSupported;
        StepCount = stepCount;
        IsEnabled = isSupported;
    }
    internal static FeatureType[] AllFeatureTypes => (FeatureType[])Enum.GetValues(typeof(FeatureType));
    internal static OutputType? FeatureToOutputType(FeatureType type)
    {
        return type switch
        {
            FeatureType.Vibrate => OutputType.Vibrate,
            FeatureType.Rotate => OutputType.Rotate,
            FeatureType.Oscillate => OutputType.Oscillate,
            FeatureType.Constrict => OutputType.Constrict,
            FeatureType.Spray => OutputType.Spray,
            FeatureType.Temperature => OutputType.Temperature,
            FeatureType.Led => OutputType.Led,
            FeatureType.Position => OutputType.Position,
            _ => null,
        };
    }
}