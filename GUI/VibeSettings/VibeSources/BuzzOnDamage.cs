using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnDamage : VibeSourceWithPunctuate
{
    /*New vibe source checklist:
     * PARAMETERS:
     * Add a private readonly UI element reference (and a public getter/setter) for every setting specific to this vibe source, e.g:
     *      private readonly Toggle _scaleWithDamage;
     *      public bool ScaleWithDamage { get => _scaleWithDamage.value; set => _scaleWithDamage.value = value; }
     * CONSTRUCTOR: 
     * pass the vibe reference and identifier string to the base. Do not take an identifier as reference, e.g.
     *      public BuzzOnDamage(VibeManager vibe) : base(vibe, "Damage")
     * Add relevant modhooks in constructor, implement as you see fit, e.g:
     *      ModHooks.TakeHealthHook += PlayerHit
     * for each UI element reference, populate it by querying the UI, then set it up for settings saving (else it won't carry between reloads) e.g:
     *      _scaleWithDamage = Get<Toggle>("ScaleWithDamage");
     *      _scaleWithDamage.SetupSaving(true);
     * set dependencies to other UI elements like this;
     *      _vulnerableVibingThreshold.SetupValueClamping(0, 100).DependsOn(_vulnerableVibing);
     * OVERRIDES:
     * Override _punctuateReminderDescription. Should follow the form of "The first second after ___ is at max power"
     * Override SetToPreset, and make sure to include a callback to the base, and a LOAD for each setting;
     *      base.SetToPreset(preset);
     *      _scaleWithDamage.Load(preset);
     */

    //Scale with damage
    private readonly Toggle _scaleWithDamage;
    public bool ScaleWithDamage { get => _scaleWithDamage.value; set => _scaleWithDamage.value = value; }

    //vulnerable vibing
    private readonly Toggle _vulnerableVibing;
    private readonly FloatField _vulnerableVibingThreshold;
    public bool VulnerableVibingEnabled { get => _vulnerableVibing.value; set => _vulnerableVibing.value = value; }
    public float VulnerableVibingThreshold { get => _vulnerableVibingThreshold.value / 100; set => _vulnerableVibingThreshold.value = value * 100; }
    public bool VulnerableVibingActive => VulnerableVibingEnabled && Vibe.Logic.TargetPower >= VulnerableVibingThreshold;

    protected override string _punctuateReminderDescription => "taking damage";

    public BuzzOnDamage() : base("Damage", true, 20, 5, true)
    {
        ModHooks.OnTakeDamageHook += PlayerHit;

        _scaleWithDamage = Get<Toggle>("ScaleWithDamage");
        _scaleWithDamage.SetupSaving(true).DependsOn(_enabled);
        _vulnerableVibing = Get<Toggle>("VulnerableVibing");
        _vulnerableVibing.SetupSaving(false);
        _vulnerableVibingThreshold = Get<FloatField>("VulnerableVibingThreshold");
        _vulnerableVibingThreshold.SetupSaving(50).SetupValueClamping(0, 100).DependsOn(_vulnerableVibing);
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _scaleWithDamage.Load(preset);
        _vulnerableVibing.Load(preset);
        _vulnerableVibingThreshold.Load(preset);
    }
    public int PlayerHit(PlayerData data, int damage)
    {
        if (Vibe.Logic.IsVibing && VulnerableVibingActive) damage *= 2;
        if (!Enabled) return damage;
        string subID = damage > 1 ? damage.ToString() : string.Empty;
        if (ScaleWithDamage) Activate(Power * damage, Time * damage, subID);
        else Activate(subID);
        return damage;
    }
}
