using ButtplugSong.Helper;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.Presets;

internal class PresetSettings : GUISection, IPresetLoadable
{
    public Preset[] Presets;
    private readonly DropdownField _presetSelector;
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
        _presetSelector.PopulateDropdown(Presets.Select(x => x.Identifier.FriendlyName()).ToArray());
        _setToPreset = Get<Button>("SetToPreset");
        _setToPreset.clicked += SetToPreset;
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
}
