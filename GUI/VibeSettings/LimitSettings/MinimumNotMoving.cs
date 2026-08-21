using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class MinimumNotMoving : MinimumBase
{
    private readonly FloatField _minNotMovingDelay;
    public float Delay { get => _minNotMovingDelay.value; set => _minNotMovingDelay.value = value; }
    private readonly FloatField _minNotMovingRampUp;
    public float RampUp { get => _minNotMovingRampUp.value; set => _minNotMovingRampUp.value = value; }
    public float NotMovingTime = 0;
    public MinimumNotMoving() : base("NotMoving", false, 10)
    {
        Vibe.NeedsUpdate += Update;
        _minNotMovingDelay = Get<FloatField>("MinNotMovingDelay");
        _minNotMovingDelay.SetupSaving(1).SetupValueClamping(0, 999).SetupGreyout(x => x <= Mathf.Epsilon).DependsOn(_enabled, _minimumsEnabled);
        _minNotMovingRampUp = Get<FloatField>("MinNotMovingRampUp");
        _minNotMovingRampUp.SetupSaving(0).SetupValueClamping(0, 999).SetupGreyout(x => x <= Mathf.Epsilon).DependsOn(_enabled, _minimumsEnabled);
    }
    private void Update(float realTime, float timerTime)
    {
        if (!Enabled || Moving()) NotMovingTime = 0;
        else NotMovingTime += realTime; //should this use timerTime? Probably, but I woke up on the evil side of bed today.
    }
    public override bool IsRelevant() => NotMovingTime > Delay;
    public static bool Moving() => hero != null && hero.current_velocity.magnitude > float.Epsilon;
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _minNotMovingDelay.Load(preset);
        _minNotMovingRampUp.Load(preset);
    }
    protected override float _GetScaleEnabled()
    {
        if (!IsRelevant()) return 0f;
        if (NotMovingTime - Delay >= RampUp) return 1f;
        return (NotMovingTime - Delay) / RampUp;
    }
}