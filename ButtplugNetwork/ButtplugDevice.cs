using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class ButtplugDevice
{
    public string Name { get; }
    public uint Index { get; }
    public List<DeviceFeature> Features { get; }
    public bool HasBattery { get; }
    private readonly ButtplugRawClient _client;

    public ButtplugDevice(string name, uint index, List<DeviceFeature> features, bool hasBattery, ButtplugRawClient client)
    {
        Name = name ?? "Unknown";
        Index = index;
        Features = features ?? new List<DeviceFeature>();
        HasBattery = hasBattery;
        _client = client;
    }

    public void SendScalarSingle(double value, string actuatorType, int actuatorIndex)
    {
        _client.SendScalar(Index, value, actuatorType, actuatorIndex);
    }

    public void SendVibrateCmd(double speed)
    {
        var actuators = Features
            .Where(f => f.CommandType == "ScalarCmd" && f.ActuatorType == "Vibrate")
            .Select(f => (f.ActuatorIndex, f.ActuatorType));
        _client.SendScalarAll(Index, speed, actuators);
    }

    public void SendRotateCmd(double speed, bool clockwise, int actuatorIndex = 0)
    {
        _client.SendRotate(Index, speed, clockwise, actuatorIndex);
    }

    public void SendLinearCmd(uint durationMs, double position, int actuatorIndex = 0)
    {
        _client.SendLinear(Index, durationMs, position, actuatorIndex);
    }

    public void SendStopCmd()
    {
        _client.SendStop(Index);
    }

    public Task<double?> ReadBatteryAsync()
    {
        if (!HasBattery) return Task.FromResult<double?>(null);
        return _client.ReadBatteryAsync(Index);
    }

    public bool SupportsFeature(string actuatorType)
    {
        foreach (var f in Features)
        {
            if (f.ActuatorType == actuatorType) return true;
        }
        return false;
    }

    public int GetStepCount(string actuatorType)
    {
        foreach (var f in Features)
        {
            if (f.ActuatorType == actuatorType) return (int)(f.StepCount ?? 0);
        }
        return 0;
    }

    public int GetActuatorCount(string actuatorType)
    {
        int count = 0;
        foreach (var f in Features)
        {
            if (f.ActuatorType == actuatorType) count++;
        }
        return count;
    }
}
