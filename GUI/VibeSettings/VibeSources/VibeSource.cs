using ButtplugSong.GUI.CustomUI;
using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GoodVibes;
using System;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

public enum VibeSourceMode
{
    Add,        //+
    Subtract,   //-
    Set,        //=
    Raise,      //>
    Lower,      //<
}
public abstract class VibeSource : GUISection, IPresetLoadable
{
    //enabled
    protected readonly Toggle _enabled;
    public bool Enabled { get => _enabled.value; set => _enabled.value = value; }

    //power
    protected readonly FloatField _power;
    protected readonly CyclingButton<VibeSourceMode> _powerMode;
    public float Power { get => _power.value / 100; set => _power.value = value * 100; }
    public VibeSourceMode PowerMode { get => _powerMode.currentMode; set => _powerMode.SetToMode(value); }

    //time
    protected readonly FloatField _time;
    protected readonly CyclingButton<VibeSourceMode> _timeMode;
    public float Time { get => _time.value; set => _time.value = value; }
    public VibeSourceMode TimeMode { get => _timeMode.currentMode; set => _timeMode.SetToMode(value); }

    protected VibeSource(string identifier, bool onByDefault, float defaultPower, float defaultTime, VibeSourceMode defaultPowerMode = VibeSourceMode.Add, VibeSourceMode defaultTimeMode = VibeSourceMode.Add) : base(identifier)
    {
        _enabled = Get<Toggle>($"BuzzOn{Identifier}");
        _enabled.SetupSaving(onByDefault, $"{Identifier}VibeSource");

        _power = Get<FloatField>($"{Identifier}Power");
        _power.SetupSaving(defaultPower).SetupValueClamping(0, 100).SetupGreyout(PowerFieldMeaningless);
        _powerMode = new CyclingButton<VibeSourceMode>($"{Identifier}PowerMode", VibeSourceMode.Add, VibeSourceModeToString);
        _powerMode.SetupSaving(defaultPowerMode);

        _time = Get<FloatField>($"{Identifier}Time");
        _time.SetupSaving(defaultTime).SetupValueClamping(0, 999).SetupGreyout(TimeFieldMeaningless);
        _timeMode = new CyclingButton<VibeSourceMode>($"{Identifier}TimeMode", VibeSourceMode.Add, VibeSourceModeToString);
        _timeMode.SetupSaving(defaultTimeMode);

        _powerMode.clicked += UpdatePowerGreyness;
        _timeMode.clicked += UpdateTimeGreyness;
    }
    public virtual void SetToPreset(Preset preset)
    {
        _enabled.Load(preset);
        _power.Load(preset);
        _powerMode.Load(preset);
        _time.Load(preset);
        _timeMode.Load(preset);
    }

    #region Button UI
    private bool PowerFieldMeaningless(float uiPower) => VibeLogic.PowerFieldMeaningless(uiPower / 100, VibeSourceModeToString(PowerMode));
    private bool TimeFieldMeaningless(float time) => VibeLogic.TimeFieldMeaningless(time, VibeSourceModeToString(TimeMode));
    private void UpdatePowerGreyness() => _power.SetClassListIf("grey-value", x => PowerFieldMeaningless(x.value));
    private void UpdateTimeGreyness() => _time.SetClassListIf("grey-value", x => TimeFieldMeaningless(x.value));
    #endregion
    public void ActivatePunctuation(float punctuateTime, string? subID = null) => Activate(0, VibeSourceMode.Add, 0, VibeSourceMode.Add, punctuateTime, subID ?? "Punctuation");

    public void Activate(string? subID = null) => Activate(Power, Time, subID);
    public void Activate(float multiplier, string? subID = null) => Activate(Power * multiplier, Time * multiplier, subID);
    public virtual void Activate(float power, float time, string? subID = null) => Activate(power, time, 0, subID); //overridden by VibeSourceWithPunctuate
    public void Activate(float power, float time, float punctuateTime, string? subID = null) => Activate(power, PowerMode, time, TimeMode, punctuateTime, subID);
    public virtual void Activate(float power, VibeSourceMode powerMode, float time, VibeSourceMode timeMode, string? subID = null) => Activate(power, powerMode, time, timeMode, 0, subID); //overridden also.
    public void Activate(float power, VibeSourceMode powerMode, float time, VibeSourceMode timeMode, float punctuateTime, string? subID = null)
    {
        string identifier = Identifier + " Source";
        if (!string.IsNullOrWhiteSpace(subID)) identifier += $" : {subID}";
        Vibe.Logic.VibeSourceActivation(identifier, power, VibeSourceModeToString(powerMode), time, VibeSourceModeToString(timeMode), punctuateTime, Power, Time);
    }


    public static VibeSourceMode StringToVibeSourceMode(string text)
    {
        return text[0] switch
        {
            '+' => VibeSourceMode.Add,
            '-' => VibeSourceMode.Subtract,
            '=' => VibeSourceMode.Set,
            '≥' => VibeSourceMode.Raise,
            '≤' => VibeSourceMode.Lower,
            _ => throw new NotImplementedException()
        };
    }
    public static string VibeSourceModeToString(VibeSourceMode mode)
    {
        return mode switch
        {
            VibeSourceMode.Add => "+",
            VibeSourceMode.Subtract => "-",
            VibeSourceMode.Set => "=",
            VibeSourceMode.Raise => "≥",
            VibeSourceMode.Lower => "≤",
            _ => throw new NotImplementedException()
        };
    }
}