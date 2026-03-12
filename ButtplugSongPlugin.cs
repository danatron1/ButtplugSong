using BepInEx;
using GoodVibes;
using HarmonyLib;
using System.IO;

namespace ButtplugSong
{
    [BepInAutoPlugin(id: ModId, name: ModName, version: ModVersion)]
    public partial class ButtplugSongPlugin : BaseUnityPlugin
    {
        public static string ModPath = "Not yet loaded";

        private const string ModId = "danatron1-ButtplugSongMod-Silksong";
        private const string ModName = "ButtplugSong";
        private const string ModVersion = "1.1.5"; //When updating, also change in thunderstore.toml & Directory.Build.props

        private readonly Harmony harmony = new(ModId);
        private VibeManager vibe;
        private void Awake()
        {
            ModPath = Path.GetDirectoryName(Info.Location);
            vibe = new VibeManager(ModPath, Logger.LogInfo);

            harmony.PatchAll();

            // Put your initialization logic here
            Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        }
    }
}
