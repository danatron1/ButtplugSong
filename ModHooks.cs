using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public static event Action? OnMaxHealthUpHook;
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddToMaxHealth))]
    [HarmonyPostfix]
    private static void OnMaxHealthUp()
    {
        OnMaxHealthUpHook?.Invoke();
    }

    public static event Action? OnMaxSilkUpHook;
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.AddToMaxSilk))]
    [HarmonyPostfix]
    private static void OnMaxSilkUp()
    {
        OnMaxSilkUpHook?.Invoke();
    }

    // Individual shard/fragment collection (heartPieces/silkSpoolParts are plain fields, bypass SetInt)
    public static event Action? OnHeartPieceCollectedHook;
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckHeartAchievements))]
    [HarmonyPostfix]
    private static void OnHeartPieceCollected()
    {
        OnHeartPieceCollectedHook?.Invoke();
    }

    public static event Action? OnSpoolFragmentCollectedHook;
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckSilkSpoolAchievements))]
    [HarmonyPostfix]
    private static void OnSpoolFragmentCollected()
    {
        OnSpoolFragmentCollectedHook?.Invoke();
    }

    public static event Action<ToolItem>? OnToolUnlockHook;
    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.Unlock))]
    [HarmonyPostfix]
    private static void OnToolUnlock(ToolItem __instance)
    {
        OnToolUnlockHook?.Invoke(__instance);
    }
    public static event Action<SavedItem>? OnItemPickupHook;

    [HarmonyPatch]
    private static class SavedItemGetPatch
    {

        static IEnumerable<MethodBase> TargetMethods()
        {
            var log = BepInEx.Logging.Logger.CreateLogSource("ButtplugSong.Hooks");
            var methods = new List<MethodBase>();

            var baseGet = typeof(SavedItem).GetMethod(nameof(SavedItem.Get), new[] { typeof(int), typeof(bool) });
            if (baseGet != null && !baseGet.IsAbstract)
                methods.Add(baseGet);

            var baseGetSimple = typeof(SavedItem).GetMethod(nameof(SavedItem.Get), new[] { typeof(bool) });
            if (baseGetSimple != null && !baseGetSimple.IsAbstract)
                methods.Add(baseGetSimple);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (!typeof(SavedItem).IsAssignableFrom(type) || type == typeof(SavedItem))
                        continue;

                    foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    {
                        if (method.Name == nameof(SavedItem.Get) && !method.IsAbstract)
                            methods.Add(method);
                    }
                }
            }

            foreach (var m in methods)
                log.LogInfo($"SavedItemGetPatch target: {m.DeclaringType.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");

            return methods;
        }

        [HarmonyPrefix]
        static void Prefix(SavedItem __instance)
        {
            OnItemPickupHook?.Invoke(__instance);
        }
    }

    // Catches ALL PlayerData field writes via SetVariable (used by PlayerDataAccess properties)
    // This is the universal hook for maps, abilities, tool pouch, silk heart, etc.
    public static event Action<string, object>? OnPlayerDataSetVariableHook;
    [HarmonyPatch(typeof(GenericVariableExtension.VariableExtensionsGeneric), nameof(GenericVariableExtension.VariableExtensionsGeneric.SetVariable),
        new[] { typeof(object), typeof(string), typeof(object), typeof(Type) })]
    [HarmonyPostfix]
    private static void OnSetVariable(object obj, string fieldName, object value)
    {
        if (obj is PlayerData)
            OnPlayerDataSetVariableHook?.Invoke(fieldName, value);
    }
}
