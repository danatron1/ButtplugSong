using System.Collections.Generic;

namespace ButtplugSong.GUI.VibeSettings.Presets
{
    internal class PresetComboMeter : Preset
    {
        public PresetComboMeter() : base("ComboMeter")
        {
        }

        protected override Dictionary<string, object> GetSettings()
        {
            //TODO: Add more presets
            return new();
        }
    }
}
