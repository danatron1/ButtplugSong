using System.Collections.Generic;

namespace ButtplugSong.GUI.VibeSettings.Presets;

public class PresetDefault : Preset
{
    private static PresetDefault? _instance = null;
    public static PresetDefault Instance
    {
        get
        {
            _instance ??= new PresetDefault();
            return _instance;
        }
    }
    // The default preset is handled differently to other presets, as it's constructed piece by piece as UI objects are loaded in.
    // Some default values are loaded as the object is loaded in, others are hardcoded independently below.
    public static void SaveDefaultSetting(string settingName, object value) => Instance.Settings.Add(settingName, value);

    public PresetDefault() : base("DefaultSettings")
    {
        _instance = this;
    }

    protected override Dictionary<string, object> GetSettings() => new();
}
