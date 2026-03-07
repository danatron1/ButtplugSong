using ButtplugSong.GUI.VibeSettings.LimitSettings;
using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnDeath : VibeSourceWithPunctuate
{
    private readonly Toggle _includeNonLethal;
    public bool IncludeNonLethal { get => _includeNonLethal.value; set => _includeNonLethal.value = value; }
    private readonly Toggle _raceTheTimer;
    public bool RaceTheTimer { get => _raceTheTimer.value; set => _raceTheTimer.value = value; }
    public bool RaceTheTimerActive = false;

    private readonly Toggle _corpseRunFail;
    private readonly FloatField _corpseRunFailMultiplier;
    public bool CorpseRunFail { get => _corpseRunFail.value; set => _corpseRunFail.value = value; }
    public float CorpseRunFailMultiplier { get => _corpseRunFailMultiplier.value; set => _corpseRunFailMultiplier.value = value; }

    private readonly Toggle _steelSoul;
    private readonly FloatField _steelSoulMultiplier;
    public bool SteelSoul { get => _steelSoul.value; set => _steelSoul.value = value; }
    public float SteelSoulMultiplier { get => _steelSoulMultiplier.value; set => _steelSoulMultiplier.value = value; }

    private readonly Toggle _raiseMinimum;
    private readonly FloatField _raiseMinimumAmount;
    internal readonly Label _raiseMinimumReminder;
    public bool RaiseMinimum { get => _raiseMinimum.value; set => _raiseMinimum.value = value; }
    public float RaiseMinimumAmount { get => _raiseMinimumAmount.value / 100; set => _raiseMinimumAmount.value = value * 100; }
    private readonly Toggle _deathDice;
    private readonly Label _mostRecentRoll;
    public bool DeathDice { get => _deathDice.value; set => _deathDice.value = value; }

    public BuzzOnDeath() : base("Death", true, 80, 10, true, 60)
    {
        ModHooks.OnBeforeDeathHook += OnDeathBefore;
        ModHooks.OnAfterDeathHook += OnDeathAfter;
        ModHooks.OnGetCocoonHook += OnGetCocoon;
        Vibe.Logic.TimerHitZero += OnTimerZero;

        _includeNonLethal = Get<Toggle>("IncludeNonLethal");
        _includeNonLethal.SetupSaving(true).DependsOn(_enabled);
        _raceTheTimer = Get<Toggle>("RaceTheTimer");
        _raceTheTimer.SetupSaving(false).DependsOn(_enabled, x => Time > 0);
        _corpseRunFail = Get<Toggle>("CorpseRunFail");
        _corpseRunFail.SetupSaving(false);
        _corpseRunFailMultiplier = Get<FloatField>("CorpseRunFailMultiplier");
        _corpseRunFailMultiplier.SetupSaving(2).DependsOn(_corpseRunFail).SetupValueClamping(0, 999).SetupGreyout(x => x == 1);
        _steelSoul = Get<Toggle>("SteelSoul");
        _steelSoul.SetupSaving(false);
        _steelSoulMultiplier = Get<FloatField>("SteelSoulMultiplier");
        _steelSoulMultiplier.SetupSaving(10).DependsOn(_steelSoul).SetupValueClamping(0, 999).SetupGreyout(x => x == 1);
        _raiseMinimum = Get<Toggle>("RaiseMinimum");//Wage
        _raiseMinimum.SetupSaving(false).RegisterValueChangedCallback(RaiseMinimumChanged);
        _raiseMinimumAmount = Get<FloatField>("RaiseMinimumAmount");
        _raiseMinimumAmount.SetupSaving(5).DependsOn(_raiseMinimum).SetupValueClamping(0, 100).SetupGreyout(x => x == 0);
        _raiseMinimumReminder = Get<Label>("RaiseMinimum-RelatedReminder");
        _deathDice = Get<Toggle>("DeathDice");
        _deathDice.SetupSaving(false);
        _mostRecentRoll = Get<Label>("MostRecentRoll");
    }

    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _includeNonLethal.Load(preset);
        _raceTheTimer.Load(preset);
        _corpseRunFail.Load(preset);
        _corpseRunFailMultiplier.Load(preset);
        _steelSoul.Load(preset);
        _steelSoulMultiplier.Load(preset);
        _raiseMinimum.Load(preset);
        _raiseMinimumAmount.Load(preset);
        _deathDice.Load(preset);
    }

    private static bool HasCocoonOut(PlayerData data) => !string.IsNullOrEmpty(data.HeroCorpseScene);
    private static bool IsSteelSoulMode(PlayerData data) => data.permadeathMode is GlobalEnums.PermadeathModes.On or GlobalEnums.PermadeathModes.Dead;
    private MinimumDefault? _minimumDefault
    {
        get
        {
            field ??= (MinimumDefault?)Vibe.UI.Limits.Minimums.FirstOrDefault(x => x is MinimumDefault);
            return field;
        }
    } = null;
    private void RaiseMinimumChanged<T>(ChangeEvent<T> evt) => _minimumDefault?.UpdateReminderText();
    private void OnDeathBefore(HeroController manager, bool nonLethal, bool frostDeath)
    {
        if (manager.gm.IsMemoryScene()) nonLethal = true;
        if (Enabled && (IncludeNonLethal || !nonLethal)) Activate();
        PlayerData data = manager.playerData;

        if (SteelSoul && IsSteelSoulMode(data)) Vibe.Logic.MultiplyTimer(SteelSoulMultiplier, "Steel Soul Death");
        if (CorpseRunFail && HasCocoonOut(data) && !IsSteelSoulMode(data)) Vibe.Logic.MultiplyTimer(CorpseRunFailMultiplier, "Corpse Run Death");
        if (RaiseMinimum) _minimumDefault?.RaiseMinimumDeath(RaiseMinimumAmount);
        if (DeathDice) DeathRoll();
    }
    private void DeathRoll()
    {
        int roll = ExtHelper.rng.Next(20) + 1;
        _mostRecentRoll.RemoveFromClassList("hide");
        _mostRecentRoll.text = $"Most recent roll: {roll}";
        Vibe.UI.DisplayDeathDice(roll);
    }
    private void OnDeathAfter(GameManager gm)
    {
        if (RaceTheTimer && !IsSteelSoulMode(gm.playerData) && HasCocoonOut(gm.playerData))
        {
            StartRaceTheTimer();
        }
    }
    private void StartRaceTheTimer()
    {
        RaceTheTimerActive = true;
        UpdateRaceTheTimerGraphic();
    }
    private void OnGetCocoon(HeroController controller)
    {
        if (RaceTheTimerActive) EndRaceTheTimer(false);
    }
    private void OnTimerZero()
    {
        if (RaceTheTimerActive) EndRaceTheTimer(true);
    }
    private void EndRaceTheTimer(bool timerHitZero)
    {
        if (RaceTheTimerActive && timerHitZero && HasCocoonOut(PlayerData.instance))
        {
            int rosariesLost = DeleteCorpse();
            Vibe.UI.LogActivity("Timer Hit Zero : Race the Timer", $"Timer hit 0. Deleting cocoon.\nPlayer lost {rosariesLost} rosaries.{(rosariesLost > 0 ? " Sorry!" : "")}");
        }
        RaceTheTimerActive = false;
        UpdateRaceTheTimerGraphic();
    }
    private static int DeleteCorpse()
    {
        //Decide what to do if the player changes safe file before the timer expires?
        //decided to just leave it as-is; if they wanted to avoid the cocoon deletion that badly, they can just alt-f4 either way.

        int silkBefore = PlayerData.instance.silk;

        //clear money pool
        int moneyBefore = PlayerData.instance.HeroCorpseMoneyPool;
        PlayerData.instance.HeroCorpseMoneyPool = 0;

        //clear corpse location data
        PlayerData.instance.HeroCorpseScene = string.Empty;
        PlayerData.instance.HeroCorpseMarkerGuid = null;

        //actually delete the thang
        EventRegister.SendEvent("BREAK HERO CORPSE");


        //fix broken silk spool (I'm not that evil)
        if (PlayerData.instance.silkMax > 9)
        {
            PlayerData.instance.IsSilkSpoolBroken = false;
            EventRegister.SendEvent(EventRegisterEvents.SpoolUnbroken);
        }
        //remove the silk that BREAK HERO CORPSE adds
        if (PlayerData.instance.silk > silkBefore)
        {
            HeroController.instance.TakeSilk(PlayerData.instance.silk - silkBefore);
        }

        return moneyBefore;
    }
    private void UpdateRaceTheTimerGraphic()
    {
        Vibe.UI.TimerReadout.SetClassListIf("race-the-timer", x => RaceTheTimerActive);
    }

    protected override string _punctuateReminderDescription => "death";

}
