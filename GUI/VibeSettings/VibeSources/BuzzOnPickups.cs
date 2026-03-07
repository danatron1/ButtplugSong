using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class BuzzOnPickups : VibeSourceWithPunctuate
{
    private readonly Toggle _scaleWithWeighting;
    public bool ScaleWithWeighting { get => _scaleWithWeighting.value; set => _scaleWithWeighting.value = value; }

    private readonly Dictionary<string, WeightedEvent> KnownItems = new();
    protected override string _punctuateReminderDescription => "collecting an item";

    public BuzzOnPickups() : base("Pickups", false, 50, 5, true)
    {
        ModHooks.OnItemPickupHook += ItemPickup;
        ModHooks.OnSetBoolHook += OnSetBool;
        ModHooks.OnSetIntHook += OnSetInt;

        _scaleWithWeighting = Get<Toggle>("PickupsScaleWithWeighting");
        _scaleWithWeighting.SetupSaving(true).DependsOn(_enabled);

        VisualElement parent = Get<Label>("PickupsItemListLabel").parent;

        KnownItems["Rosary_Set_Frayed"] = CreateUI("FrayedRosaryString", 0.3f, true, categoryLabel: "Collectable Items");
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("RosaryString", 0.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("RosaryNecklace", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("HeavyRosaryNecklace", 2.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("PaleRosaryNecklace", 3.4f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ShardBundle", 0.5f, true);
        KnownItems["Great Shard"]       = CreateUI("BeastShard", 1.4f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("PristineCore", 2.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("HornetStatuette", 3f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Growstone", 3f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SilkEater", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Craftmetal", 0.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Relics", 0.6f, true); // e.g. bone/weaver/choral/harp
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ArcaneEgg", 2.4f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("PsalmCylinders", 2.4f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SacredCylinder", 2, true, true); // u/Smart_Calendar1874

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ShamanSouls", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("OldHearts", 1.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("PaleOil", 2, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("TwistedBud", 6, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("OtherQuestItems", 0.2f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Mementos", 1, true, true);

        
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("RedTools", 1f, true, categoryLabel: "Equipment / Upgrades");
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("BlueTools", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("YellowTools", 0.6f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ToolPouch", 0.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("CraftingKit", 0.5f, true, true);

        KnownItems["silkRegenMax"] = CreateUI("SilkHeart", 1.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("MaskShard", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("MemoryLocket", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SpoolFragment", 0.6f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("DriftersCloak", 1, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("FaydownCloak", 2, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Maps", 0.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("MapMarkers", 0.1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Quill", 0.3f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("BellwayUnlocks", 0.4f, true, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("VentricaUnlocks", 0.5f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Keys", 0.6f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("PlasmiumGland", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Everbloom", 2.5f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("BellhomeUpgrades", 0.5f, true, true);
        
        
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SilkSkills", 1, true, categoryLabel: "Abilities");
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SwiftStep", 0.6f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ClingGrip", 1.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Clawline", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("SilkSoar", 2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("NeedleStrike", 0.6f, true, true);

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Needolin", 1.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ElegyOfTheDeep", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("BeastlingCall", 0.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Sylphsong", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ArchitectMelody", 1.2f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ConductorMelody", 1.2f, true, true);
        //KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Bind", 2, true, true); //To be added once randomizer releases (assuming they allow for bind rando)

        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ReaperCrest", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("WandererCrest", 0.8f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("BeastCrest", 0.5f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("WitchCrest", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ArchitectCrest", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("ShamanCrest", 1f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("HuntersUpgrade", 0.7f, true);
        KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("VesticrestUpgrade", 1f, true, true);

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
        TryActivate(intName);
    }
    private void OnSetBool(string boolName, bool value)
    {
        if (value) TryActivate(boolName);

        //purely for logging (helps me locate bools for unknown items)
        if (DEBUG_SEEN_NAMES.Contains(boolName)) return;
        string message = $"Set bool {boolName} to {value}";
        if (KnownBools.TryGetValue(boolName, out string realName)) message += $" - RECOGNISED AS: {realName}";
        else message += " - UNRECOGNISED";
        DEBUG_SEEN_NAMES.Add(boolName);
        Log(message);
    }
    private void ItemPickup(SavedItem item, CollectableItemPickup instance)
    {
        Log($"GOT ITEM: {item.GetType().Name} - {item.name} (unique: {item.IsUnique})");
        TryActivate(item.name);
    }
    private void TryActivate(string name)
    {
        if (!Enabled) return;
        if (KnownItems.TryGetValue(name, out WeightedEvent weightedEvent)) Activate(weightedEvent);
    }
    private void Activate(WeightedEvent weightedEvent)
    {
        if (!weightedEvent.Enabled) return;
        if (!ScaleWithWeighting) Activate(weightedEvent.EnabledText);
        else Activate(weightedEvent.Weight, weightedEvent.EnabledText);
    }

    #region Recognised Pickups

    //Not currently doing anything with these, they're purely for logging. I imagine it'll be useful later.
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