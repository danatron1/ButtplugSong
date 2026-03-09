using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources;

internal class WeightedEvent : GUISection
{
    protected readonly Toggle _enabled;
    public bool Enabled { get => _enabled.value; set => _enabled.value = value; }
    public string EnabledText { get => _enabled.text; set => _enabled.text = value; }

    protected readonly FloatField _weight;
    public float Weight { get => _weight.value; set => _weight.value = value; }
    public WeightedEvent(string identifier, float defaultWeight, Toggle enabledDependsOn, Toggle weightDependsOn, bool defaultOn = true) :
        this(identifier, defaultWeight,
            Get<GroupBox>(identifier).Q<Toggle>("Enabled"),
            Get<GroupBox>(identifier).Q<FloatField>("Weight"),
            enabledDependsOn, weightDependsOn, defaultOn)
    { }
    public WeightedEvent(string identifier, float defaultWeight, Toggle enabled, FloatField weight, Toggle enabledDependsOn, Toggle weightDependsOn, bool defaultOn) : base(identifier)
    {
        _enabled = enabled;
        _weight = weight;

        _enabled.DependsOn(enabledDependsOn);
        _weight.DependsOn(_enabled, weightDependsOn, enabledDependsOn);

        _enabled.SetupSaving(defaultOn, Identifier);
        _weight.SetupSaving(defaultWeight, $"{Identifier}Weight").SetupValueClamping(0, 999).SetupGreyout(x => x == 0);
    }
    public void Load(Preset preset)
    {
        _enabled.value = preset.Get<bool>(Identifier) ?? _enabled.value;
        _weight.value = preset.Get<float>($"{Identifier}Weight") ?? _weight.value;
    }
    public static WeightedEvent CreateWithUI(string identifier, float defaultWeight, bool defaultOn, VisualElement parent, VibeSource source, Toggle enabledDependsOn, Toggle weightDependsOn, bool cosyWithBelow = false, string? newCategoryLabel = null)
    {
        if (newCategoryLabel != null)
        {
            Label categoryLabel = new(newCategoryLabel);
            categoryLabel.AddToClassList("weighted-event-category");
            parent.Add(categoryLabel);
        }

        GroupBox group = new(string.Empty);
        group.name = $"{source.Identifier}{identifier}"; //e.g. "RosaryString" -> "PickupRosaryString
        group.AddToClassList("weighted-event");

        Toggle enable = new(string.Empty)
        {
            text = identifier.FriendlyName(),
            value = defaultOn,
            enabledSelf = false
        };
        enable.AddToClassList("h3");
        if (cosyWithBelow) enable.AddToClassList("weighted-event-cosy");

        FloatField weight = new(string.Empty)
        {
            maxLength = 3,
            value = defaultWeight,
            enabledSelf = false
        };
        weight.AddToClassList("hEntry");
        weight.AddToClassList("weighted-event-weight");

        group.Add(enable);
        group.Add(weight);

        parent.Add(group);

        return new WeightedEvent(identifier, defaultWeight, enable, weight, enabledDependsOn, weightDependsOn, defaultOn);
    }
}