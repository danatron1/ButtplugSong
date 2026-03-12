using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace ButtplugSong
{
    public static class DependencyPatcher
    {
        public static IEnumerable<string> TargetDLLs => Array.Empty<string>();

        public static void Initialize()
        {
            string libsDir = Path.Combine(
                BepInEx.Paths.PluginPath,
                "danatron1-ButtplugSong",
                "libs");

            foreach (string dll in new[] { "Newtonsoft.Json.dll", "Buttplug.dll" })
            {
                string path = Path.Combine(libsDir, dll);
                if (File.Exists(path))
                {
                    try { Assembly.Load(File.ReadAllBytes(path)); }
                    catch { }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string dllName = new AssemblyName(args.Name).Name + ".dll";
                string path = Path.Combine(libsDir, dllName);
                return File.Exists(path) ? Assembly.Load(File.ReadAllBytes(path)) : null;
            };
        }

        public static void Patch(AssemblyDefinition assembly) { }
    }
}
