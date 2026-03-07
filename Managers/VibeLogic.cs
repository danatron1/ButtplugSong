using System;
using UnityEngine;

namespace GoodVibes;

enum WaveType
{
    ConstantLinear,
    ConstantExponental,
    SineSmall,
    SineBig,
    Square,
    PulseWidth,
    Triangle,
    InverseTriangle,
    Bounce,
    ZigZag,
}
public enum AfterZeroMode
{
    Subtract,
    Multiply,
}
internal class VibeLogic
{
    public static bool Armed = false;

    public event Action? PowerChanged;
    public event Action<bool>? PunctuateChanged;
    public event Action? VibeSourceActivated;
    public event Action? TimerHitZero;
    public event Action<string, string, float>? AddActivityLog;

    private Action<string>? _logger;

    private (string identifier, string details)? triggeredActivityLog = null;
    internal bool timerCountdownPaused = true;
    internal bool vibeWhileTimerPaused = true;

    #region Timer Management
    public bool ShouldCountDown;
    internal const float _actualHardTimerMaximum = 5999; //99:59 on the display.
    //Maximum timer, default 10 minutes
    private float _maxTimer = 600;
    public float MaxTimer
    {
        get => _maxTimer;
        set
        {
            _maxTimer = value.Clamp(0, _actualHardTimerMaximum);
            if (Time > _maxTimer) Time = _maxTimer;
        }
    }
    //Current timer
    public bool TimerZero => _timeRemaining <= 0;
    public bool IsVibing => ActualPower > 0;
    private float _timeRemaining;
    public float Time
    {
        get => _timeRemaining;
        set
        {
            bool wasOn = !TimerZero;
            _timeRemaining = value.Clamp(0, MaxTimer);
            if (TimerZero && wasOn) TimerBecameZero();
        }
    }
    //After zero settings
    internal AfterZeroMode afterZeroMode;
    internal float afterZeroPowerChange = 1;
    internal float afterZeroTimer = 0;
    internal float afterZeroPunctuate = 0;
    private void TimerBecameZero()
    {
        if (TargetPower == MinPower && TimerZero) return;

        string logMessage, identifier = "Timer Hit Zero";
        if (TargetPower == MinPower) //timer becomes zero because power hit minimum
        {
            identifier += " : Power Min";
            logMessage = $"Power hit {(MinPower == 0 ? "zero" : "minimum")}.\nTimer reset to 0 seconds.";
            Time = 0;
        }
        else //power becomes zero because timer hit zero
        {
            float newPower = afterZeroMode switch
            {
                AfterZeroMode.Subtract => TargetPower - afterZeroPowerChange,
                AfterZeroMode.Multiply => TargetPower * afterZeroPowerChange,
                _ => throw new NotImplementedException()
            };
            newPower = newPower.Clamp(MinPower, MaxPower);
            logMessage = $"Power: {TargetPower * 100:0.#}% {AfterZeroModeString()} {afterZeroPowerChange * 100:0.#}% = {newPower * 100:0.#}%";
            if (MinPower != 0) logMessage += $" (minimum {MinPower * 100:0.#}%)";
            if (newPower - MinPower < 0.02f)
            {
                if (newPower - MinPower > Mathf.Epsilon) logMessage += $"\n(New power almost {(MinPower == 0 ? "zero" : "minimum")}; cutting off)";
                newPower = MinPower;
            }
            else //power not zero, add time
            {
                if (afterZeroTimer <= 0)
                {
                    logMessage += $"\nNo timer increase, so setting power to {(MinPower == 0 ? "0" : "minimum")}.";
                    newPower = MinPower;
                }
                else
                {
                    logMessage += $"\nTimer set to {afterZeroTimer} second{(afterZeroTimer != 1 ? "s" : "")}.";
                    Time += afterZeroTimer;
                }
            }
            TargetPower = newPower;
        }
        if (afterZeroPunctuate > 0)
        {
            AddPunctuateHit(afterZeroPunctuate);
            logMessage += $"\nPunctuate: +{afterZeroPunctuate}s of max power";
        }

        triggeredActivityLog = (identifier, logMessage);
        TimerHitZero?.Invoke();

        string AfterZeroModeString()
        {
            return afterZeroMode switch
            {
                AfterZeroMode.Subtract => "-",
                AfterZeroMode.Multiply => "*",
                _ => throw new NotImplementedException()
            };
        }
    }
    public void DecreaseTimer(float realTimeAmount, float timerAmount)
    {
        TickActivityLogs(realTimeAmount);
        if (Punctuating)
        {
            punctuatingTimer -= timerAmount;
            if (punctuatingTimer <= 0)
            {
                Time += punctuatingTimer; //take excess time from the main timer.
                punctuatingTimer = 0;
                PunctuateChanged?.Invoke(false);
            }
        }
        else Time -= timerAmount;
        timerCountdownPaused = timerAmount <= float.Epsilon;
    }
    //Punctuate hits logic
    public bool Punctuating => punctuatingTimer > 0;
    private float punctuatingTimer = 0;
    #endregion

