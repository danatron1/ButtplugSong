using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GoodVibes;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings;

internal class WaveSettings : GUISection, IPresetLoadable
{
    private static float[] WavePeriods =
    [
        0.25f, //0
        0.5f,  //1
        0.75f, //2
        1f,    //3
        1.5f,  //4
        2f,    //5
        3f,    //6
        4f,    //7
        6f,    //8
        8f,    //9
        12f,   //10
        16f,   //11
        24f,   //12
        32f    //13
    ];

    private const string RandomWaveString = "Random Wave";

    private readonly DropdownField _waveTypeSelector;
    private readonly SliderInt _wavePeriod;
    private readonly Label _wavePeriodLabel;
    private readonly Button _testWave;
    public string WaveTypeSelector { get => _waveTypeSelector.value; }
    public WaveSettings() : base("WaveType")
    {
        _waveTypeSelector = Get<DropdownField>("WaveTypeSelector");
        _wavePeriod = Get<SliderInt>("WavePeriod");
        _wavePeriodLabel = Get<Label>("WavePeriodLabel");
        _testWave = Get<Button>("TestWave");

        _waveTypeSelector
            .PopulateDropdown<WaveType>(RandomWaveString)
            .RegisterDropdownChangedCallback<WaveType>(WaveTypeChanged)
            .SetupSaving(WaveType.ConstantLinear.ToString(), "WaveType");
        _wavePeriod
            .SetupSaving(3, "WavePeriodOptionNumber") //name this so it's not mistaken for the actual period by someone reading the txt
            .DependsOn(_waveTypeSelector, x => !x.value.StartsWith("Constant"))
            .RegisterValueChangedCallback(WavePeriodChanged);

        _testWave.clicked += TestWaveClicked;
        Vibe.Logic.VibeSourceActivated += ReRandomizeWave;
    }
    public void SetToPreset(Preset preset)
    {
        _waveTypeSelector.Load(preset);
        _wavePeriod.Load(preset);
        WavePeriodChanged(); //no clue why this doesn't update on its own.
    }

    private void WavePeriodChanged(ChangeEvent<int> evt) => WavePeriodChanged();
    private void WavePeriodChanged()
    {
        Vibe.Logic.wavePeriod = WavePeriods[_wavePeriod.value];
        _wavePeriodLabel.text = WavePeriods[_wavePeriod.value].ToString();
    }
    private void TestWaveClicked() => Vibe.Logic.VibeSourceActivation("Test Button", 0.25f, "+", 2, "+", 0);
    private void WaveTypeChanged(string newValue, bool isEnum, WaveType type)
    {
        if (isEnum) Vibe.Logic.waveType = type;
    }
    internal void ReRandomizeWave()
    {
        if (WaveTypeSelector == RandomWaveString) Vibe.Logic.waveType = ExtHelper.ChooseRandom<WaveType>();
    }
}
