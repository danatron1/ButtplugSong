using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class MinimumNotMoving : MinimumBase
{
    private readonly FloatField _minNotMovingDelay;
    public float MinNotMovingDelay { get => _minNotMovingDelay.value; set => _minNotMovingDelay.value = value; }
    public float NotMovingTime = 0;
    public MinimumNotMoving() : base("NotMoving", false, 10)
    {
        Vibe.NeedsUpdate += Update;
        _minNotMovingDelay = Get<FloatField>("MinNotMovingDelay");
        _minNotMovingDelay.SetupSaving(1).SetupValueClamping(0, 999).SetupGreyout(x => x == 0).DependsOn(_enabled, _minimumsEnabled);
    }
    private void Update(float realTime, float timerTime)
    {
        if (Moving()) NotMovingTime = 0;
        else NotMovingTime += realTime; //should this use timerTime? Probably, but I woke up on the evil side of bed today.
    }
    public override bool IsRelevant() => NotMovingTime > MinNotMovingDelay;
    public static bool Moving() => hero != null && hero.current_velocity.magnitude > float.Epsilon;
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _minNotMovingDelay.Load(preset);
    }
}