using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ButtplugSong;

/// <summary>
/// Polls tracked PlayerData fields via reflection to detect direct field writes
/// that bypass SetBool/SetInt (e.g., shop purchases, map acquisitions).
/// Fires OnSetBoolHook/OnSetIntHook when changes are detected.
/// </summary>
internal class PlayerDataPoller : MonoBehaviour
{
    private static PlayerDataPoller? _instance;
    private static readonly BepInEx.Logging.ManualLogSource _log =
        BepInEx.Logging.Logger.CreateLogSource("ButtplugSong.Poller");

    private readonly Dictionary<string, object> _snapshot = new();
    private readonly HashSet<string> _trackedBools = new();
    private readonly HashSet<string> _trackedInts = new();
    private readonly HashSet<string> _trackedCollectables = new();
    private readonly Dictionary<string, int> _collectableSnapshot = new();
    private object? _lastPdRef;

    private const float PollIntervalSeconds = 0.5f;

    public static PlayerDataPoller EnsureExists()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("ButtplugSong_PDPoller");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<PlayerDataPoller>();
        _log.LogInfo("PlayerData poller started.");
        return _instance;
    }

    public void TrackBool(string fieldName)
    {
        _trackedBools.Add(fieldName);
    }

    // Most PD int writes now route through Harmony patches on SetInt/IncrementInt/IntAdd/DecrementInt.
    // The int list here is the residue: fields written by direct field assignment in compiled game
    // code that bypasses every typed setter. The dedup guard in BuzzOnPickups handles overlap.
    public void TrackInt(string fieldName)
    {
        _trackedInts.Add(fieldName);
    }

    /// <summary>
    /// Track a CollectableItemManager item by name (e.g. "Mossberry").
    /// When its Amount increases, fires OnSetIntHook with the collectable name.
    /// </summary>
    public void TrackCollectable(string collectableName)
    {
        _trackedCollectables.Add(collectableName);
    }

    private void Start()
    {
        StartCoroutine(PollLoop());
    }

    private IEnumerator PollLoop()
    {
        // Wait for game to fully load
        yield return new WaitForSeconds(2f);

        while (true)
        {
            yield return new WaitForSeconds(PollIntervalSeconds);

            if (!PlayerData.HasInstance) continue;
            var pd = PlayerData.instance;
            var pdType = pd.GetType();

            // New save loaded: PD instance flipped, drop stale baselines.
            if (!ReferenceEquals(pd, _lastPdRef))
            {
                _snapshot.Clear();
                _collectableSnapshot.Clear();
                _lastPdRef = pd;
            }

            foreach (var fieldName in _trackedBools)
            {
                try
                {
                    object? raw = GetMemberValue(pdType, pd, fieldName);
                    if (raw == null) continue;

                    bool current = (bool)raw;
                    bool prev = false;
                    if (_snapshot.TryGetValue(fieldName, out var prevObj) && prevObj is bool pb)
                        prev = pb;

                    if (current != prev)
                    {
                        _snapshot[fieldName] = current;
                        if (current) // Only trigger on false -> true
                        {
                            _log.LogInfo($"POLL BOOL CHANGED: {fieldName} = {current}");
                            ModHooks.RaiseSetBool(fieldName, current);
                        }
                    }
                }
                catch { /* member doesn't exist on this PD version, skip */ }
            }

            foreach (var fieldName in _trackedInts)
            {
                try
                {
                    object? raw = GetMemberValue(pdType, pd, fieldName);
                    if (raw == null) continue;

                    int current = (int)raw;
                    int prev = 0;
                    if (_snapshot.TryGetValue(fieldName, out var prevObj) && prevObj is int pi)
                        prev = pi;

                    if (current != prev)
                    {
                        int prevVal = prev;
                        _snapshot[fieldName] = current;
                        if (current > prevVal) // Only trigger on increase
                        {
                            _log.LogInfo($"POLL INT CHANGED: {fieldName} = {current} (was {prevVal})");
                            ModHooks.RaiseSetInt(fieldName, current);
                        }
                    }
                }
                catch { /* member doesn't exist on this PD version, skip */ }
            }

            // Poll CollectableItemManager for tracked collectables (e.g. Mossberry)
            if (_trackedCollectables.Count > 0)
            {
                try
                {
                    var mgrType = typeof(CollectableItemManager);
                    var instanceProp = mgrType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                    var mgr = instanceProp?.GetValue(null);
                    if (mgr != null)
                    {
                        // Find all SavedItem children (CollectableItemBasic, etc.)
                        // and compare their .name to tracked names
                        var getAllMethod = mgrType.GetMethod("GetAllCollectables", BindingFlags.Instance | BindingFlags.Public)
                                       ?? mgrType.GetMethod("GetAllItems", BindingFlags.Instance | BindingFlags.Public);
                        if (getAllMethod != null)
                        {
                            var items = getAllMethod.Invoke(mgr, null) as System.Collections.IEnumerable;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    if (item is SavedItem si && _trackedCollectables.Contains(si.name))
                                    {
                                        // Try to get amount via reflection (CollectableItemBasic stores it differently)
                                        int amount = 0;
                                        var amtProp = si.GetType().GetProperty("Amount", BindingFlags.Instance | BindingFlags.Public);
                                        if (amtProp != null)
                                            amount = (int)amtProp.GetValue(si);
                                        else
                                        {
                                            var amtField = si.GetType().GetField("Amount", BindingFlags.Instance | BindingFlags.Public);
                                            if (amtField != null)
                                                amount = (int)amtField.GetValue(si);
                                        }

                                        int prev = -1;
                                        if (_collectableSnapshot.TryGetValue(si.name, out var p))
                                            prev = p;

                                        if (amount != prev)
                                        {
                                            _collectableSnapshot[si.name] = amount;
                                            if (prev >= 0 && amount > prev) // skip initial snapshot
                                            {
                                                _log.LogInfo($"POLL COLLECTABLE: {si.name} = {amount} (was {prev})");
                                                ModHooks.RaiseSetInt(si.name, amount);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { _log.LogDebug($"Collectable poll error: {ex.Message}"); }
            }
        }
    }

    /// <summary>
    /// Resets the snapshot (call when loading a new save to avoid false positives from stale data).
    /// </summary>
    public void ResetSnapshot()
    {
        _snapshot.Clear();
        _collectableSnapshot.Clear();
        _log.LogInfo("Snapshot reset.");
    }

    // Resolved-once-then-cached MemberInfo per field name. Each entry is either
    // FieldInfo, PropertyInfo, or null sentinel meaning "no such member, do not look again."
    // Bounds the per-poll reflection to a single dictionary lookup after warmup.
    private static readonly Dictionary<string, MemberInfo?> _memberCache = new();
    private static readonly object _missingSentinel = new();

    private static object? GetMemberValue(Type type, object instance, string name)
    {
        if (!_memberCache.TryGetValue(name, out var member))
        {
            member = (MemberInfo?)type.GetField(name, BindingFlags.Instance | BindingFlags.Public)
                  ?? (MemberInfo?)type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            _memberCache[name] = member;
        }
        return member switch
        {
            FieldInfo f => f.GetValue(instance),
            PropertyInfo p => p.GetValue(instance),
            _ => null,
        };
    }
}
