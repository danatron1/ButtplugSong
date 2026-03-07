using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnRumble : VibeSourceWithPunctuate
{

    /*New vibe source checklist:
     * Add relevant modhooks in constructor, implement as you see fit, e.g:
     *      ModHooks.TakeHealthHook += PlayerHit
     * for each UI element reference, populate it by querying the UI, e.g:
     *      _scaleWithDamage = Get<Toggle>("ScaleWithDamage");
     * set dependencies to other UI elements like this;
     *      _vulnerableVibingThreshold.SetupValueClamping(0, 100).DependsOn(_vulnerableVibing);
     * OVERRIDES:
     * Override _punctuateReminderDescription. Should follow the form of "The first second after ___ is at max power"
     */
    private readonly Toggle _scaleWithWeighting;
    public bool ScaleWithWeighting { get => _scaleWithWeighting.value; set => _scaleWithWeighting.value = value; }
    protected override string _punctuateReminderDescription => "a rumble";

    private readonly Dictionary<string, WeightedEvent> RumbleEvents = new();
    private readonly WeightedEvent UncategorisedRumbleEvent;

    public BuzzOnRumble() : base("Rumble", false, 10, 1, false, 10)
    {
        _scaleWithWeighting = Get<Toggle>("RumbleScaleWithWeighting");
        _scaleWithWeighting.SetupSaving(true).DependsOn(_enabled);

        ModHooks.OnRumbleHook += OnRumble;

        VisualElement parent = Get<Label>("RumbleEventsListLabel").parent;
        int id = 0;

        UncategorisedRumbleEvent = new("UncategorisedRumble", 1, _enabled, _scaleWithWeighting, false);
        RumbleEvents["PlayFootStep"] =      CreateUI("Walking", 0.1f, false);
        RumbleEvents["PlayWallJump"] =      CreateUI("WallJump", 0.5f, true);
        RumbleEvents["PlayAirDash"] =       CreateUI("Dashing", 1, true);
        RumbleEvents["StartShuttlecock"] =  CreateUI("SprintJump", 1, true);
        RumbleEvents["PlaySoftLand"] =      CreateUI("Landing", 0.5f, true);
        RumbleEvents["UNKNOWNIDENTIFIER"] = CreateUI("HardLanding", 5, true);
        RumbleEvents["Camera Shake"] =      CreateUI("CameraShake", 5, true);
        RumbleEvents["PlaySubmitSound"] =   CreateUI("MenuSubmitSound", 0.5f, false);
        RumbleEvents["PressCancel"] =       RumbleEvents["PlaySubmitSound"];
        RumbleEvents["PressSubmit"] =       RumbleEvents["PlaySubmitSound"];
        RumbleEvents["PlayToolThrow"] =     CreateUI("UseTool", 0.5f, true);
        RumbleEvents["PlayConsumeFinalShake"] = CreateUI("UseConsumable", 0.5f, true);

        WeightedEvent CreateUI(string identifier, float defaultWeight, bool defaultOn, bool gapBelow = true, string? categoryLabel = null)
        {
            return WeightedEvent.CreateWithUI(identifier, defaultWeight, defaultOn, parent, this, _enabled, _scaleWithWeighting, !gapBelow, categoryLabel);
        }
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _scaleWithWeighting.Load(preset);
        foreach (var item in RumbleEvents)
        {
            item.Value.Load(preset);
        }
    }
    private void OnRumble(string rumbleName)
    {
        if (RumbleEvents.TryGetValue(rumbleName, out WeightedEvent rumbleEvent))
        {
            ActivateRumble(rumbleEvent);
        }
        else
        {
            ActivateRumble(UncategorisedRumbleEvent, $"Uncategorised ({rumbleName})");
            Log($"Uncategorised rumble event: {rumbleName}");
        }
    }
    private void ActivateRumble(WeightedEvent rumbleEvent, string? subID = null)
    {
        if (!rumbleEvent.Enabled) return;
        if (!ScaleWithWeighting) Activate(subID ?? rumbleEvent.EnabledText);
        else Activate(rumbleEvent.Weight, subID ?? rumbleEvent.EnabledText);
    }
}
