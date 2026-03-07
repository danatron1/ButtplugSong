using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GoodVibes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class LimitsSettings : GUISection, IPresetLoadable
{
    protected readonly FloatField _masterMaxPower;
    protected readonly FloatField _masterMaxTimer;
    internal readonly List<MinimumBase> Minimums;
    internal readonly Toggle _minimumsEnabled;
    protected readonly Toggle _minimumsStack;
    protected readonly Label _minimumsStackLabel;

    public float MasterMaxPower { get => _masterMaxPower.value / 100; set => _masterMaxPower.value = value * 100; }
    public float MasterMaxTimer { get => _masterMaxTimer.value; set => _masterMaxTimer.value = value; }
    public LimitsSettings() : base("Limits")
    {
        _masterMaxPower = Get<FloatField>($"MasterMaxPower");
        _masterMaxPower.SetupSaving(100).SetupValueClamping(0, 100).SetupGreyout(x => x == 100).RegisterValueChangedCallback(MasterMaxPowerChanged);
        _masterMaxTimer = Get<FloatField>($"MasterMaxTimer");
        _masterMaxTimer.SetupSaving(300).SetupValueClamping(0, VibeLogic._actualHardTimerMaximum).RegisterValueChangedCallback(MasterMaxTimerChanged);
        _minimumsEnabled = Get<Toggle>("EnableMinimums");
        _minimumsEnabled.SetupSaving(true);
        _minimumsStack = Get<Toggle>("MinimumsStack");
        _minimumsStack.SetupSaving(false).DependsOn(_minimumsEnabled).RegisterValueChangedCallback(MinimumsStackChanged);
        _minimumsStackLabel = Get<Label>("MinimumsStackLabel");

        MinimumBase._minimumsEnabled = _minimumsEnabled; //may not be necessary but playing it safe

        Minimums =
        [
            new MinimumDefault(),
            new MinimumBelowFullHealth(),
            new MinimumTouchingGround(),
            new MinimumNotMoving(),
            new MinimumSwimming(),
            new MinimumSprinting(),
            new MinimumSittingAtBench(),
            new MinimumSoaring(),
            new MinimumNaked(),
            new MinimumCourier(),
            new MinimumMaggoted(),
            new MinimumFreezing(),
            new MinimumCursed(),
        ];

        Vibe.NeedsUpdate += Update;
    }
    public void SetToPreset(Preset preset)
    {
        _masterMaxPower.Load(preset);
        _masterMaxTimer.Load(preset);
        _minimumsEnabled.Load(preset);
        _minimumsStack.Load(preset);
        foreach (MinimumBase minimum in Minimums)
        {
            minimum.SetToPreset(preset);
        }
    }
    private void MinimumsStackChanged(ChangeEvent<bool> evt)
    {
        _minimumsStackLabel.text = evt.newValue
            ? "While multiple minimums are active, their minimum values are combined additively."
            : "While multiple minimums are active, the greatest minimum among them is used.";
    }
    private void Update(float realTime, float timerTime)
    {
        float minPower = GetMinPower();
        if (minPower != Vibe.Logic.MinPower) Vibe.Logic.MinPower = minPower;
    }
    private float GetMinPower()
    {
        float min = _minimumsStack.value ? Minimums.Sum(x => x.Minimum) : Minimums.Max(x => x.Minimum);
        return min.Clamp(0, Vibe.Logic.MaxPower); //never exceed the maximum
    }

    private void MasterMaxPowerChanged(ChangeEvent<float> evt) => Vibe.Logic.MaxPower = MasterMaxPower;
    private void MasterMaxTimerChanged(ChangeEvent<float> evt) => Vibe.Logic.MaxTimer = MasterMaxTimer;

}
