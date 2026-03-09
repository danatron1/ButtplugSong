using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings;

internal class StopTheVibes : GUISection, IPresetLoadable
{
    protected readonly Button _stopTheVibes;
    protected readonly Label _stopTheVibesCostLabel;
    protected readonly Toggle _enabled;
    protected readonly Toggle _buttonCostsResources;
    protected readonly Toggle _buttonCostsRosaries;
    protected readonly Toggle _buttonCostsShards;
    protected readonly FloatField _rosaryCostPercent;
    protected readonly FloatField _shardCostPercent;
    protected readonly IntegerField _rosaryCostFlat;
    protected readonly IntegerField _shardCostFlat;
    protected readonly Toggle _buttonDisablesMinimums;
    protected readonly Toggle _lockSettings;

    public bool LockSettings { get => _lockSettings.value; set => _lockSettings.value = value; }

    public int RosaryCost = 0;
    public int ShardCost = 0;

    public StopTheVibes() : base("StopVibes")
    {
        _stopTheVibes = Get<Button>("StopVibes");
        _stopTheVibes.clicked += StopTheVibesButtonClicked;
        _stopTheVibesCostLabel = Get<Label>("StopVibesCostLabel");

        _enabled = Get<Toggle>("StopVibesEnabled");
        _enabled.SetupSaving(true).RegisterValueChangedCallback(RecalculateCosts);

        //COSTS
        _buttonCostsResources = Get<Toggle>("StopCostsResources");
        _buttonCostsResources.SetupSaving(true).DependsOn(_enabled).RegisterValueChangedCallback(RecalculateCosts);
        //rosaries
        _buttonCostsRosaries = Get<Toggle>("StopRosaryCost");
        _buttonCostsRosaries.SetupSaving(false).DependsOn(_enabled, _buttonCostsResources).RegisterValueChangedCallback(RecalculateCosts);
        _rosaryCostPercent = Get<FloatField>("StopRosaryCostPercent");
        _rosaryCostPercent.SetupSaving(50).DependsOn(_enabled, _buttonCostsResources, _buttonCostsRosaries).SetupValueClamping(0, 100).SetupGreyout(x => x == 0)
            .RegisterValueChangedCallback(RecalculateCosts);
        _rosaryCostFlat = Get<IntegerField>("StopRosaryCostFlat");
        _rosaryCostFlat.SetupSaving(0).DependsOn(_enabled, _buttonCostsResources, _buttonCostsRosaries).SetupValueClamping(0, 9999).SetupGreyout(x => x == 0)
            .RegisterValueChangedCallback(RecalculateCosts);
        //shards
        _buttonCostsShards = Get<Toggle>("StopShardCost");
        _buttonCostsShards.SetupSaving(true).DependsOn(_enabled, _buttonCostsResources).RegisterValueChangedCallback(RecalculateCosts);
        _shardCostPercent = Get<FloatField>("StopShardCostPercent");
        _shardCostPercent.SetupSaving(0).DependsOn(_enabled, _buttonCostsResources, _buttonCostsShards).SetupValueClamping(0, 100).SetupGreyout(x => x == 0)
            .RegisterValueChangedCallback(RecalculateCosts);
        _shardCostFlat = Get<IntegerField>("StopShardCostFlat");
        _shardCostFlat.SetupSaving(400).DependsOn(_enabled, _buttonCostsResources, _buttonCostsShards).SetupValueClamping(0, 800).SetupGreyout(x => x == 0)
            .RegisterValueChangedCallback(RecalculateCosts);

        _buttonDisablesMinimums = Get<Toggle>("ButtonDisablesMinimums");
        _buttonDisablesMinimums.SetupSaving(false).DependsOn(_enabled);
        _lockSettings = Get<Toggle>("LockSettings");
        _lockSettings.SetupSaving(false).RegisterValueChangedCallback(LockSettingsChanged);

        ModHooks.OnAddRosariesHook += CurrencyChanged;
        ModHooks.OnTakeRosariesHook += CurrencyChanged;
        ModHooks.OnAddShardsHook += CurrencyChanged;
        ModHooks.OnTakeShardsHook += CurrencyChanged;
    }
    public void SetToPreset(Preset preset)
    {
        _enabled.Load(preset);
        _buttonCostsResources.Load(preset);
        _buttonCostsRosaries.Load(preset);
        _rosaryCostPercent.Load(preset);
        _rosaryCostFlat.Load(preset);
        _buttonCostsShards.Load(preset);
        _shardCostPercent.Load(preset);
        _shardCostFlat.Load(preset);
        _buttonDisablesMinimums.Load(preset);
        _lockSettings.Load(preset);
    }

    private void LockSettingsChanged(ChangeEvent<bool> evt) => Vibe.UI.UpdateLockSettings();
    private int CurrencyChanged(PlayerData data, int amount)
    {
        RecalculateCosts();
        return amount;
    }
    private void RecalculateCosts<T>(ChangeEvent<T> evt) => RecalculateCosts();
    private void RecalculateCosts()
    {
        if (!_enabled.value || !_buttonCostsResources.value)
        {
            _stopTheVibes.enabledSelf = _enabled.value;
            _stopTheVibesCostLabel.text = _enabled.value ? "Free!" : "Button disabled";
            return;
        }

        RosaryCost = CalculateCost(CurrencyType.Money);
        ShardCost = CalculateCost(CurrencyType.Shard);

        if (RosaryCost == 0 && ShardCost == 0) _stopTheVibesCostLabel.text = "Free!";
        else if (RosaryCost == 0) _stopTheVibesCostLabel.text = $"Costs {ShardCost} shards";
        else if (ShardCost == 0) _stopTheVibesCostLabel.text = $"Costs {RosaryCost} rosaries";
        else _stopTheVibesCostLabel.text = $"Costs {RosaryCost} rosaries, {ShardCost} shards";

        _stopTheVibes.enabledSelf = CurrencyManager.GetCurrencyAmount(CurrencyType.Money) >= RosaryCost
                                 && CurrencyManager.GetCurrencyAmount(CurrencyType.Shard) >= ShardCost;

        int CalculateCost(CurrencyType type)
        {
            if (!_enabled.value || !_buttonCostsResources.value) return 0;
            int amount = CurrencyManager.GetCurrencyAmount(type);
            if (type == CurrencyType.Money && _buttonCostsRosaries.value)
            {
                amount = (int)(amount * _rosaryCostPercent.value / 100);
                return amount + _rosaryCostFlat.value;
            }
            else if (type == CurrencyType.Shard && _buttonCostsShards.value)
            {
                amount = (int)(amount * _shardCostPercent.value / 100);
                return amount + _shardCostFlat.value;
            }
            return 0;
        }
    }
    private void StopTheVibesButtonClicked()
    {
        if (_buttonCostsResources.value)
        {
            if (_buttonCostsRosaries.value)
            {
                if (RosaryCost > CurrencyManager.GetCurrencyAmount(CurrencyType.Money)) return;
                CurrencyManager.TakeCurrency(RosaryCost, CurrencyType.Money);
            }
            if (_buttonCostsShards.value)
            {
                if (ShardCost > CurrencyManager.GetCurrencyAmount(CurrencyType.Shard)) return;
                CurrencyManager.TakeCurrency(ShardCost, CurrencyType.Shard);
            }
        }

        if (_buttonDisablesMinimums.value) Vibe.UI.Limits._minimumsEnabled.value = false;
        Vibe.Logic.VibeSourceActivation("Stop the Vibes!", 1, "-", 0, "+", 0);
    }
}
