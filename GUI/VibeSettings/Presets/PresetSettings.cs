using ButtplugSong.Helper;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.Presets;

internal class PresetSettings : GUISection, IPresetLoadable
{
    public Preset[] Presets;
    private readonly DropdownField _presetSelector;
    private readonly Label _presetDescription;
    private readonly Button _setToPreset;
    public PresetSettings() : base("Presets")
    {
        Presets =
        [
            Preset.Default,
            //new PresetComboMeter(),

            //do not put the Custom preset in here - it should not be selectable from the dropdown.
        ];

        _presetSelector = Get<DropdownField>("PresetSelector");
        _presetDescription = Get<Label>("PresetDescription");
        _presetSelector.PopulateDropdown(Presets.Select(x => x.Identifier.FriendlyName()).ToArray()).RegisterValueChangedCallback(DropdownChanged);
        _setToPreset = Get<Button>("SetToPreset");
        _setToPreset.clicked += SetToPreset;

        _presetDescription.RemoveFromClassList("h1"); //this would display an error if the mod failed to load :D
        _presetDescription.RemoveFromClassList("red-text");
    }
    private void SetToPreset()
    {
        Preset? preset = Preset.PresetFromString(_presetSelector.value);
        if (preset != null)
        {
            Preset.Custom.SetBasePreset(preset);
            Vibe.UI.SetToPreset(preset);
        }
    }
    public void SetToPreset(Preset preset)
    {
        if (preset == Preset.Custom)
        {
            _presetSelector.value = preset.BasePreset?.Identifier.FriendlyName() ?? "Custom";
        }
        else _presetSelector.value = preset.Identifier.FriendlyName();
    }
    private void DropdownChanged(ChangeEvent<string> evt)
    {
        _presetDescription.text = evt.newValue switch
        {
            "Default Settings" => "The default experience. Vibes as a punishment for making mistakes, such as taking damage.",
            _ => $"Description for preset {evt.newValue} not defined"
        };
    }
}