    #region Power Management
    //Maximum power. Default 1 (meaning 100%)
    private float _maxPower = 1;
    public float MaxPower
    {
        get => _maxPower;
        set
        {
            float previousValue = _maxPower;
            _maxPower = value.Clamp(0, 1);
            if (MinPower > MaxPower) MinPower = MaxPower;
            if (_maxPower != previousValue) UpdatePowerClamped(TargetPower);
        }
    }
    //Minimum power Default 0 (off)
    private float _minPower = 0;
    public float MinPower
    {
        get => _minPower;
        set
        {
            float previousValue = _minPower;
            _minPower = value.Clamp(0, MaxPower);
            if (_minPower != previousValue) UpdatePowerClamped(TimerZero ? _minPower : TargetPower);
        }
    }

    //Target power
    private float _targetPower;
    public float TargetPower
    {
        get => _targetPower;
        private set => UpdatePowerClamped(value);
    }
    private void UpdatePowerClamped(float value)
    {
        float powerBefore = _targetPower;
        _targetPower = value.Clamp(MinPower, MaxPower);
        if (_targetPower != powerBefore) PowerUpdated(powerBefore, _targetPower);
    }

    //Called whenever power is updated
    private void PowerUpdated(float before, float after)
    {
        if (after == MinPower) TimerBecameZero(); //set through this method instead of directly for logging purposes.
        PowerChanged?.Invoke();
    }
    //Actual power - differs based on punctuate hits and waves
    public float ActualPower
    {
        get
        {
            if (timerCountdownPaused && !vibeWhileTimerPaused) return GetWavePower(MinPower);
            if (Punctuating) return MaxPower;
            if (TimerZero) return GetWavePower(MinPower);
            return GetWavePower(TargetPower);
        }
    }

    #endregion

    #region Wave Management
    public WaveType waveType;
    public float wavePeriod = 1;
    private float GetWavePower(float power)
    {
        if (waveType == WaveType.ConstantLinear || power == 0) return power;
        float period = Time / wavePeriod;
        float wavePower = waveType switch
        {
            WaveType.ConstantExponental => power * power,
            WaveType.SineSmall => power * ((Mathf.Sin(period * Mathf.PI * 2) + 2) / 3),
            WaveType.SineBig => Mathf.Sin(period * Mathf.PI * 2) * 0.8f * power + (0.8f * power),
            WaveType.Square => (period % 1f) > 0.5f ? 0f : power,
            WaveType.PulseWidth => (period % 1f) > power ? 0f : 1f,
            WaveType.Triangle => period % 1f * power,
            WaveType.InverseTriangle => power * (1 - (period % 1f)),
            WaveType.Bounce => period % 1 * (1 - (period % 1)) * power * 4,
            WaveType.ZigZag => Mathf.Max(period % 1, 1 - (period % 1)) * power + (power / 4),
            _ => power
        };
        return wavePower.Clamp(MinPower, MaxPower);
    }

    #endregion

