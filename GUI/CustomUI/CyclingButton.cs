using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using System;
using UnityEngine.UIElements;
using WebSocketSharp;

namespace ButtplugSong.GUI.CustomUI;

public class CyclingButton<T> : GUISection where T : struct, Enum
{
    public T currentMode { get; private set; }
    public T defaultMode { get; private set; }
    public readonly Button button;
    public Func<T, string> ModeString;
    public CyclingButton(string buttonIdentifier, T defaultMode) : this(buttonIdentifier, defaultMode, x => x.ToString()) { }
    public CyclingButton(string buttonIdentifier, T defaultMode, Func<T, string> textSelector) : base(buttonIdentifier)
    {
        ModeString = textSelector;

        button = Get<Button>(Identifier);
        button.clicked += ButtonClicked;

        SetDefault(defaultMode);
        ResetToDefault();
    }
    public void ResetToDefault() => currentMode = defaultMode;
    private void SetDefault(T defaultMode) => this.defaultMode = defaultMode;

    public Action? clicked;
    private void ButtonClicked()
    {
        CycleMode();
        clicked?.Invoke();
    }
    public void CycleMode() => SetToMode(NextMode());
    public void SetToMode(T mode)
    {
        currentMode = mode;
        button.text = ModeString(currentMode);
    }
    private T NextMode() => currentMode.Next();

    internal void SetupSaving(T? defaultValue = null)
    {
        clicked += () => Preset.Custom.SaveSettingChange(Identifier, currentMode);
        if (defaultValue != null) PresetDefault.SaveDefaultSetting(Identifier, defaultValue);

    }
    internal void Load(Preset preset)
    {
        SetToMode(preset.Get<T>(Identifier) ?? currentMode);
        Preset.Custom.SaveSettingChange(Identifier, currentMode);
    }
}



//same thing but with a string[] instead of an enum


/*
internal class CyclingButton : GUISection
{
    public string currentMode { get; private set; }
    public string defaultMode { get; private set; }
    public string[] modes { get; private set; }
    public readonly Button button;
    public CyclingButton(string buttonIdentifier, params string[] modes) : base(buttonIdentifier)
    {
        if (modes.Length == 0) throw new ArgumentException("Cycling button needs at least one mode (option)");

        this.modes = modes;
        button = UI.Q<Button>(buttonIdentifier);
        button.clicked += ButtonClicked;

        SetDefault(modes[0]);
        ResetToDefault();
    }
    public void ResetToDefault()
    {
        currentMode = defaultMode;
    }
    private void SetDefault(string defaultMode)
    {
        this.defaultMode = defaultMode;
    }
    public Action? OnButtonClick;
    private void ButtonClicked()
    {
        CycleMode();
        OnButtonClick?.Invoke();
    }
    public void CycleMode() => SetToMode(NextMode());
    public void SetToMode(string mode)
    {
        if (!modes.Contains(mode)) throw new ArgumentException($"modes list does not contain mode \"{mode}\"");
        currentMode = mode;
    }
    private string NextMode() => NextMode(currentMode);
    private string NextMode(string afterMode)
    {
        if (!modes.Contains(currentMode)) throw new ArgumentException($"modes list does not contain mode \"{currentMode}\"");
        int index = modes.IndexOf(afterMode) + 1;
        if (index == modes.Length) return modes[0];
        return modes[index];
    }
}
*/