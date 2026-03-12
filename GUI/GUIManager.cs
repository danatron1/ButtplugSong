using ButtplugSong.GUI.CustomUI;
using ButtplugSong.GUI.Network;
using ButtplugSong.GUI.VibeSettings;
using ButtplugSong.GUI.VibeSettings.LimitSettings;
using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.GUI.VibeSettings.VibeSources;
using ButtplugSong.Helper;
using GlobalEnums;
using GoodVibes;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI;

internal class GUIManager : MonoBehaviour
{
    private static GUIManager? _instance;
    public static GUIManager Instance
    {
        get
        {
            if (_instance == null) throw new NotImplementedException("Instantiate properly before referencing! >:(");
            return _instance;
        }
        private set => _instance = value;
    }

    public GameObject GUI_GameObject;
    public UIDocument UIDoc;
    public VisualElement Root;

    internal VisualTreeAsset LogRow;
    internal VisualTreeAsset Device;

    public VibeManager Vibe;
    public VisualTreeAsset TreeAsset;
    public TemplateContainer TreeContainer;
    public StyleSheet Style;
    public PanelSettings Settings;

    private const float _displayUpdateFrequency = 1f / 16;
    private float _timeSinceLastUpdate = 0.1f;

    public bool UIHidden { get; private set; } = false;

