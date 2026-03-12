using BepInEx;
using GoodVibes;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace ButtplugSong
{
    [BepInAutoPlugin(id: ModId, name: ModName, version: ModVersion)]
    public partial class ButtplugSongPlugin : BaseUnityPlugin
    {
        static ButtplugSongPlugin()
        {
            string libsDir = Path.Combine(
                Path.GetDirectoryName(typeof(ButtplugSongPlugin).Assembly.Location),
                "libs");
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string dllName = new AssemblyName(args.Name).Name + ".dll";
                string path = Path.Combine(libsDir, dllName);
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
        }
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
