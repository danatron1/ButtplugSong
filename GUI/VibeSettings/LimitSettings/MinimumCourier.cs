using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class MinimumCourier : MinimumBase
{
    private readonly Toggle _minCourierPunctuate;
    private readonly FloatField _minCourierPunctuateTime;
    public MinimumCourier() : base("Courier", true, 15)
    {
        ModHooks.OnCourierBreakItemHook += ItemBroken;

        _minCourierPunctuate = Get<Toggle>("MinCourierPunctuate");
        _minCourierPunctuateTime = Get<FloatField>("MinCourierPunctuateTime");

        _minCourierPunctuate.SetupSaving(true).DependsOn(_enabled, _minimumsEnabled);
        _minCourierPunctuateTime.SetupSaving(1.7f).SetupValueClamping(0, 999).SetupGreyout(x => x == 0).DependsOn(_enabled, _minCourierPunctuate, _minimumsEnabled);
    }
    public override bool IsRelevant()
    {
        foreach (var quest in QuestManager.GetActiveQuests())
        {
            foreach (var target in quest.TargetsAndCounters)
            {
                if (target.target.Counter is DeliveryQuestItem) return true;
            }
        }
        return false;
    }
    private void ItemBroken(DeliveryQuestItem item)
    {
        if (_enabled.value && _minCourierPunctuate.value && _minCourierPunctuateTime.value > 0)
        {
            Vibe.Logic.VibeSourceActivation("Courier Broken Source", 0, "+", 0, "+", _minCourierPunctuateTime.value);
        }
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _minCourierPunctuate.Load(preset);
        _minCourierPunctuateTime.Load(preset);
    }
}