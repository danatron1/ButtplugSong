using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI;

internal class UISettings : GUISection, IPresetLoadable
{
    private static readonly string[] UIScaleOptions =
    [
        "0.6x",
        "0.8x",
        "1x",
        "1.2x",
        "1.4x",
        "1.6x",
        "1.8x",
        "2x"
     ];
    private static readonly Dictionary<string, string> UIHeightOptions = new()
    {
        {"Short", "tab-view-short" },
        {"Medium", "tab-view-medium" },
        {"Long", "tab-view-long" },
    };

    protected readonly DropdownField _uiScale;
    protected readonly DropdownField _uiHeight;
    protected readonly Toggle _displayWaveform;
    protected readonly Toggle _autoCollapseSettings;
    public bool AutoCollapseSettings { get => _autoCollapseSettings != null && _autoCollapseSettings.value; set => _autoCollapseSettings.value = value; }
    public UISettings() : base("UI")
    {
        _uiScale = Get<DropdownField>("UIScale");
        _uiScale.PopulateDropdown(UIScaleOptions).SetupSaving().RegisterValueChangedCallback(UIScaleChanged);
        _uiHeight = Get<DropdownField>("UIHeight");
        _uiHeight.PopulateDropdown([.. UIHeightOptions.Select(x => x.Key)]).SetupSaving().RegisterValueChangedCallback(UIHeightChanged);
        _displayWaveform = Get<Toggle>("DisplayWaveform");
        _displayWaveform.SetupSaving().RegisterValueChangedCallback(DisplayWaveformChanged);
        _autoCollapseSettings = Get<Toggle>("AutoCollapseSettings");
        _autoCollapseSettings.SetupSaving();

        _uiScale.SetIndexWithoutNotify(2);
        _uiHeight.SetIndexWithoutNotify(1);
    }
    public void SetToPreset(Preset preset)
    {
        _uiScale.Load(preset);
        _uiHeight.Load(preset);
        _displayWaveform.Load(preset);
        _autoCollapseSettings.Load(preset);
    }

    private void DisplayWaveformChanged(ChangeEvent<bool> evt)
    {
        Vibe.UI.VibeDisplay.SetClassListIf("hide", x => !evt.newValue);
    }

    private void UIHeightChanged(ChangeEvent<string> evt)
    {
        string newStyle = UIHeightOptions.TryGetValue(evt.newValue, out string style) ? style : "tab-view-medium";
        foreach (var kvp in UIHeightOptions) Vibe.UI.MainTabView.RemoveFromClassList(kvp.Value);
        Vibe.UI.MainTabView.AddToClassList(newStyle);
    }

    private void UIScaleChanged(ChangeEvent<string> evt)
    {
        Vibe.UI.UIDoc.panelSettings.scale = evt.newValue switch
        {
            "0.6x" => 0.6f,
            "0.8x" => 0.8f,
            "1x" => 1f,
            "1.2x" => 1.2f,
            "1.4x" => 1.4f,
            "1.6x" => 1.6f,
            "1.8x" => 1.8f,
            "2x" => 2f,
            _ => 1f
        };
    }
}