    public VibeLogic(Action<string>? logger = null)
    {
        _logger = logger;

    }
    public void VibeSourceActivation(string identifier, float power, string powerMode, float time, string timeMode, float punctuateTime, float? basePower = null, float? baseTime = null)
    {
        if (!Armed) return;

        //record initial values, and initial changes, for logging purposes
        float powerBefore = TargetPower;
        float timeBefore = Time;

        float newPower = ChangeByMode(TargetPower, power, powerMode).Clamp(MinPower, MaxPower);
        float newTime = ChangeByMode(Time, time, timeMode).Clamp(0, MaxTimer);
        if (newTime != 0) TargetPower = newPower;
        if (TargetPower != MinPower) Time = newTime;

        if (punctuateTime != 0) AddPunctuateHit(punctuateTime);

        SendDetailedActivityLog();
        VibeSourceActivated?.Invoke();

        static float ChangeByMode(float original, float modifier, string mode)
        {
            return mode switch
            {
                "+" => original + modifier,
                "-" => original - modifier,
                "=" => modifier,
                "≥" => Mathf.Max(modifier, original),
                "≤" => Mathf.Min(modifier, original),
                "x" => original * modifier,
                _ => original
            };
        }

        void SendDetailedActivityLog()
        {
            string vibeLine = "";
            if (!PowerFieldMeaningless(power, powerMode))
            {
                vibeLine += $"Power: {powerMode}{power * 100:0.#}%";
                if (basePower.HasValue && basePower.Value != power) vibeLine += $" ({basePower.Value * 100:0.#}% x {power / basePower.Value:0.###})";
                if (newPower == powerBefore) vibeLine += $", remain at {powerBefore * 100:0.#}%";
                else vibeLine += $", {powerBefore * 100:0.#}% -> {newPower * 100:0.#}%";
                if (MinPower != 0 && newPower == MinPower) vibeLine += " (min)";
                else if (MaxPower != 1 && newPower == MaxPower) vibeLine += " (max)";
            }
            if (!TimeFieldMeaningless(time, timeMode))
            {
                vibeLine += $"\nTime: {timeMode}{DisplayTime(time)}";
                if (baseTime.HasValue && baseTime.Value != time) vibeLine += $" ({DisplayTime(baseTime.Value, false)} x {time / baseTime.Value:0.###})";
                if (newTime == timeBefore) vibeLine += $", remain at {DisplayTime(timeBefore)}";
                else vibeLine += $", {DisplayTime(timeBefore, false)} -> {DisplayTime(newTime, false)}";
                if (MaxTimer == newTime) vibeLine += " (max)";
            }
            if (punctuateTime != 0) vibeLine += $"\nPunctuate: +{punctuateTime:0.##}s (now at {punctuatingTimer:0.##}s)";
            vibeLine = vibeLine.Trim('\n');
            if (string.IsNullOrWhiteSpace(vibeLine)) return;
            ProcessActivityLog(identifier, vibeLine);
        }
    }
    internal static string DisplayTime(float time, bool includeS = true) //consider moving elsewhere as GUIManager also uses
    {
        int mins = (int)(time / 60);
        int hours = mins / 60;
        if (hours >= 1) return $"{hours:f0}:{mins:00}:{time % 60:00}"; //yikes
        if (mins >= 1) return $"{mins:f0}:{time % 60:00}";
        if (includeS) return $"{time:0.#}s";
        return $"{time:0.#}";
    }
    private void AddPunctuateHit(float punctuateTime)
    {
        bool wasPunctuating = Punctuating;
        punctuatingTimer += punctuateTime;
        if (Punctuating != wasPunctuating) PunctuateChanged?.Invoke(Punctuating);
    }

    internal static bool PowerFieldMeaningless(float power, string powerMode)
    {
        return powerMode switch
        {
            "+" or "-" or "≥" => power <= 0,
            "≤" => power >= 1,
            "x" => power == 1,
            _ => false
        };
    }
    internal static bool TimeFieldMeaningless(float time, string timeMode)
    {
        return timeMode switch
        {
            "+" or "-" or "≥" => time <= 0,
            "x" => time == 1,
            _ => false
        };
    }
    private void Log(string s) => _logger?.Invoke(s);

    private float _timeSinceLastActivityLog;
    private void TickActivityLogs(float deltaTime)
    {
        _timeSinceLastActivityLog += deltaTime;
        ProcessTriggeredActivityLog();
    }
    private void ProcessActivityLog(string identifier, string details)
    {
        AddActivityLog?.Invoke(identifier, details, _timeSinceLastActivityLog);
        _timeSinceLastActivityLog = 0;
        ProcessTriggeredActivityLog();
    }
    private void ProcessTriggeredActivityLog()
    {
        if (triggeredActivityLog is null) return;
        string identifier = triggeredActivityLog.Value.identifier, details = triggeredActivityLog.Value.details;
        triggeredActivityLog = null;
        ProcessActivityLog(identifier, details);
    }

    internal void MultiplyTimer(float multiplier, string? subID = null)
    {
        if (multiplier == 1 || Time == 0) return;
        float timeBefore = Time;
        Time *= multiplier;
        string identifier = "Multiply Timer";
        if (!string.IsNullOrWhiteSpace(subID)) identifier += $" : {subID}";
        ProcessActivityLog(identifier, $"Timer {multiplier}x multiplier: {DisplayTime(timeBefore, false)} -> {DisplayTime(Time, false)}");
    }
    private class ActivityLogEntry(string identifier, string details)
    {
        internal string Identifier = identifier;
        internal string Details = details;
        internal ActivityLogEntry? TriggeredLog = null;
    }
}

