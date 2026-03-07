using ButtplugSong.GUI.CustomUI;
using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GoodVibes;
using UnityEngine;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings;

enum CountdownMode //doesn't populate the UI. If you change one, change the other manually. I know, boo.
{
    Always,
    Default,
    InGame,
    Unpaused,
    SuperHot,
    Never
}
internal class TimerSettings : GUISection, IPresetLoadable
{
    private readonly DropdownField _timerCountdownMode;
    private readonly Label _countdownModeDescription;
    private readonly Toggle _vibeWhileTimerPaused;
    private readonly FloatField _afterZeroPunctuate;
    private readonly Label _afterZeroPunctuateLabel;
    private readonly FloatField _afterZeroPower;
    private readonly Label _percentagesReminder;
    private readonly Label _zeroPowerWarning;
    private readonly FloatField _afterZeroTimer;
    private readonly CyclingButton<AfterZeroMode> _afterZeroPowerMode;
    public int TimerCountdownModeIndex { get => _timerCountdownMode.index; set => _timerCountdownMode.index = value; }
    public CountdownMode TimerCountdownMode => (CountdownMode)TimerCountdownModeIndex;
    public TimerSettings() : base("Timer")
    {
        _timerCountdownMode = Get<DropdownField>("TimerCountdownMode");
        _timerCountdownMode.PopulateDropdown<CountdownMode>().RegisterDropdownChangedCallback<CountdownMode>(CountdownModeChanged).SetupSaving(CountdownMode.Default.ToString());
        _countdownModeDescription = Get<Label>("CountdownModeDescription");
        _vibeWhileTimerPaused = Get<Toggle>("VibeWhileTimerPaused");
        _vibeWhileTimerPaused.SetupSaving(true).RegisterValueChangedCallback(VibeWhilePausedChanged);
        _afterZeroPunctuate = Get<FloatField>("AfterZeroPunctuate");
        _afterZeroPunctuate.SetupSaving(0).SetupValueClamping(0, 999).SetupGreyout(x => x == 0).RegisterValueChangedCallback(UpdateAfterZeroPunctuate);
        _afterZeroPunctuateLabel = Get<Label>("AfterZeroPunctuateLabel");
        _afterZeroTimer = Get<FloatField>("AfterZeroTimer");
        _afterZeroTimer.SetupSaving(0).SetupValueClamping(0, 999).SetupGreyout(x => x == 0).RegisterValueChangedCallback(AfterZeroTimerChanged);
        _percentagesReminder = Get<Label>("PercentagesReminder");
        _zeroPowerWarning = Get<Label>("ZeroPowerWarning");
        _afterZeroPower = Get<FloatField>("AfterZeroPower");
        _afterZeroPower.SetupSaving(100).SetupValueClamping(0, 100).SetupGreyout(AfterZeroPowerMeaningless).RegisterValueChangedCallback(AfterZeroPowerChanged);
        _afterZeroPowerMode = new("AfterZeroPowerMode", AfterZeroMode.Subtract, AfterZeroModeTextSelector);
        _afterZeroPowerMode.SetupSaving(AfterZeroMode.Subtract);

        _afterZeroPowerMode.clicked += AfterZeroPowerModeChanged;
    }
    public void SetToPreset(Preset preset)
    {
        _timerCountdownMode.Load(preset);
        _vibeWhileTimerPaused.Load(preset);
        _afterZeroPunctuate.Load(preset);
        _afterZeroTimer.Load(preset);
        _afterZeroPower.Load(preset);
        _afterZeroPowerMode.Load(preset);
    }
    private void AfterZeroTimerChanged(ChangeEvent<float> evt)
    {
        Vibe.Logic.afterZeroTimer = evt.newValue;
    }

    private void AfterZeroPowerModeChanged()
    {
        Vibe.Logic.afterZeroMode = _afterZeroPowerMode.currentMode;
        UpdateAfterZeroPowerReminderLabels();
    }
    private void AfterZeroPowerChanged(ChangeEvent<float> evt)
    {
        Vibe.Logic.afterZeroPowerChange = evt.newValue;
        UpdateAfterZeroPowerReminderLabels();
    }
    private void UpdateAfterZeroPowerReminderLabels()
    {
        _percentagesReminder.SetClassListIf("hide", x => _afterZeroPowerMode.currentMode != AfterZeroMode.Multiply || _afterZeroPower.value > 1);
        _zeroPowerWarning.SetClassListIf("hide", x => !AfterZeroPowerMeaningless(_afterZeroPower.value));
    }
    private bool AfterZeroPowerMeaningless(float value)
    {
        return _afterZeroPowerMode.currentMode switch
        {
            AfterZeroMode.Subtract => value <= 0,
            AfterZeroMode.Multiply => value >= 100,
            _ => false
        };
    }
    private void VibeWhilePausedChanged(ChangeEvent<bool> evt)
    {
        Vibe.Logic.vibeWhileTimerPaused = evt.newValue;
    }

    private void UpdateAfterZeroPunctuate(ChangeEvent<float> evt)
    {
        if (evt.newValue < 0) return;
        _afterZeroPunctuateLabel.text = $"Punctuate with {evt.newValue}s of max power, then";
        Vibe.Logic.afterZeroPunctuate = evt.newValue;
    }

    private void CountdownModeChanged(string newValue, bool isEnum, CountdownMode type)
    {
        if (isEnum) _countdownModeDescription.text = CountdownModeExplanation(type);
    }

    private static string AfterZeroModeTextSelector(AfterZeroMode mode)
    {
        return mode switch
        {
            AfterZeroMode.Subtract => "-",
            AfterZeroMode.Multiply => "x",
            _ => "?"
        };
    }

    internal float GetTimerCountdown(float unscaledDeltaTime)
    {
        if (!ShouldTimerCountDown(TimerCountdownMode)) return 0;
        if (TimerCountdownMode == CountdownMode.SuperHot)
        {
            Vibe.UI.DebugInfo.text = HeroController.instance.current_velocity.magnitude.ToString();
        }
        return unscaledDeltaTime;
    }
    private static bool ShouldTimerCountDown(CountdownMode mode)
    {
        return mode switch
        {
            CountdownMode.Always => true,
            CountdownMode.Default => Time.timeScale > float.Epsilon,
            CountdownMode.InGame => HeroController.instance != null && HeroController.instance.isGameplayScene,
            CountdownMode.Unpaused => HeroController.instance != null && HeroController.instance.isGameplayScene && !HeroController.instance.IsPaused(),
            CountdownMode.SuperHot => ShouldTimerCountDown(CountdownMode.Unpaused) && HeroController.instance.current_velocity.magnitude > float.Epsilon,
            CountdownMode.Never => false,
            _ => true,
        };
    }
    private static string CountdownModeExplanation(CountdownMode mode)
    {
        return mode switch
        {
            CountdownMode.Always => "\"Always\": Always counts down (unless the game freezes, i.e. when loading).",
            CountdownMode.Default => "\"Default\": Counts down whenever the game simulates time (includes main menu, excludes pausing).",
            CountdownMode.InGame => "\"In Game\": excludes non-gameplay scenes like the main menu, but includes pausing.",
            CountdownMode.Unpaused => "\"Unpaused\": stops the timer unless the game is playing (i.e. unpaused).",
            CountdownMode.SuperHot => "\"Super Hot\": the timer only moves when you move.",
            CountdownMode.Never => "\"Never\": for if you want to control power/time reduction via subtractive vibe sources.",
            _ => $"DESCRIPTION FOR COUNTDOWN MODE {mode} NOT IMPLEMENTED"
        };
    }
}
