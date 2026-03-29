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

    /// <summary>
    /// Gets a value from PlayerData by trying field first, then property.
    /// PlayerDataAccess uses C# properties, not raw fields.
    /// </summary>
    private static object? GetMemberValue(Type type, object instance, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (field != null) return field.GetValue(instance);

        var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null) return prop.GetValue(instance);

        return null;
    }
}
