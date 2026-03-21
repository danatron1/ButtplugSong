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
        FeatureType.Position
    };

    public FeatureType Type { get; }
    public bool IsSupported { get; internal set; }
    public uint? StepCount { get; }
    public DeviceInfo DeviceInfo { get; internal set; }
    public bool IsEnabled { get; set; }

    // Raw protocol fields (consolidated from ButtplugDevice.Feature)
    public string CommandType { get; }
    public string ActuatorType { get; }
    public int ActuatorIndex { get; }

    public DeviceFeature(DeviceInfo device, FeatureType type, bool isSupported, uint? stepCount)
    {
        DeviceInfo = device;
        Type = type;
        IsSupported = isSupported;
        StepCount = stepCount;
        IsEnabled = isSupported;
    }

    /// <summary>Protocol-level constructor used during device parsing.</summary>
    public DeviceFeature(string commandType, string actuatorType, int actuatorIndex, int stepCount)
    {
        CommandType = commandType;
        ActuatorType = actuatorType;
        ActuatorIndex = actuatorIndex;
        StepCount = (uint)stepCount;
        IsSupported = true;
        IsEnabled = false;
    }

    internal static readonly string[] AllCommandKeys =
    [
        "VibrateCmd",
        "RotateCmd",
        "OscillateCmd",
        "ConstrictionCmd",
        "SprayCmd",
        "TemperatureCmd",
        "LedCmd",
        "LinearCmd",
        "ShockCmd"
    ];

    internal static FeatureType CommandKeyToType(string key)
    {
        return key switch
        {
            "VibrateCmd" => FeatureType.Vibrate,
            "RotateCmd" => FeatureType.Rotate,
            "OscillateCmd" => FeatureType.Oscillate,
            "ConstrictionCmd" => FeatureType.Constrict,
            "SprayCmd" => FeatureType.Spray,
            "TemperatureCmd" => FeatureType.Temperature,
            "LedCmd" => FeatureType.Led,
            "LinearCmd" => FeatureType.Position,
            "ShockCmd" => FeatureType.Shock,
            _ => throw new ArgumentException($"Unknown command key: {key}"),
        };
    }

    internal static string TypeToCommandKey(FeatureType type)
    {
        return type switch
        {
            FeatureType.Vibrate => "VibrateCmd",
            FeatureType.Rotate => "RotateCmd",
            FeatureType.Oscillate => "OscillateCmd",
            FeatureType.Constrict => "ConstrictionCmd",
            FeatureType.Spray => "SprayCmd",
            FeatureType.Temperature => "TemperatureCmd",
            FeatureType.Led => "LedCmd",
            FeatureType.Position => "LinearCmd",
            FeatureType.Shock => "ShockCmd",
            _ => throw new ArgumentException($"Unknown feature type: {type}"),
        };
    }
}