using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ButtplugSong.Network;

public class ButtplugRawClient
{
    private ClientWebSocket _ws;
    private int _msgIdCounter;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JToken>> _pending = new();
    private readonly Action<string> _log;
    private CancellationTokenSource _cts;

    public event Action<ButtplugDevice> DeviceAdded;
    public event Action<ButtplugDevice> DeviceRemoved;
    public event Action ServerDisconnect;
    public event Action<string> ErrorReceived;

    public bool Connected => _ws != null && _ws.State == WebSocketState.Open;
    public List<ButtplugDevice> Devices { get; } = new();

    public ButtplugRawClient(Action<string> log)
    {
        _log = log;
    }

    public async Task ConnectAsync(string address, int port)
    {
        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        var uri = new Uri($"ws://{address}:{port}/buttplug");
        await _ws.ConnectAsync(uri, _cts.Token);

        StartReceiveLoop();

        int id = NextId();
        var tcs = RegisterPending(id);
        await SendAsync($"[{{\"RequestServerInfo\":{{\"Id\":{id},\"ClientName\":\"ButtplugSong\",\"MessageVersion\":3}}}}]");

        var timeoutTask = Task.Delay(5000);
        var result = await Task.WhenAny(tcs.Task, timeoutTask);
        if (result == timeoutTask)
            throw new TimeoutException("Handshake timeout");
    }