    #region Setup
    public static GUIManager CreateAndInitialize()
    {
        GameObject managerObject = new("ButtplugSong GUI Manager");
        Instance = managerObject.AddComponent<GUIManager>();
        DontDestroyOnLoad(managerObject);

        Instance.Vibe = VibeManager.Instance;
        Instance.Vibe.UI = Instance;

        Instance.LoadAssetBundle();
        Instance.CreateGUI();
        Instance.CreateUIHandles();
        Instance.AddHooks();

        return Instance;
    }
    public void LoadAssetBundle()
    {
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(ButtplugSongPlugin.ModPath, "buttplugsong.gui"));
        TreeAsset = assetBundle.LoadAsset<VisualTreeAsset>("ButtplugSong-UXML");
        Style = assetBundle.LoadAsset<StyleSheet>("ButtplugSong-USS");
        Settings = assetBundle.LoadAsset<PanelSettings>("ButtplugSong-PanelSettings");
        LogRow = assetBundle.LoadAsset<VisualTreeAsset>("ButtplugSong-LogRow");
        Device = assetBundle.LoadAsset<VisualTreeAsset>("ButtplugSong-Device");
        assetBundle.Unload(false);
    }
    public void CreateGUI()
    {
        GUI_GameObject = new GameObject("ButtplugSong GUI");
        UIDoc = GUI_GameObject.AddComponent<UIDocument>();

        UIDoc.visualTreeAsset = TreeAsset;
        UIDoc.panelSettings = Settings;
        Root = UIDoc.rootVisualElement;

        CreateWaveDisplay();

        GUI_GameObject.transform.SetParent(transform);

        Root.AddToClassList("hide");

        void CreateWaveDisplay()
        {
            try
            {
                WaveDisplayHolder = Root.Q("WaveDisplayHolder");
                VibeDisplay = new WaveDisplay()
                {
                    LineColor = new Color(1f, 184f / 255, 246f / 255, 0.8f),
                    GlowColor = new Color(152f / 255, 19f / 255, 143f / 255, 0.5f),
                    LineWidth = 1,
                    GlowWidth = 1.2f,
                    MinimumValue = 0,
                    MaximumValue = 1,
                    DefaultValue = 0,
                    LineBufferTop = 0.2f,
                    LineBufferBottom = 0.8f,
                    RecordSteps = 128,
                };
                VibeDisplay.SetSize(new Vector2(320, 35));
                VibeDisplay.AddToClassList("wave-display");
                WaveDisplayHolder.Add(VibeDisplay);
            }
            catch (Exception e)
            {
                Vibe.Log($"Failed to create WaveDisplay: {e.Message} - {e.InnerException} - {e.StackTrace}");
            }
        }
    }

    #endregion

    #region UIHandles
    public WaveDisplay VibeDisplay; //set upon creation
    public VisualElement WaveDisplayHolder; //set upon creation

    public Label DebugInfo;
    public VisualElement SettingsPanel;
    public TabView MainTabView;

    public Label TimerReadout;
    public Label PowerReadout;
    public Label PowerReadoutActual;
    public Label DeathRoll;

    public List<Foldout> SettingsFoldouts;
    public List<VibeSource> Sources;
    public LimitsSettings Limits;
    public WaveSettings Wave;
    public TimerSettings Timer;
    public PresetSettings Presets;
    public StopTheVibes StopVibes;

    public NetworkSettings Network;

    public UISettings UISettings;
    public GroupBox VibeLog;

    private void CreateUIHandles()
    {
        DebugInfo = Root.Q<Label>(nameof(DebugInfo));
        SettingsPanel = Root.Q<VisualElement>(nameof(SettingsPanel));
        MainTabView = Root.Q<TabView>(nameof(MainTabView));

        //Mini display
        TimerReadout = Root.Q<Label>(nameof(TimerReadout));
        PowerReadout = Root.Q<Label>(nameof(PowerReadout));
        PowerReadoutActual = Root.Q<Label>(nameof(PowerReadoutActual));

        DeathRoll = Root.Q<Label>(nameof(DeathRoll));

        //Settings foldouts - these are what get disabled when the settings are locked
        SettingsFoldouts =
        [
            Root.Q<Foldout>("VibeSources"),
            Root.Q<Foldout>("LimitsSettings"),
            Root.Q<Foldout>("WaveTypeSettings"),
            Root.Q<Foldout>("TimerSettings"),
            Root.Q<Foldout>("PresetsSettings"),
            Root.Q<Foldout>("StopVibesSettings"),
        ];

        //Vibe settings
        Sources =
        [
            new BuzzOnDamage(),
            new BuzzOnHeal(),
            new BuzzOnStrike(),
            new BuzzOnDeath(),
            new BuzzOnPickups(),
            new BuzzOnRumble(),
            new BuzzOnRandom(),
        ];

        Presets = new PresetSettings();
        Limits = new LimitsSettings();
        Wave = new WaveSettings();
        Timer = new TimerSettings();
        StopVibes = new StopTheVibes();

        //Network
        Network = new NetworkSettings();

        //GUI and Log
        UISettings = new UISettings();
        VibeLog = Root.Q<GroupBox>(nameof(VibeLog));

        LogActivity("Vibe Log Initialized!\nDebug logs are found in \"LogOutput.log\" in your BepInEx folder. This log is just for vibe activity.");

        DebugInfo.AddToClassList("hide");
    }
    #endregion
    public void FinishedLoading()
    {
        SetToPreset(Preset.Custom);
        Root.RemoveFromClassList("hide");

        //Doing here instead of in AddHooks as InputHandler needs time to load.
        if (InputHandler.Instance != null) InputHandler.Instance.OnCursorVisibilityChange += UpdateSettingsPanelVisibility;
    }
    public void SetToPreset(Preset preset)
    {
        Vibe.Log($"Setting to preset: {preset.Identifier} ({preset.Settings.Count})");
        foreach (VibeSource source in Sources)
        {
            source.SetToPreset(preset);
        }
        Limits.SetToPreset(preset);
        Wave.SetToPreset(preset);
        Timer.SetToPreset(preset);
        Presets.SetToPreset(preset);
        StopVibes.SetToPreset(preset);

        Network.SetToPreset(preset);
        UISettings.SetToPreset(preset);
    }

    #region Hooks
    public void AddHooks()
    {
        Vibe.Logic.PunctuateChanged += UpdatePunctuateGraphic;
        Vibe.Logic.AddActivityLog += LogActivity;
        Vibe.Logic.PowerChanged += UpdateLockSettings;
        Vibe.Logic.VibeSourceActivated += UpdateLockSettings;
    }
    public void UpdatePunctuateGraphic(bool punctuating)
    {
        VibeDisplay.SetClassListIf("punctuating", x => Vibe.Logic.Punctuating);
    }
    #endregion
    private const int MAX_VIBELOG_ENTRIES = 100;
    private Queue<VisualElement> vibeLogEntries = new();
    private DateTime? _lastLogEntry = null;
    public void LogActivity(object generalMessage) => LogActivity("Debug Message", generalMessage.ToString());
    public void LogActivity(string sourceLine, string vibeLine) => LogActivity(sourceLine, vibeLine, -1);
    public void LogActivity(string sourceLine, string vibeLine, float timerSeconds)
    {
        if (string.IsNullOrWhiteSpace(sourceLine) || string.IsNullOrWhiteSpace(vibeLine)) return;
        VisualElement logrow = LogRow.Instantiate();
        Label time = logrow.Q<Label>("TimeData");
        Label source = logrow.Q<Label>("SourceData");
        Label details = logrow.Q<Label>("VibeData");
        DateTime now = DateTime.Now;
        float? realSeconds = _lastLogEntry.HasValue ? (float)(now - _lastLogEntry.Value).TotalSeconds : null;
        string timeLine = $"[{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2}  :{now.Millisecond:D3}]     ";
        if (realSeconds.HasValue)
        {
            timeLine += $"Passed: {VibeLogic.DisplayTime(realSeconds.Value)} real";
            if (timerSeconds >= 0) timeLine += $", {VibeLogic.DisplayTime(timerSeconds)} timer";
        }
        else timeLine += "First log";

        source.text = sourceLine;
        details.text = vibeLine;
        time.text = timeLine;

        vibeLogEntries.Enqueue(logrow);
        VibeLog.Add(logrow);
        _lastLogEntry = now;
        while (vibeLogEntries.Count > MAX_VIBELOG_ENTRIES)
        {
            VibeLog.Remove(vibeLogEntries.Dequeue());
        }
    }
    private float deathRollTimeRemaining;
    public void Update()
    {
        //PASS UPDATES ON TO REST OF THE MOD
        float realTime = Time.unscaledDeltaTime, timerTime = Timer.GetTimerCountdown(Time.unscaledDeltaTime);
        Vibe.Update(realTime, timerTime);

        UpdateWaveDisplay();
        UpdatePowerDisplay();
        UpdateTimeDisplay();
        UpdateDeathRollDisplay();
        UpdateToggleUI();

        void UpdateToggleUI()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                UIHidden = !UIHidden;
                Root.SetClassListIf("hide", x => UIHidden);
            }
        }

        void UpdateWaveDisplay()
        {
            _timeSinceLastUpdate += realTime;
            while (_timeSinceLastUpdate >= _displayUpdateFrequency)
            {
                _timeSinceLastUpdate -= _displayUpdateFrequency;
                VibeDisplay?.PushRecordStep(Vibe.Logic.ActualPower);
            }
        }
        void UpdatePowerDisplay()
        {
            PowerReadout.text = $"{Vibe.Logic.TargetPower * 100:f0}%";

            if (Vibe.Logic.TargetPower != Vibe.Logic.ActualPower)
            {
                PowerReadoutActual.text = $"{Vibe.Logic.ActualPower * 100:f0}%";
                PowerReadoutActual.opacity = 0.75f;
            }
            else if (PowerReadoutActual.opacity > 0)
            {
                PowerReadoutActual.text = $"{Vibe.Logic.ActualPower * 100:f0}%";
                PowerReadoutActual.opacity -= realTime / 3;
            }
        }
        void UpdateTimeDisplay()
        {
            if (Vibe.Logic.Time >= 60) TimerReadout.text = $"{(int)(Vibe.Logic.Time / 60)}:{Vibe.Logic.Time % 60:00}";
            else TimerReadout.text = $"{Vibe.Logic.Time:f2}";
        }
        void UpdateDeathRollDisplay()
        {
            if (deathRollTimeRemaining <= 0) return;
            deathRollTimeRemaining -= realTime;
            if (deathRollTimeRemaining <= 0)
            {
                DeathRoll.AddToClassList("hide");
                DeathRoll.opacity = 0;
                deathRollTimeRemaining = 0;
            }
            else if (deathRollTimeRemaining <= 5) DeathRoll.opacity = (deathRollTimeRemaining / 5).Clamp(0, 1);
        }
    }
    private void UpdateSettingsPanelVisibility(bool isVisible)
    {
        SettingsPanel.SetClassListIf("hide", x => UISettings.AutoCollapseSettings && !isVisible);
    }
    internal void DisplayDeathDice(int rollAmount)
    {
        DeathRoll.text = $"Death roll result: {rollAmount}";
        DeathRoll.opacity = 1;
        DeathRoll.RemoveFromClassList("hide");
        deathRollTimeRemaining = 20 + Math.Abs(11 - rollAmount);
    }

    internal void UpdateLockSettings()
    {
        LockSettings(!Vibe.Logic.TimerZero && StopVibes != null && StopVibes.LockSettings);
    }
    private bool _settingsCurrentlyLocked = false;
    private void LockSettings(bool disabled = true)
    {
        if (_settingsCurrentlyLocked == disabled) return;
        _settingsCurrentlyLocked = disabled;
        foreach (Foldout foldout in SettingsFoldouts)
        {
            foldout.enabledSelf = !disabled;
        }
    }
}
