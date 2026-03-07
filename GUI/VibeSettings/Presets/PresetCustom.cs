using GoodVibes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ButtplugSong.GUI.VibeSettings.Presets;

public class PresetCustom : Preset
{
    private enum SavingStatus
    {
        Ready,
        Queued,
        Saving
    }
    private static PresetCustom? _instance = null;
    public static PresetCustom Instance
    {
        get
        {
            _instance ??= new PresetCustom();
            return _instance;
        }
    }
    private static string UserSettingsPath => Path.Combine(ButtplugSongPlugin.ModPath, "SavedSettings.txt");
    private bool saveQueued = false;
    private bool savingInProgress = false;

    public PresetCustom() : base("Custom")
    {
        _instance = this;
        VibeManager.Instance.NeedsUpdate += Update;
    }
    protected override Dictionary<string, object> GetSettings()
    {
        Dictionary<string, object> settings = new();

        //Load settings from file
        if (File.Exists(UserSettingsPath))
        {
            foreach (string line in File.ReadAllLines(UserSettingsPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length < 2) continue;
                settings.Add(parts[0], parts[1]);
            }
        }

        //settings must have base preset.
        if (!settings.ContainsKey(BasePresetString)) settings[BasePresetString] = Default.Identifier;

        return settings;
    }

    internal void SaveSettingChange<T>(string settingName, T newValue) where T : struct
    {
        object? valueBefore = Settings.TryGetValue(settingName, out object value) ? value : null;
        Settings[settingName] = newValue;
        //if this preset would have the same setting from inherited base class alone
        if (BasePreset != null && BasePreset.TryGet(settingName, out T baseValue) && baseValue.Equals(newValue))
        {   //new setting value is now null
            Settings.Remove(settingName);
            if (valueBefore is not null) saveQueued = true;
        }
        else if (valueBefore is null || !newValue.Equals(valueBefore))
        {   //new setting value not null
            saveQueued = true;
        }
    }
    internal void SaveSettingChange(string settingName, string newValue)
    {
        object? valueBefore = Settings.TryGetValue(settingName, out object value) ? value : null;
        Settings[settingName] = newValue;

        if (BasePreset != null && BasePreset.TryGetString(settingName, out string baseValue) && baseValue.Equals(newValue))
        {   //new setting value is now null
            Settings.Remove(settingName);
            if (valueBefore is not null) saveQueued = true;
        }
        else if (valueBefore is null || !newValue.Equals(valueBefore))
        {   //new setting value not null
            saveQueued = true;
        }
    }
    internal void Update(float realTime, float timerTime)
    {
        if (!savingInProgress && saveQueued)
        {
            savingInProgress = true;
            Task.Run(() => SaveUserFile(Settings));
        }
    }
    internal async void SaveUserFile(Dictionary<string, object> settings)
    {
        File.WriteAllLines(UserSettingsPath, settings.Select(x => $"{x.Key}={x.Value}"));
        saveQueued = false;
        savingInProgress = false;
    }
}
