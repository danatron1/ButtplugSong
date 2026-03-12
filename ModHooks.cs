using HarmonyLib;
using System;
using UnityEngine;

namespace ButtplugSong;

[HarmonyPatch]
internal static class ModHooks
{
    //Damage
    public static event Func<PlayerData, int, int>? OnTakeDamageHook;

    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeHealth))]
    [HarmonyPrefix]
    private static void PlayerData_TakeHealth(PlayerData __instance, ref int amount, ref bool hasBlueHealth, ref bool allowFracturedMaskBreak)
    {
        if (OnTakeDamageHook != null)
        {
            amount = OnTakeDamageHook.Invoke(__instance, amount);
        }
    }

    //Heal
    public static event Action<HeroController>? OnBindInterruptedHook;

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.BindInterrupted))]
    [HarmonyPostfix]
    private static void OnBindInterrupted(HeroController __instance)
    {
        OnBindInterruptedHook?.Invoke(__instance); //note - can check for shield: __instance.WillDoBellBindHit();
    }

    public static event Func<PlayerData, int, int>? OnAddHealthHook;

    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddHealth))]
    [HarmonyPrefix]
    private static void PlayerData_AddHealth(PlayerData __instance, ref int amount)
    {
        if (OnAddHealthHook != null)
        {
            amount = OnAddHealthHook.Invoke(__instance, amount);
        }
    }

    //Strike
    public static event Func<HealthManager, HitInstance, int>? OnDealDamageHook;

    [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
    [HarmonyPrefix]
    private static void OnDealDamage(HealthManager __instance, ref HitInstance hitInstance)
    {
        if (OnDealDamageHook != null)
        {
            hitInstance.DamageDealt = OnDealDamageHook.Invoke(__instance, hitInstance);
        }
    }

    //Death
    public static event Action<HeroController, bool, bool>? OnBeforeDeathHook;

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.Die))]
    [HarmonyPrefix]
    private static void OnBeforePlayerDead(HeroController __instance, bool nonLethal, bool frostDeath)
    {
        OnBeforeDeathHook?.Invoke(__instance, nonLethal, frostDeath);
    }
    public static event Action<GameManager>? OnAfterDeathHook;

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDead))] //hooking onto different method as this one triggers fully after
    [HarmonyPrefix]
    private static void OnAfterPlayerDead(GameManager __instance, float waitTime)
    {
        OnAfterDeathHook?.Invoke(__instance);
    }

    //GetCocoon
    public static event Action<HeroController>? OnGetCocoonHook;
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.CocoonBroken), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    private static void OnGetCocoon(HeroController __instance, bool doAirPause, bool forceCanBind)
    {
        OnGetCocoonHook?.Invoke(__instance);
    }

    //Item pickup (hooks SavedItem.Get to catch both world pickups AND shop purchases)
    public static event Action<SavedItem>? OnItemPickupHook;
    [HarmonyPatch(typeof(SavedItem), nameof(SavedItem.Get), typeof(int), typeof(bool))]
    [HarmonyPrefix]
    private static void OnItemGet(SavedItem __instance, int amount, bool showPopup)
    {
        OnItemPickupHook?.Invoke(__instance);
    }
    //[HarmonyPatch(typeof(SavedItem), nameof(SavedItem.Get), typeof(bool))]
    //[HarmonyPrefix]
    //private static void OnItemGetSimple(SavedItem __instance, bool showPopup)
    //{
    //    OnItemPickupHook?.Invoke(__instance);
    //}

    public static event Action<string, bool>? OnSetBoolHook;
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.SetBool))]
    [HarmonyPostfix]
    private static void OnSetBool(string boolName, bool value)
    {
        OnSetBoolHook?.Invoke(boolName, value);
    }

    public static event Action<string, int>? OnSetIntHook;
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.SetInt))]
    [HarmonyPostfix]
    private static void OnSetInt(string intName, int value)
    {
        OnSetIntHook?.Invoke(intName, value);
    }

    //Controller rumble
    public static event Action<string?>? OnRumbleHook;

    [HarmonyPatch(typeof(VibrationManager), nameof(VibrationManager.PlayVibrationClipOneShot), typeof(VibrationData), typeof(VibrationTarget?), typeof(bool), typeof(string), typeof(bool))]
    [HarmonyPrefix]
    private static void OnPlayVibrationClipOneShot(ref VibrationData vibrationData, ref VibrationTarget? vibrationTarget, ref bool isLooping, ref string tag, ref bool isRealtime)
    {
        OnRumbleHook?.Invoke(tag);
    }

    //currency
    public static event Func<PlayerData, int, int>? OnAddRosariesHook;
    public static event Func<PlayerData, int, int>? OnTakeRosariesHook;
    public static event Func<PlayerData, int, int>? OnAddShardsHook;
    public static event Func<PlayerData, int, int>? OnTakeShardsHook;

    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddGeo))]
    [HarmonyPrefix]
    private static void OnAddRosaries(PlayerData __instance, ref int amount)
    {
        if (OnAddRosariesHook != null) amount = OnAddRosariesHook.Invoke(__instance, amount);
    }
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeGeo))]
    [HarmonyPrefix]
    private static void OnTakeRosaries(PlayerData __instance, ref int amount)
    {
        if (OnTakeRosariesHook != null) amount = OnTakeRosariesHook.Invoke(__instance, amount);
    }
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddShards))]
    [HarmonyPrefix]
    private static void OnAddShards(PlayerData __instance, ref int amount)
    {
        if (OnAddShardsHook != null) amount = OnAddShardsHook.Invoke(__instance, amount);
    }
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeShards))]
    [HarmonyPrefix]
    private static void OnTakeShards(PlayerData __instance, ref int amount)
    {
        if (OnTakeShardsHook != null) amount = OnTakeShardsHook.Invoke(__instance, amount);
    }

    //Bump
    public static event Action<HeroController>? OnHardLandingHook;

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.DoHardLandingEffectNoHit))] //for purposes that aren't this mod, you probably want DoHardLanding
    [HarmonyPrefix]
    private static void OnHardLanding(HeroController __instance)
    {
        OnHardLandingHook?.Invoke(__instance);
    }


    //other
    public static event Action<DeliveryQuestItem>? OnCourierBreakItemHook;

    [HarmonyPatch(typeof(DeliveryQuestItem), nameof(DeliveryQuestItem.BreakEffect))]
    [HarmonyPrefix]
    private static void CourierBreakItem(DeliveryQuestItem __instance, Vector2 heroPos)
    {
        OnCourierBreakItemHook?.Invoke(__instance);
    }
    //mods loaded
    public static event Action? OnFinishedLoadingModsHook;
    [HarmonyPatch(typeof(OnScreenDebugInfo), nameof(OnScreenDebugInfo.Awake))]
    [HarmonyPrefix]
    private static void OnFinishedLoadingMods()
    {
        OnFinishedLoadingModsHook?.Invoke();
    }
    //Upgrade completion
    public static event Action<int>? OnMaxHealthUpHook;
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddToMaxHealth))]
    [HarmonyPostfix]
    private static void OnMaxHealthUp(int amount)
    {
        OnMaxHealthUpHook?.Invoke(amount);
    }

    public static event Action<int>? OnMaxSilkUpHook;
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.AddToMaxSilk))]
    [HarmonyPostfix]
    private static void OnMaxSilkUp(int amount)
    {
        OnMaxSilkUpHook?.Invoke(amount);
    }
}