    public void Disconnect()
    {
        try { _cts?.Cancel(); } catch { }
        try
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait(1000);
        }
        catch { }
        _ws = null;
        _cts = null;
        Devices.Clear();
        _pending.Clear();
    }

    public void StartScanning()
    {
        int id = NextId();
        _ = SendAsync($"[{{\"StartScanning\":{{\"Id\":{id}}}}}]");
    }

    public void StopScanning()
    {
        int id = NextId();
        _ = SendAsync($"[{{\"StopScanning\":{{\"Id\":{id}}}}}]");
    }

    public void RequestDeviceList()
    {
        int id = NextId();
        _ = SendAsync($"[{{\"RequestDeviceList\":{{\"Id\":{id}}}}}]");
    }

    public void SendScalar(uint deviceIndex, double value, string actuatorType, int actuatorIndex = 0)
    {
        int id = NextId();
        string val = value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        _ = SendAsync($"[{{\"ScalarCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex},\"Scalars\":[{{\"Index\":{actuatorIndex},\"Scalar\":{val},\"ActuatorType\":\"{actuatorType}\"}}]}}}}]");
    }

    public void SendScalarAll(uint deviceIndex, double value, IEnumerable<(int index, string actuatorType)> actuators)
    {
        int id = NextId();
        string val = value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        var scalars = actuators.Select(a => $"{{\"Index\":{a.index},\"Scalar\":{val},\"ActuatorType\":\"{a.actuatorType}\"}}");
        string scalarsJson = string.Join(",", scalars);
        if (string.IsNullOrEmpty(scalarsJson)) return;
        _ = SendAsync($"[{{\"ScalarCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex},\"Scalars\":[{scalarsJson}]}}}}]");
    }

    public void SendRotate(uint deviceIndex, double speed, bool clockwise, int actuatorIndex = 0)
    {
        int id = NextId();
        string spd = speed.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        string cw = clockwise ? "true" : "false";
        _ = SendAsync($"[{{\"RotateCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex},\"Rotations\":[{{\"Index\":{actuatorIndex},\"Speed\":{spd},\"Clockwise\":{cw}}}]}}}}]");
    }

    public void SendLinear(uint deviceIndex, uint durationMs, double position, int actuatorIndex = 0)
    {
        int id = NextId();
        string pos = position.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        _ = SendAsync($"[{{\"LinearCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex},\"Vectors\":[{{\"Index\":{actuatorIndex},\"Duration\":{durationMs},\"Position\":{pos}}}]}}}}]");
    }

    public void SendStop(uint deviceIndex)
    {
        int id = NextId();
        _ = SendAsync($"[{{\"StopDeviceCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex}}}}}]");
    }

    public Task<double?> ReadBatteryAsync(uint deviceIndex)
    {
        int id = NextId();
        var tcs = RegisterPending(id);

        _ = SendAsync($"[{{\"SensorReadCmd\":{{\"Id\":{id},\"DeviceIndex\":{deviceIndex},\"SensorIndex\":0,\"SensorType\":\"Battery\"}}}}]");

        Task.Delay(3000).ContinueWith(_ =>
        {
            TaskCompletionSource<JToken> ignored;
            _pending.TryRemove(id, out ignored);
            tcs.TrySetResult(null);
        });

        return tcs.Task.ContinueWith(t =>
        {
            if (t.IsFaulted || t.Result == null) return (double?)null;
            try
            {
                var data = t.Result["Data"];
                if (data != null && data.HasValues)
                {
                    var first = data[0];
                    double val;
                    if (first.Type == JTokenType.Array)
                        val = first[0].Value<double>();
                    else
                        val = first.Value<double>();

                    if (val <= 1.0) val *= 100.0;
                    _log($"Battery read: {val}%");
                    return (double?)val;
                }
            }
            catch (Exception ex) { _log($"Battery parse error: {ex.Message}"); }
            return (double?)null;
        });
    }

    private async void StartReceiveLoop()
    {
        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();
        try
        {
            while (_ws != null && _ws.State == WebSocketState.Open && _cts != null && !_cts.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _log($"WS receive error: {ex.Message}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close) break;

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    string msg = messageBuffer.ToString();
                    messageBuffer.Clear();
                    try { HandleMessage(msg); }
                    catch (Exception ex) { _log($"Message handle error: {ex.Message}"); }
                }
            }
        }
        catch (Exception ex)
        {
            _log($"Receive loop ended: {ex.Message}");
        }

        Devices.Clear();
        ServerDisconnect?.Invoke();
    }

    private async Task SendAsync(string msg)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(msg);
        try
        {
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log($"WS send error: {ex.Message}");
        }
    }

    private void HandleMessage(string raw)
    {
        JArray messages;
        try { messages = JArray.Parse(raw); }
        catch (Exception ex)
        {
            _log($"JSON parse error: {ex.Message}");
            return;
        }

        foreach (JObject wrapper in messages)
        {
            var prop = wrapper.Properties().FirstOrDefault();
            if (prop == null) continue;

            string msgType = prop.Name;
            JObject body = prop.Value as JObject;
            if (body == null) continue;

            int msgId = body.Value<int?>("Id") ?? -1;

            switch (msgType)
            {
                case "ServerInfo":
                    ResolvePending(msgId, body);
                    break;

                case "Ok":
                    ResolvePending(msgId, body);
                    break;

                case "Error":
                    string errMsg = body.Value<string>("ErrorMessage") ?? "Unknown error";
                    ResolvePending(msgId, null);
                    ErrorReceived?.Invoke(errMsg);
                    break;

                case "DeviceAdded":
                    HandleDeviceAdded(body);
                    break;

                case "DeviceRemoved":
                    HandleDeviceRemoved(body);
                    break;

                case "DeviceList":
                    HandleDeviceList(body);
                    break;

                case "SensorReading":
                    ResolvePending(msgId, body);
                    break;

                case "ScanningFinished":
                    // Auto-restart scanning so disconnected devices can reconnect
                    StartScanning();
                    break;
            }
        }
    }

    private void HandleDeviceAdded(JObject body)
    {
        var device = ParseDevice(body);
        if (device == null) return;
        Devices.RemoveAll(d => d.Index == device.Index);
        Devices.Add(device);
        DeviceAdded?.Invoke(device);
    }

    private void HandleDeviceRemoved(JObject body)
    {
        uint devIdx = body.Value<uint>("DeviceIndex");
        var dev = Devices.FirstOrDefault(d => d.Index == devIdx);
        if (dev != null)
        {
            Devices.Remove(dev);
            DeviceRemoved?.Invoke(dev);
            // Re-start scanning so the device can reconnect
            StartScanning();
        }
    }

    private void HandleDeviceList(JObject body)
    {
        var devicesArray = body["Devices"] as JArray;
        if (devicesArray == null) return;
        foreach (JObject devObj in devicesArray)
        {
            var device = ParseDevice(devObj);
            if (device == null) continue;
            Devices.RemoveAll(d => d.Index == device.Index);
            Devices.Add(device);
            DeviceAdded?.Invoke(device);
        }
    }

    private ButtplugDevice ParseDevice(JObject body)
    {
        try
        {
            uint index = body.Value<uint>("DeviceIndex");
            string name = body.Value<string>("DeviceName") ?? "Unknown";

            var features = new List<DeviceFeature>();
            var msgs = body["DeviceMessages"] as JObject;

            if (msgs != null)
            {
                var scalarCmd = msgs["ScalarCmd"] as JArray;
                if (scalarCmd != null)
                {
                    for (int i = 0; i < scalarCmd.Count; i++)
                    {
                        var entry = scalarCmd[i] as JObject;
                        string actType = entry?.Value<string>("ActuatorType") ?? "Vibrate";
                        int stepCount = entry?.Value<int?>("StepCount") ?? 20;
                        features.Add(new DeviceFeature("ScalarCmd", actType, i, stepCount));
                    }
                }

                var rotateCmd = msgs["RotateCmd"] as JArray;
                if (rotateCmd != null)
                {
                    for (int i = 0; i < rotateCmd.Count; i++)
                    {
                        var entry = rotateCmd[i] as JObject;
                        int stepCount = entry?.Value<int?>("StepCount") ?? 20;
                        features.Add(new DeviceFeature("RotateCmd", "Rotate", i, stepCount));
                    }
                }

                var linearCmd = msgs["LinearCmd"] as JArray;
                if (linearCmd != null)
                {
                    for (int i = 0; i < linearCmd.Count; i++)
                    {
                        var entry = linearCmd[i] as JObject;
                        int stepCount = entry?.Value<int?>("StepCount") ?? 100;
                        features.Add(new DeviceFeature("LinearCmd", "Position", i, stepCount));
                    }
                }
            }

            bool hasBattery = HasBatteryCapability(msgs);
            _log($"Device {name}: features={features.Count}, hasBattery={hasBattery}");
            return new ButtplugDevice(name, index, features, hasBattery, this);
        }
        catch (Exception ex)
        {
            _log($"Failed to parse device: {ex.Message}");
            return null;
        }
    }

    private bool HasBatteryCapability(JObject msgs)
    {
        if (msgs == null) return false;
        var sensorRead = msgs["SensorReadCmd"];
        if (sensorRead == null) return false;
        var arr = sensorRead as JArray;
        if (arr != null)
        {
            foreach (JObject sensor in arr)
            {
                string sType = sensor.Value<string>("SensorType");
                if (sType == "Battery") return true;
            }
        }
        return false;
    }

    private void ResolvePending(int msgId, JToken body)
    {
        if (msgId >= 0 && _pending.TryRemove(msgId, out var tcs))
            tcs.TrySetResult(body);
    }

    private int NextId() => Interlocked.Increment(ref _msgIdCounter);

    private TaskCompletionSource<JToken> RegisterPending(int id)
    {
        var tcs = new TaskCompletionSource<JToken>();
        _pending[id] = tcs;
        return tcs;
    }
}
