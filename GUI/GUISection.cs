using GoodVibes;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI;

public abstract class GUISection(string identifier)
{
    public string Identifier { get; private set; } = identifier;
    protected static VibeManager Vibe => VibeManager.Instance;
    protected static void Log(object message) => Log(message.ToString());
    protected static void Log(string message) => Vibe.Log(message);
    protected static T Get<T>(string elementName) where T : VisualElement => Get<T>(elementName, Vibe.UI.Root);
    protected static T Get<T>(string elementName, VisualElement root) where T : VisualElement
    {
        T found = root.Q<T>(elementName);
        if (found == null)
        {
            Log($"Searched for {elementName} but couldn't find it");
            throw new System.NullReferenceException($"Could not find {elementName} under root {root.name}");
        }
        return found;
    }
}
