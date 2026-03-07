using ButtplugSong.GUI.VibeSettings.VibeSources;
using ButtplugSong.Helper;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class MinimumDefault : MinimumBase
{
    public Label _reminderText;
    public MinimumDefault() : base("Default", false, 0)
    {
        _reminderText = Get<Label>("MinimumDefaultReminder");
        _enabled.RegisterValueChangedCallback(UpdateReminderText);
        _minimumsEnabled.RegisterValueChangedCallback(UpdateReminderText);
    }
    public override bool IsRelevant() => true;
    private BuzzOnDeath? _buzzOnDeath
    {
        get
        {
            field ??= (BuzzOnDeath?)Vibe.UI.Sources.FirstOrDefault(x => x is BuzzOnDeath);
            return field;
        }
    } = null;
    public void UpdateReminderText<T>(ChangeEvent<T> evt) => UpdateReminderText();
    public void UpdateReminderText()
    {
        _reminderText.SetClassListIf("hide", x => _buzzOnDeath is null || !_buzzOnDeath.RaiseMinimum || (_minimumsEnabled.value && _enabled.value));
        _buzzOnDeath?._raiseMinimumReminder.SetClassListIf("hide", x => _minimumsEnabled.value && _enabled.value);
    }
    internal void RaiseMinimumDeath(float raiseAmount)
    {
        _minimumsEnabled.value = true;
        _enabled.value = true;
        MinAmount += raiseAmount;
        UpdateReminderText();
    }
}