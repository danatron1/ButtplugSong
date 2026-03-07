using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnHeal : VibeSourceWithPunctuate
{
    //no healing while vibing
    private readonly Toggle _noHealingWhileVibing;
    public bool NoHealingWhileVibing { get => _noHealingWhileVibing.value; set => _noHealingWhileVibing.value = value; }
    public bool NoHealing => NoHealingWhileVibing && Vibe.Logic.IsVibing;

    //interrupted
    private readonly Toggle _interruption;
    private readonly FloatField _interruptionTime;
    public bool Interruption { get => _interruption.value; set => _interruption.value = value; }
    public float InterruptionTime { get => _interruptionTime.value; set => _interruptionTime.value = value; }

    public BuzzOnHeal() : base("Heal", true, 10, 5)
    {
        ModHooks.OnAddHealthHook += Healed;
        ModHooks.OnBindInterruptedHook += HealInterrupted;

        _noHealingWhileVibing = Get<Toggle>("NoHealingWhileVibing");
        _noHealingWhileVibing.SetupSaving(false);
        _interruption = Get<Toggle>("Interruption");
        _interruption.SetupSaving(true);
        _interruptionTime = Get<FloatField>("InterruptionTime");
        _interruptionTime.SetupSaving(2).SetupValueClamping(0, 999).SetupGreyout(x => x == 0).DependsOn(_interruption);
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _noHealingWhileVibing.Load(preset);
        _interruption.Load(preset);
        _interruptionTime.Load(preset);
    }
    private int Healed(PlayerData data, int amount)
    {
        if (NoHealingWhileVibing && Vibe.Logic.IsVibing) amount = 0;
        if (!Enabled) return amount;
        Activate();
        return amount;
    }

    private DateTime _lastHealInterruptionTime = DateTime.MinValue;
    private static TimeSpan _healInterruptionCooldown = TimeSpan.FromSeconds(1);
    private void HealInterrupted(HeroController controller)
    {
        if (!Interruption) return; //setting disabled
        if (DateTime.Now - _lastHealInterruptionTime < _healInterruptionCooldown) return; //sometimes Silksong double triggers this
        if (controller.WillDoBellBindHit()) ActivatePunctuation(InterruptionTime / 2); //ooooh spooky undocumented mechanic
        else ActivatePunctuation(InterruptionTime);
        _lastHealInterruptionTime = DateTime.Now;
    }

    protected override string _punctuateReminderDescription => "healing";
}
