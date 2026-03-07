using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.Presets;

public abstract class Preset
{
    internal const string BasePresetString = "BasePreset";
    public static PresetDefault Default => PresetDefault.Instance;
    public static PresetCustom Custom => PresetCustom.Instance;
    public static Dictionary<string, Preset> All = new();

    public string Identifier;
    public Dictionary<string, object> Settings;
    public Preset? BasePreset
    {
        get
        {
            if (Settings.TryGetValue(BasePresetString, out object preset)) return PresetFromString(preset.ToString());
            return null;
        }
    }
    protected Preset(string identifier)
    {
        Identifier = identifier;
        Settings = GetSettings();
        All.Add(Identifier, this);
    }
    public void SetBasePreset(Preset other)
    {
        if (other == this) Settings.Remove(BasePresetString);
        else Settings[BasePresetString] = other.Identifier;
    }
    protected abstract Dictionary<string, object> GetSettings();
    public virtual object? GetSettingObject(string settingName)
    {
        if (Settings.TryGetValue(settingName, out object setting)) return setting;
        else if (BasePreset != null) return BasePreset.GetSettingObject(settingName);
        return null;
    }
    public virtual bool TryGetSettingObject(string settingName, [NotNullWhen(true)] out object setting)
    {
        object? foundSetting = GetSettingObject(settingName);
        if (foundSetting != null)
        {
            setting = foundSetting;
            return true;
        }
        setting = default;
        return false;
    }

    public string? Get(string settingName)
    {
        if (TryGetSettingObject(settingName, out object setting))
        {
            return setting.ToString();
        }
        return null;
    }
    public virtual string? GetString(string settingName) => TryGetSettingObject(settingName, out object setting) ? setting.ToString() : null;
    public bool TryGetString(string settingName, out string setting)
    {
        if (TryGetSettingObject(settingName, out object settingObj))
        {
            setting = settingObj.ToString();
            return true;
        }
        setting = string.Empty;
        return false;
    }
    public virtual T? Get<T>(string settingName) where T : struct
    {
        if (TryGetSettingObject(settingName, out object setting))
        {
            if (typeof(T).IsEnum) return Enum.Parse<T>(setting.ToString());
            return (T)Convert.ChangeType(setting, typeof(T));
        }
        return null;
    }
    public virtual bool TryGet<T>(string settingName, out T setting) where T : struct
    {
        if (TryGetSettingObject(settingName, out object settingObj))
        {
            if (typeof(T).IsEnum) setting = Enum.Parse<T>(settingObj.ToString());
            else setting = (T)Convert.ChangeType(settingObj, typeof(T));
            return true;
        }
        setting = default;
        return false;
    }
    public static Preset? PresetFromString(string? presetName)
    {
        if (presetName == null) return null;
        if (All.TryGetValue(presetName, out Preset preset)) return preset;
        if (presetName.Contains(' ')) return PresetFromString(presetName.Replace(" ", ""));
        return null;
    }
    private static Dictionary<VisualElement, string> _settingsUIElements = new();
    private static HashSet<string> _seenSettingNames = new();
    public static void BindElementToSetting(VisualElement element, string settingName)
    {
        if (_seenSettingNames.Contains(settingName)) throw new ArgumentException($"Could not assign setting name {settingName} to {element.name} ({element.typeName}) as it is already assigned.");
        _settingsUIElements[element] = settingName;
        _seenSettingNames.Add(settingName);
    }
    public static bool TryGetBoundSetting(VisualElement element, out string settingName) => _settingsUIElements.TryGetValue(element, out settingName);
}
