using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

public abstract class VibeSourceWithPunctuate : VibeSource
{
    //punctuate
    protected readonly Toggle _punctuate;
    protected readonly SliderInt _punctuateTime;
    protected readonly Label _punctuateReminder;
    public bool Punctuate { get => _punctuate.value; set => _punctuate.value = value; }
    public float PunctuateTime { get => (float)_punctuateTime.value / 100; set => _punctuateTime.value = (int)(value * 100); }
    protected abstract string _punctuateReminderDescription { get; }

    protected VibeSourceWithPunctuate(string identifier, bool onByDefault, float defaultPower, float defaultTime,
        bool defaultPunctuate = false, int defaultPunctuateTime = 30, VibeSourceMode defaultPowerMode = VibeSourceMode.Add, VibeSourceMode defaultTimeMode = VibeSourceMode.Add)
        : base(identifier, onByDefault, defaultPower, defaultTime, defaultPowerMode, defaultTimeMode)
    {
        _punctuate = Get<Toggle>($"Punctuate{Identifier}");
        _punctuate.SetupSaving(defaultPunctuate, $"{Identifier}Punctuate").DependsOn(_enabled);
        _punctuateTime = Get<SliderInt>($"Punctuate{Identifier}-Slider");
        _punctuateTime.SetupSaving(defaultPunctuateTime).DependsOn(_punctuate, _enabled).RegisterValueChangedCallback(PunctuateSliderChangedEvent);
        _punctuateReminder = Get<Label>($"Punctuate{Identifier}-Reminder");
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _punctuate.Load(preset);
        _punctuateTime.Load(preset);
    }
    private void PunctuateSliderChangedEvent(ChangeEvent<int> evt)
    {
        _punctuateReminder.text = $"Adds {(evt.newValue == 0 ? "no" : "some")} punch. " +
            $"The first {(evt.newValue == 100 ? "1 second" : $"{(float)evt.newValue / 100} seconds")} after " +
            $"{_punctuateReminderDescription} {(evt.newValue == 100 ? "is" : "are")} at max power.";
    }
    public void ActivatePunctuation(string? subID = null) => ActivatePunctuation(PunctuateTime, subID);
    public override void Activate(float power, float time, string? subID = null) => Activate(power, time, Punctuate ? PunctuateTime : 0, subID);
    public override void Activate(float power, VibeSourceMode powerMode, float time, VibeSourceMode timeMode, string? subID = null)
    {
        Activate(power, powerMode, time, timeMode, Punctuate ? PunctuateTime : 0, subID);
    }
}
