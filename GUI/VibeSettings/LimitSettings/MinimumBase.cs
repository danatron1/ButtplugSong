using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;


internal abstract class MinimumBase : GUISection, IPresetLoadable
{
    protected static HeroController hero => HeroController.instance; //here because a lot of minimums use it.

    private static Toggle? __minimumsEnabledProp;
    internal static Toggle _minimumsEnabled
    {
        get
        {
            __minimumsEnabledProp ??= Vibe.UI.Limits._minimumsEnabled;
            return __minimumsEnabledProp;
        }
        set => __minimumsEnabledProp = value;
    }

    protected readonly Toggle _enabled;
    protected readonly FloatField _minAmount;
    protected VisualElement LocalRoot;
    public float MinAmount { get => _minAmount.value / 100; set => _minAmount.value = value.Clamp(0, 1) * 100; }
    public float MinimumWhenActive => Mathf.Min(Vibe.Logic.MaxPower, MinAmount * _GetScaleEnabled());
    public bool Active => _enabled.value && IsRelevant();
    public float Minimum => Active ? MinimumWhenActive : 0;
    public MinimumBase(string identifier, bool enabledByDefault, float defaultAmount) : base(identifier)
    {
        LocalRoot = Get<GroupBox>($"Minimum{Identifier}");
        _enabled = LocalRoot.Q<Toggle>($"EnableToggle");
        _enabled.SetupSaving(enabledByDefault, $"Min{Identifier}").DependsOn(_minimumsEnabled);
        _minAmount = LocalRoot.Q<FloatField>($"Amount");
        _minAmount.SetupSaving(defaultAmount, $"Min{Identifier}Amount").DependsOn(_minimumsEnabled, _enabled).SetupValueClamping(0, 100).SetupGreyout(x => x == 0);
    }
    public abstract bool IsRelevant();
    protected virtual float _GetScaleEnabled() => 1f;
    public virtual void SetToPreset(Preset preset)
    {
        _enabled.Load(preset);
        _minAmount.Load(preset);
    }
}
internal abstract class MinimumWithScale : MinimumBase
{
    protected readonly Toggle _scales;
    public MinimumWithScale(string identifier, bool enabledByDefault, float defaultAmount, bool scalesByDefault) : base(identifier, enabledByDefault, defaultAmount)
    {
        _scales = LocalRoot.Q<Toggle>($"Scale");
        _scales.SetupSaving(scalesByDefault, $"Min{Identifier}Scales").DependsOn(_enabled);
    }
    protected override float _GetScaleEnabled() => _scales.value ? GetScale() : 1f; // Don't override this! Override GetScale() instead!
    protected abstract float GetScale();
    public override void SetToPreset(Preset preset)
    {
        base.SetToPreset(preset);
        _scales.Load(preset);
    }
}
