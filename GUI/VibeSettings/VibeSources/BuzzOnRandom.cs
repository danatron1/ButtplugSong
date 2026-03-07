using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GlobalSettings;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources
{
    internal class BuzzOnRandom : VibeSourceWithPunctuate
    {
        private readonly IntegerField _randomOdds;
        public int RandomOdds { get => _randomOdds.value; set => _randomOdds.value = value; }

        float _timeSinceLastRoll = 0;
        protected override string _punctuateReminderDescription => "getting unlucky";

        public BuzzOnRandom() : base("Random", true, 100, 10)
        {
            _randomOdds = Get<IntegerField>("RandomOdds");

            _randomOdds.SetupSaving(7200).SetupValueClamping(1, 99999).DependsOn(_enabled);

            Vibe.NeedsUpdate += Update;
        }
        public override void SetToPreset(Preset preset)
        {
            base.SetToPreset(preset);
            _randomOdds.Load(preset);
        }
        private void Update(float realTime, float timerTime)
        {
            if (!Enabled || timerTime <= float.Epsilon) return;
            _timeSinceLastRoll += timerTime;
            if (_timeSinceLastRoll > 1)
            {
                _timeSinceLastRoll -= 1;
                RollForVibes();
            }
        }

        private void RollForVibes()
        {
            int roll = ExtHelper.rng.Next(RandomOdds);
            if (roll == 0) Activate();
            if (roll == 1 && Gameplay.LuckyDiceTool.IsEquipped) Activate(); //gotta debuff the best tool somehow :)
        }
    }
}
