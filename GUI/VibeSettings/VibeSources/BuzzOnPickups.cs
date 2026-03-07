using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnPickups : VibeSourceWithPunctuate
{
    private readonly Toggle _scaleWithWeighting;
    public bool ScaleWithWeighting { get => _scaleWithWeighting.value; set => _scaleWithWeighting.value = value; }



    protected override string _punctuateReminderDescription => "collecting an item";

    public BuzzOnPickups() : base("Pickups", false, 50, 5, true)
    {
        ModHooks.OnItemPickupHook += ItemPickup;
        ModHooks.OnSetBoolHook += OnSetBool;
        ModHooks.OnSetIntHook += OnSetInt;

        _scaleWithWeighting = Get<Toggle>("PickupsScaleWithWeighting");
        _scaleWithWeighting.SetupSaving(true).DependsOn(_enabled);

        VisualElement parent = Get<Label>("PickupsItemListLabel").parent;

        int id = 0;

        KnownItems = new Dictionary<string, WeightedEvent>()
        {
            //notes for weighting: On default settings, a weight of 1 is 50% for 5 seconds.
            //Meaning that anything with a weight of 2 or above is 100% for 10+ seconds.

            //ABILITIES
            //silk skills (any) - 1
            //swift step - 0.6f
            //cling grip - 1.5f,
            //needolin - 1.5f
            //clawline - 1
            //silksoar - 1.8f
            //sylphsong - 1
            //beastling call - 1
            //elegy of the deep - 1.6f
            //bind - weight 2 - ONLY INCLUDE IF ADDED TO ARCHIPELAGO RANDOMIZER
            //needle strike - 0.6f
            //architect's melody - 1.6f
            //conductor's melody - 1.6f

            //UPGRADES
            //spool fragment - 0.6
            //memory locket - 0.8
            //mask shard - 1
            //silk heart - 1.2

            //drifters cloak - 1
            //faydown cloak - 2
            //tool pouch - 0.5
            //crafting kit - 0.5

            //Bellway - 0.4f
            //map (any) - 0.2f
            //map markers - 0.2f
            //quill - 0.4
            //plasmium gland - 1
            //everbloom - 2.5

            //PICKUPS
            { "Rosary_Set_Frayed", CreateUI("FrayedRosaryString", 0.3f, true, categoryLabel: "Collectable Items") },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("RosaryString", 0.5f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("RosaryNecklace", 1f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("HeavyRosaryNecklace", 2.2f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("PaleRosaryNecklace", 3.4f, true, true) },

            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("ShardBundle", 0.5f, true) },
            { "Great Shard", CreateUI("BeastShard", 1.4f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("PristineCore", 2.2f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("HornetStatuette", 3f, true) },
            { id+++ "UNKNOWN IDENTIFIER ", CreateUI("Growstone", 3f, false, true) },

            //silk eater - 1
            //craftmetal - 1
            //pale oil - 2
            //relics (bone/weaver/choral/harp) - 0.6f
            //arcane egg - 2.4f (same effect as in hollow knight :D)
            //psalm cylinder - 1
            //sacred cylinder - 2

            //quest deliverables - 0.2f
            //souls - 0.8f
            //hearts - 1.6f
            //twisted bud - 6 

            //keys (any. includes craw summons) - 0.6

            //bellhome upgrades - 0.4f

            //mementos - 1

            //TOOLS - do I want to list each one individually?
            //red tools - 1
            //blue tools - 0.8f
            //yellow tools - 0.6f

            //CRESTS
            //hunter upgrade - 0.7f
            //vesticrest upgrade - 1
            //reaper - 0.8f
            //wanderer - 0.8f
            //beast - 0.6f
            //witch - 1
            //architect - 1
            //shaman - 1
        };
        WeightedEvent CreateUI(string identifier, float defaultWeight, bool defaultOn, bool gapBelow = false, string? categoryLabel = null)
        {
            return WeightedEvent.CreateWithUI(identifier, defaultWeight, defaultOn, parent, this, _enabled, _scaleWithWeighting, !gapBelow, categoryLabel);
        }
    }
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _scaleWithWeighting.Load(preset);
        foreach (var item in KnownItems.Values)
        {
            item.Load(preset);
        }
    }
    readonly HashSet<string> DEBUG_SEEN_NAMES = new();
    private void OnSetInt(string intName, int value)
    {
        string message = $"Set int {intName} to {value}";
        if (KnownInts.TryGetValue(intName, out string realName))
        {
            message += $" - RECOGNISED AS: {realName}";
        }
        else message += " - UNRECOGNISED";
        if (DEBUG_SEEN_NAMES.Contains(intName)) return;
        DEBUG_SEEN_NAMES.Add(intName);
        Log(message);
    }
    private void OnSetBool(string boolName, bool value)
    {
        string message = $"Set bool {boolName} to {value}";
        if (KnownBools.TryGetValue(boolName, out string realName))
        {
            message += $" - RECOGNISED AS: {realName}";
        }
        else message += " - UNRECOGNISED";
        if (DEBUG_SEEN_NAMES.Contains(boolName)) return;
        DEBUG_SEEN_NAMES.Add(boolName);
        Log(message);
    }
    private void ItemPickup(SavedItem item, CollectableItemPickup instance)
    {
        Log($"GOT ITEM: {item.GetType().Name} - {item.name} (unique: {item.IsUnique})");
        Log($"{item.CanGetMore()} - {item.CanGetMultipleAtOnce} - {item}");
        Log($"Instance: {instance.name}: {instance.persistent} - {instance.didStart} - {instance.activatedSave} - {instance.IsInvoking()} - {instance.isActiveAndEnabled} - {instance.tag} - {instance.hasStarted}");

        if (!Enabled) return;
        if (KnownItems.TryGetValue(item.name, out WeightedEvent itemEvent))
        {
            if (!itemEvent.Enabled) return;
            if (!ScaleWithWeighting) Activate(itemEvent.EnabledText);
            else Activate(itemEvent.Weight, itemEvent.EnabledText);
        }
    }

    #region Recognised Pickups
    private readonly Dictionary<string, WeightedEvent> KnownItems;
    public static readonly Dictionary<string, string> KnownInts = new()
    {
        { "silkRegenMax", "Silk Heart" },
    };
    public static readonly Dictionary<string, string> KnownBools = new()
    {
        { "hasDash", "Swift Step" },
        { "hasWalljump", "Cling Grip" },
        { "hasHarpoonDash", "Clawline" },
        { "hasSuperJump", "Silk Soar" },
        { "hasNeedolin", "Needolin" },
        { "HasBoundCrestUpgrader", "Sylphsong" },
        { "UnlockedFastTravelTeleport", "Beastling Call" },
        { "hasNeedolinMemoryPowerup", "Elegy of the Deep" },
        { "hasBrolly", "Drifter's Cloak" },
        { "hasDoubleJump", "Faydown Cloak" },
        { "hasChargeSlash", "Needle Strike" },

        { "UnlockedDocksStation", "Bellway: Deep Docks" },
        { "UnlockedBoneforestEastStation", "Bellway: Far Fields" },
        { "UnlockedGreymoorStation", "Bellway: Greymoor" },
        { "UnlockedBelltownStation", "Bellway: Bellhart" },
        { "UnlockedCoralTowerStation", "Bellway: Blasted Steps" },
        { "UnlockedCityStation", "Bellway: Grand Bellway" },
        { "UnlockedPeakStation", "Bellway: The Slab" },
        { "UnlockedShellwoodStation", "Bellway: Shellwood" },
        { "UnlockedShadowStation", "Bellway: Bilewater" },
        { "UnlockedAqueductStation", "Bellway: Putrified Ducts" },
        { "UnlockedSongTube", "Ventrica: Choral Chambers" },
        { "UnlockedUnderTube", "Ventrica: Underworks" },
        { "UnlockedCityBellwayTube", "Ventrica: Grand Bellway" },
        { "UnlockedHangTube", "Ventrica: High Halls" },
        { "UnlockedEnclaveTube", "Ventrica: First Shrine" },
        { "UnlockedArboriumTube", "Ventrica: Memorium" },

        { "HasBellhartMap", "Bellhart Map" },
        { "HasBilewaterMap", "Bilewater Map" },
        { "HasBlastedStepsMap", "Blasted Steps Map" },
        { "HasChoralChambersMap", "Choral Chambers Map" },
        { "HasDeepDocksMap", "Deep Docks Map" },
        { "HasFarFieldsMap", "Far Fields Map" },
        { "HasGreymoorMap", "Greymoor Map" },
        { "HasMosslandsMap", "Mosslands Map" },
        { "HasShellwoodMap", "Shellwood Map" },
        { "HasTheMarrowMap", "The Marrow Map" },
        { "HasTheSlabMap", "The Slab Map" },
        { "HasUnderworksMap", "Underworks Map" },
        { "HasVerdaniaMap", "Verdania Map" },

        { "hasPinBench", "Bench Pins" },
        { "hasPinStag", "Bellway Pins" },
        { "hasPinShop", "Vendor Pins" },
        { "hasPinTube", "Ventrica Pins" },
        { "hasPinFleaMarrowlands", "Flea Pins: Marrowlands" },
        { "hasPinFleaMidlands", "Flea Pins: Midlands" },
        { "hasPinFleaBlastedlands", "Flea Pins: Blastedlands" },
        { "hasPinFleaCitadel", "Flea Pins: Citadel" },
        { "hasPinFleaPeaklands", "Flea Pins: Peaklands" },
        { "hasPinFleaMucklands", "Flea Pins: Mucklands" },

        { "hasMarker_a", "Shell Marker" },
        { "hasMarker_b", "Ring Marker" },
        { "hasMarker_c", "Hunt Marker" },
        { "hasMarker_d", "Dark Marker" },
        { "hasMarker_e", "Bronze Marker" },

        { "UnlockedExtraBlueSlot", "Vesticrest Blue Expansion" },
        { "UnlockedExtraYellowSlot", "Vesticrest Yellow" },
        { "HasMelodyArchitect", "Architect's Melody" },
        { "HasMelodyConductor", "Conductor's Melody" },

        //also: materium, farsight, crawbell
        { "BelltownFurnishingDesk", "Desk" },
        { "BelltownFurnishingFairyLights", "Gleamlights" },
        { "BelltownFurnishingGramaphone", "Gramophone" },
        { "BelltownFurnishingSpa", "Personal Spa" },
    };
    #endregion
}