using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnStrike : VibeSourceWithPunctuate
{
    private readonly Toggle _scaleWithDamageDealt;
    public bool ScaleWithDamageDealt { get => _scaleWithDamageDealt.value; set => _scaleWithDamageDealt.value = value; }

    private readonly Toggle _dealBonusDamage;
    public bool DealBonusDamage { get => _dealBonusDamage.value; set => _dealBonusDamage.value = value; }

    protected override string _punctuateReminderDescription => "dealing damage";
    public BuzzOnStrike() : base("Strike", false, 2.4f, 1, false, 10)
    {
        ModHooks.OnDealDamageHook += DealtDamage;

        _scaleWithDamageDealt = Get<Toggle>("ScaleWithDamageDealt");
        _scaleWithDamageDealt.SetupSaving(true).DependsOn(_enabled);
        _dealBonusDamage = Get<Toggle>("DealBonusDamage");
        _dealBonusDamage.SetupSaving(false);

    }

    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _scaleWithDamageDealt.Load(preset);
        _dealBonusDamage.Load(preset);
    }
    private int DealtDamage(HealthManager manager, HitInstance instance)
    {
        if (!instance.IsHeroDamage || !instance.IsNailDamage) return instance.DamageDealt; //not relevant

        float damageMultiplier = DealBonusDamage ? 1 + Vibe.Logic.TargetPower : 1; //calculate BEFORE adding the power from this hit.

        if (Enabled)
        {
            if (ScaleWithDamageDealt) Activate(Power * instance.DamageDealt / 10, Time);
            else Activate();
        }

        return (int)(instance.DamageDealt * damageMultiplier);
    }
}
