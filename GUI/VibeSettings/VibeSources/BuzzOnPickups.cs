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
    private readonly Dictionary<string, object> _lastValues = new();
    protected override string _punctuateReminderDescription => "collecting an item";

    public BuzzOnPickups() : base("Pickups", false, 50, 5, true)
    {
        ModHooks.OnItemPickupHook += ItemPickup;
        ModHooks.OnSetBoolHook += OnSetBool;
        ModHooks.OnSetIntHook += OnSetInt;
        ModHooks.OnMaxHealthUpHook += OnMaxHealthUp;
        ModHooks.OnMaxSilkUpHook += OnMaxSilkUp;
        ModHooks.OnHeartPieceCollectedHook += OnHeartPieceCollected;
        ModHooks.OnSpoolFragmentCollectedHook += OnSpoolFragmentCollected;
        ModHooks.OnToolUnlockHook += ToolUnlock;
        ModHooks.OnPlayerDataSetVariableHook += OnPlayerDataSetVariable;

        _scaleWithWeighting = Get<Toggle>("PickupsScaleWithWeighting");
        _scaleWithWeighting.SetupSaving(true).DependsOn(_enabled);

        VisualElement parent = Get<Label>("PickupsItemListLabel").parent;

        #region Collectable Items
        KnownItems["Rosary_Set_Frayed"] = CreateUI("FrayedRosaryString", 0.3f, true, categoryLabel: "Collectable Items");
        KnownItems["Rosary_Set_Small"] = CreateUI("RosaryString", 0.6f, true);
        KnownItems["Rosary_Set_Medium"] = CreateUI("RosaryNecklace", 1.2f, true);
        KnownItems["Rosary_Set_Large"] = CreateUI("HeavyRosaryNecklace", 2.2f, true);
        KnownItems["Rosary_Set_Huge_White"] = CreateUI("PaleRosaryNecklace", 3.4f, true, true);

        KnownItems["Shard Pouch"] = CreateUI("ShardBundle", 0.5f, true); //lower weight because easily purchasable
        KnownItems["Great Shard"] = CreateUI("BeastShard", 1.4f, true);
        KnownItems["Pristine Core"] = CreateUI("PristineCore", 2.2f, true);
        KnownItems["Fixer Idol"] = CreateUI("HornetStatuette", 3f, true);
        KnownItems["Growstone"] = CreateUI("Growstone", 3f, true, true);

        KnownItems["Silk Grub"] = CreateUI("SilkEater", 0.8f, true);
        KnownItems["Tool Metal"] = CreateUI("Craftmetal", 0.5f, true);
        MapCategory(CreateUI("Relics", 0.6f, true), // bone/weaver/choral/harp
            "Bone Record Wisp Top", "Bone Record Greymoor_flooded_corridor",
            "Bone Record Bone_East_14", "Bone Record Understore_Map_Room",
            "Seal Chit City Merchant", "Seal Chit Silk Siphon",
            "Seal Chit Ward Corpse", "Seal Chit Aspid_01",
            "Weaver Record Conductor", "Weaver Record Sprint_Challenge", "Weaver Record Weave_08",
            "Weaver Totem Slab_Bottom", "Weaver Totem Bonetown_upper_room", "Weaver Totem Witch");
        KnownItems["Ancient Egg Abyss Middle"] = CreateUI("ArcaneEgg", 2.4f, true);
        MapCategory(CreateUI("PsalmCylinders", 0.6f, true),
            "Psalm Cylinder Hang", "Psalm Cylinder Librarian",
            "Psalm Cylinder Grindle", "Psalm Cylinder Library Roof", "Psalm Cylinder Ward");
        KnownItems["Librarian Melody Cylinder"] = CreateUI("SacredCylinder", 2, true, true); //u/Smart_Calendar1874

        MapCategory(CreateUI("ShamanSouls", 0.8f, true),
            "Snare Soul Bell Hermit", "Snare Soul Churchkeeper", "Snare Soul Swamp Bug");
        MapCategory(CreateUI("OldHearts", 1.5f, true),
            "Clover Heart", "Coral Heart", "Flower Heart", "Hunter Heart");
        KnownItems["Pale_Oil"] = CreateUI("PaleOil", 2, true);
        KnownItems["Wood Witch Item"] = CreateUI("TwistedBud", 6, true);
        KnownItems["Plasmium Gland"] = CreateUI("PlasmiumGland", 1f, true);
        KnownItems["White Flower"] = CreateUI("Everbloom", 2.5f, true, true);

        MapCategory(CreateUI("OtherQuestItems", 0.2f, true, true),
            "Broodmother Remains", "Cog Heart Pieces", "Skull King Fragment",
            "Coral Ingredient", "Rock Roller Item", "Ant Trapper Item",
            "Beastfly Remains", "Mossberry", "Mossberry Stew", "Pickled Roach Egg",
            "Shell Flower", "Broken SilkShot", "Extractor Machine Pins", "Vintage Nectar",
            "Courier Supplies Gourmand", "Courier Supplies", "Courier Supplies Mask Maker",
            "Courier Supplies Slave", "Song Pilgrim Cloak", "Fine Pin", "Pilgrim Rag",
            "Plasmium Blood", "Plasmium", "Crow Feather", "Roach Corpse Item",
            "Enemy Morsel Seared", "Enemy Morsel Shredded", "Silver Bellclapper",
            "Enemy Morsel Speared", "Common Spine", "Flintgem");

        MapCategory(CreateUI("Mementos", 1, true, true),
            "Crowman Memento", "Grey Memento", "Memento Seth",
            "Memento Garmond", "Hunter Memento", "Sprintmaster Memento", "Memento Surface");

        #endregion

        #region Equipment / Upgrades

        MapCategory(CreateUI("RedTools", 0.8f, true, categoryLabel: "Equipment / Upgrades"),
            "Cogwork Flier", "Cogwork Saw", "Conch Drill", "Curve Claws", "Curve Claws Upgraded",
            "Screw Attack", "Flea Brew", "Flintstone", "Harpoon", "Extractor", "Pimpilo",
            "Lifeblood Syringe", "Rosary Cannon", "WebShot Forge", "WebShot Weaver",
            "WebShot Architect", "Silk Snare", "Sting Shard", "Straight Pin", "Tack",
            "Tri Pin", "Shakra Ring", "Lightning Rod");

        MapCategory(CreateUI("BlueTools", 0.7f, true),
            "Dazzle Bind", "Dazzle Bind Upgraded", "Mosscreep Tool 1", "Mosscreep Tool 2",
            "Flea Charm", "Fractured Mask", "Quickbind", "Longneedle", "Lava Charm",
            "Revenge Crystal", "Multibind", "Pinstress Tool", "Poison Pouch", "Quick Sling",
            "Reserve Bind", "Brolly Spike", "Thief Claw", "Spool Extender", "Zap Imbuement",
            "Bell Bind", "White Ring", "Wisp Lantern", "Maggot Charm");

        MapCategory(CreateUI("YellowTools", 0.6f, true),
            "Wallcling", "Barbed Wire", "Compass", "Dead Mans Purse", "Rosary Magnet",
            "Magnetite Dice", "Scuttlebrace", "Bone Necklace", "Shell Satchel",
            "Sprintmaster", "Musician Charm", "Thief Charm", "Weighted Anklet");

        KnownItems["ToolPouchUpgrades"] = CreateUI("ToolPouch", 0.5f, true);
        KnownItems["Tool Pouch&Kit Inv"] = KnownItems["ToolPouchUpgrades"];
        KnownItems["ToolKitUpgrades"] = CreateUI("CraftingKit", 0.5f, true, true);

        KnownItems["silkRegenMax"] = CreateUI("SilkHeart", 1.2f, true);
        KnownItems["Crest Socket Unlocker"] = CreateUI("MemoryLocket", 0.8f, true);
        KnownItems["heartPieces"] = CreateUI("MaskShard", 0.6f, true);
        KnownItems["fullHeart"] = CreateUI("FullMask", 1f, true);
        KnownItems["silkSpoolParts"] = CreateUI("SpoolFragment", 0.4f, true);
        KnownItems["fullSpool"] = CreateUI("FullSpool", 0.6f, true, true);

        #endregion

        #region Navigation / World

        MapCategory(CreateUI("Maps", 0.2f, true, categoryLabel: "Navigation / World"),
            "HasBellhartMap", "HasSwampMap", "HasJudgeStepsMap", "HasHallsMap", "HasCogMap",
            "HasCradleMap", "HasDocksMap", "HasWildsMap", "HasSongGateMap", "HasGreymoorMap",
            "HasHangMap", "HasHuntersNestMap", "HasArboriumMap", "HasMossGrottoMap", "HasPeakMap",
            "HasAqueductMap", "HasCoralMap", "HasShellwoodMap", "HasDustpensMap", "HasAbyssMap",
            "HasBoneforestMap", "HasSlabMap", "HasCitadelUnderstoreMap", "HasCloverMap",
            "HasWeavehomeMap", "HasLibraryMap", "HasWardMap", "HasCrawlMap");
        MapCategory(CreateUI("MapMarkers", 0.1f, true),
            "hasMarker_a", "hasMarker_b", "hasMarker_c", "hasMarker_d", "hasMarker_e",
            "hasPinBench", "hasPinStag", "hasPinShop", "hasPinTube",
            "hasPinFleaMarrowlands", "hasPinFleaMidlands", "hasPinFleaBlastedlands",
            "hasPinFleaCitadel", "hasPinFleaPeaklands", "hasPinFleaMucklands");
        KnownItems["QuillState"] = CreateUI("Quill", 0.3f, true, true);

        MapCategory(CreateUI("BellwayUnlocks", 0.4f, true),
            "UnlockedDocksStation", "UnlockedBoneforestEastStation", "UnlockedGreymoorStation",
            "UnlockedBelltownStation", "UnlockedCoralTowerStation", "UnlockedCityStation",
            "UnlockedPeakStation", "UnlockedShellwoodStation", "UnlockedShadowStation",
            "UnlockedAqueductStation");
        MapCategory(CreateUI("VentricaUnlocks", 0.5f, true, true),
            "UnlockedSongTube", "UnlockedUnderTube", "UnlockedCityBellwayTube",
            "UnlockedHangTube", "UnlockedEnclaveTube", "UnlockedArboriumTube");

        MapCategory(CreateUI("Keys", 0.6f, true, true),
            "Architect Key", "Belltown House Key", "Craw Summons", "Dock Key",
            "Slab Key", "Simple Key", "Ward Boss Key", "Ward Key");

        MapCategory(CreateUI("BellhomeUpgrades", 0.4f, true),
            "Crawbell", "Farsight", "Materium",
            "BelltownFurnishingDesk", "BelltownFurnishingFairyLights",
            "BelltownFurnishingGramaphone", "BelltownFurnishingSpa");
        MapCategory(CreateUI("MateriumEntries", 0.3f, true, true),
            "Materium-Flintstone", "Materium-Magnetite", "Materium-Voltridian",
            "Journal_Entry-Void_Tendrils");

        #endregion

        #region Abilities

        MapCategory(CreateUI("SilkSkills", 1, true, categoryLabel: "Abilities"),
            "Parry", "Silk Boss Needle", "Silk Bomb", "Silk Charge", "Silk Spear", "Thread Sphere");

        KnownItems["hasDash"] = CreateUI("SwiftStep", 0.6f, true);
        KnownItems["hasWalljump"] = CreateUI("ClingGrip", 1.2f, true);
        KnownItems["hasHarpoonDash"] = CreateUI("Clawline", 1f, true);
        KnownItems["hasSuperJump"] = CreateUI("SilkSoar", 2f, true);
        KnownItems["hasChargeSlash"] = CreateUI("NeedleStrike", 0.6f, true, true);

        //KnownItems["UNKNOWNIDENTIFIER"] = CreateUI("Bind", 2, true, true); //To be added once randomizer releases (assuming they allow for bind rando)

        KnownItems["hasBrolly"] = CreateUI("DriftersCloak", 1, true);
        KnownItems["hasDoubleJump"] = CreateUI("FaydownCloak", 2, true, true);

        KnownItems["hasNeedolin"] = CreateUI("Needolin", 1.5f, true);
        KnownItems["hasNeedolinMemoryPowerup"] = CreateUI("ElegyOfTheDeep", 1f, true);
        KnownItems["UnlockedFastTravelTeleport"] = CreateUI("BeastlingCall", 0.5f, true);
        KnownItems["HasMelodyArchitect"] = CreateUI("ArchitectMelody", 1.2f, true);
        KnownItems["HasMelodyConductor"] = CreateUI("ConductorMelody", 1.2f, true);
        KnownItems["HasMelodyLibrarian"] = CreateUI("VaultkeeperMelody", 1.2f, true, true);

        KnownItems["Reaper"] = CreateUI("ReaperCrest", 0.8f, true);
        KnownItems["Wanderer"] = CreateUI("WandererCrest", 0.8f, true);
        KnownItems["Warrior"] = CreateUI("BeastCrest", 0.5f, true);
        KnownItems["Witch"] = CreateUI("WitchCrest", 1f, true);
        KnownItems["Toolmaster"] = CreateUI("ArchitectCrest", 1f, true);
        KnownItems["Spell"] = CreateUI("ShamanCrest", 1f, true, true);
        KnownItems["Cloakless"] = CreateUI("CloaklessCrest", 1.2f, true);
        KnownItems["Cursed"] = CreateUI("CursedWitchCrest", 1.8f, true, true);

        MapCategory(CreateUI("HuntersUpgrade", 0.7f, true), "Hunter", "Hunter_v2", "Hunter_v3");
        MapCategory(CreateUI("VesticrestUpgrade", 0.8f, true), "UnlockedExtraBlueSlot", "UnlockedExtraYellowSlot");
        KnownItems["HasBoundCrestUpgrader"] = CreateUI("Sylphsong", 1f, true, true);

        #endregion


        WeightedEvent CreateUI(string identifier, float defaultWeight, bool defaultOn, bool gapBelow = false, string? categoryLabel = null)
        {
            return WeightedEvent.CreateWithUI(identifier, defaultWeight, defaultOn, parent, this, _enabled, _scaleWithWeighting, !gapBelow, categoryLabel);
        }
        void MapCategory(WeightedEvent evt, params string[] ids)
        {
            foreach (var id in ids) KnownItems[id] = evt;
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
    private void OnSetInt(string intName, int value)
    {
        if (!KnownItems.ContainsKey(intName)) return;
        if (_lastValues.TryGetValue(intName, out var prev) && prev is int prevInt && value <= prevInt) return;
        _lastValues[intName] = value;
        Log($"SET INT CHANGED: {intName} = {value}");
        TryActivate(intName);
    }
    private void OnSetBool(string boolName, bool value)
    {
        if (!value || !KnownItems.ContainsKey(boolName)) return;
        if (_lastValues.TryGetValue(boolName, out var prev) && prev is bool prevBool && prevBool) return;
        _lastValues[boolName] = value;
        Log($"SET BOOL CHANGED: {boolName} = true");
        TryActivate(boolName);
    }
    private void ItemPickup(SavedItem item)
    {
        Log($"GOT ITEM: {item.GetType().Name} - {item.name} (unique: {item.IsUnique})");
        if (item is ToolItem) return; // Handled by OnToolUnlockHook
        TryActivate(item.name);
    }
    private void ToolUnlock(ToolItem tool)
    {
        Log($"GOT TOOL: {tool.GetType().Name} - {tool.name}");
        TryActivate(tool.name);
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
    private void OnMaxHealthUp() => TryActivate("fullHeart");
    private void OnMaxSilkUp() => TryActivate("fullSpool");
    private void OnHeartPieceCollected() => TryActivate("heartPieces");
    private void OnSpoolFragmentCollected() => TryActivate("silkSpoolParts");
    private void OnPlayerDataSetVariable(string fieldName, object value)
    {
        if (!KnownItems.ContainsKey(fieldName)) return;
        // Diff against snapshot — only trigger on actual change
        if (_lastValues.TryGetValue(fieldName, out var prev))
        {
            if (value is bool curBool && prev is bool prevBool && curBool == prevBool) return;
            if (value is int curInt && prev is int prevInt && curInt <= prevInt) return;
        }
        _lastValues[fieldName] = value;
        Log($"PD SETVAR CHANGED: {fieldName} = {value}");
        TryActivate(fieldName);
    }
}