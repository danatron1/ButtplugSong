using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnRumble : VibeSourceWithPunctuate
{
    public const string UncategorisedRumbleEventName = "UncategorisedRumble";

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
    //private readonly WeightedEvent UncategorisedRumbleEvent;

    public BuzzOnRumble() : base("Rumble", false, 10, 1f, false, 10)
    {
        _scaleWithWeighting = Get<Toggle>("RumbleScaleWithWeighting");
        _scaleWithWeighting.SetupSaving(true).DependsOn(_enabled);

        ModHooks.OnRumbleHook += OnRumble;

        VisualElement parent = Get<Label>("RumbleEventsListLabel").parent;

        RumbleEvents[UncategorisedRumbleEventName] = new(UncategorisedRumbleEventName, 1, _enabled, _scaleWithWeighting, false);

        // Movement - basic
        RumbleEvents["PlayFootStep"] = CreateUI("Sprinting", 0.2f, false);
        RumbleEvents["PlayWallJump"] = CreateUI("WallJump", 0.3f, true);
        RumbleEvents["HeroDash"] = CreateUI("Dashing", 0.6f, true);
        RumbleEvents["PlayDash"] = RumbleEvents["HeroDash"];
        RumbleEvents["PlayAirDash"] = RumbleEvents["HeroDash"];
        RumbleEvents["StartShuttlecock"] = CreateUI("SprintJump", 0.8f, true);
        RumbleEvents["PlaySoftLand"] = CreateUI("Landing", 0.5f, true);
        RumbleEvents["DoHardLandingEffectNoHit"] = CreateUI("HardLanding", 3, true);
        RumbleEvents["StartWallSlide"] = CreateUI("WallSlide", 0.3f, true);

        // Movement - advanced
        RumbleEvents["PlayDoubleJump"] = CreateUI("DoubleJump", 0.5f, true);
        RumbleEvents["PlayShadowDash"] = CreateUI("ShadowDash", 0.8f, true);

        // Movement - swimming
        RumbleEvents["PlaySwimEnter"] = CreateUI("SwimEnter", 0.5f, true, gapBelow: false);
        RumbleEvents["PlaySwimExit"] = CreateUI("SwimExit", 0.4f, true, gapBelow: false);
        RumbleEvents["UpdateSwimSpeed"] = CreateUI("Swimming", 0.2f, true);

        // Combat
        RumbleEvents["StartSlash"] = CreateUI("DownSlash", 0.3f, true, gapBelow: false);
        RumbleEvents["OnSlashStarting"] = CreateUI("AnySlash", 0.2f, true, gapBelow: false);
        RumbleEvents["Hit"] = CreateUI("EnemyHit", 0.4f, true, gapBelow: false);
        RumbleEvents["Burst"] = CreateUI("Burst", 0.6f, true);

        // Tools and consumables
        RumbleEvents["PlayToolThrow"] = CreateUI("UseTool", 1f, true, gapBelow: false);
        RumbleEvents["PlayConsumeFinalShake"] = CreateUI("UseConsumable", 1f, true, gapBelow: false);
        RumbleEvents["UpdateConsumeRumble"] = CreateUI("Consuming", 0.3f, true);

        // Death
        RumbleEvents["PlayHeroDeath"] = CreateUI("HeroDeath", 3f, true, gapBelow: false);
        RumbleEvents["PlayHeroHazardDeath"] = CreateUI("HazardDeath", 2f, true);

        // Progression and unlocks
        RumbleEvents["UpdateUnlockRumble"] = CreateUI("Unlocking", 0.5f, true, gapBelow: false);
        RumbleEvents["PlayFinalShake"] = CreateUI("UnlockShake", 1f, true);

        // Map and pickups
        RumbleEvents["PlaceMarker"] = CreateUI("PlaceMapMarker", 0.3f, true, gapBelow: false);
        RumbleEvents["RemoveMarker"] = CreateUI("RemoveMapMarker", 0.2f, true, gapBelow: false);
        RumbleEvents["DoCollect"] = CreateUI("Collect", 0.5f, true);

        // Environment
        RumbleEvents["Camera Shake"] = CreateUI("CameraShake", 0.5f, true, gapBelow: false);
        RumbleEvents["DoBobInternal"] = CreateUI("EnvironmentBob", 0.3f, true);

        // Menus
        RumbleEvents["PlaySubmitSound"] = CreateUI("MenuSubmitSound", 0.5f, false);
        RumbleEvents["PressCancel"] = RumbleEvents["PlaySubmitSound"];
        RumbleEvents["PressSubmit"] = RumbleEvents["PlaySubmitSound"];

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
    private readonly static HashSet<string> meaninglessRumbleNames =
    [ //these tell me nothing, so go further back into the stack trace
        "Play",
        "PlaySound",
        "PlayVibration",
        "PlayVibrationClipOneShot",
        "PlayRandomVibration",
        "PlayDelayedVibrationRoutine",
        "SpawnAndPlayOneShot",
        "SpawnAndPlayOneShotInternal",
        "StartVibration",
        "StartEmission",
        "StartSyncedEmission",
        "StartRumble",
        "GetDuration",
        "OnEnter",
        "OnEnable",
        "DoShake",
        "ActivateActions",
        "DoTransition", //hornet trans confirmed
        "EnterState",
        "SwitchState",
        "UpdateState",
        "heroAction",
        "UpdateStateChanges",
        "SetActive_Injected",
        "ProcessEvent",
        "Event",
        "SendEvent",
        "SetActive",
    ];
    private void OnRumble(string? rumbleName)
    {
        if (!Enabled) return;
        if (string.IsNullOrWhiteSpace(rumbleName) || meaninglessRumbleNames.Contains(rumbleName)) rumbleName = FigureOutRumbleName();

        if (RumbleEvents.TryGetValue(rumbleName, out WeightedEvent rumbleEvent))
        {
            ActivateRumble(rumbleEvent);
        }
        else
        {
            ActivateRumble(RumbleEvents[UncategorisedRumbleEventName], $"Uncategorised ({rumbleName})");
            Log($"Uncategorised rumble event: {rumbleName}");
        }

        string FigureOutRumbleName()
        {
            string name = "unknown";
            for (int i = 3; i <= 8; i++)
            {
                try
                {
                    name = new StackFrame(i).GetMethod().Name;
                }
                catch
                {
                    break;
                }
                if (name.Contains(':')) name = name[name.IndexOf(':')..].Trim(':', '>', '<');
                if (!meaninglessRumbleNames.Contains(name)) return name;
            }
            return UncategorisedRumbleEventName;
        }
    }
    private void ActivateRumble(WeightedEvent rumbleEvent, string? subID = null)
    {
        if (!rumbleEvent.Enabled) return;
        if (!ScaleWithWeighting) Activate(subID ?? rumbleEvent.EnabledText);
        else Activate(rumbleEvent.Weight, subID ?? rumbleEvent.EnabledText);
    }
}
