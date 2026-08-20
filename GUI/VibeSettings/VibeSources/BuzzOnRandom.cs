using ButtplugSong.GUI.VibeSettings.Presets;
using ButtplugSong.Helper;
using GlobalSettings;
using System;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.VibeSettings.VibeSources
{
    internal class BuzzOnRandom : VibeSourceWithPunctuate
    {
        private readonly IntegerField _randomOdds;
        public int RandomOdds { get => _randomOdds.value; set => _randomOdds.value = value; }
        private readonly Toggle _randomizePower;
        public bool RandomizePower { get => _randomizePower.value; set => _randomizePower.value = value; }
        private readonly Toggle _randomizeTime;
        public bool RandomizeTime { get => _randomizeTime.value; set => _randomizeTime.value = value; }
        private readonly Toggle _periodic;
        private readonly Label _periodicReminder;
        public bool Periodic { get => _periodic.value; set => _periodic.value = value; }


        float _timeSinceLastRoll = 0;
        protected override string _punctuateReminderDescription => "getting unlucky";

        public BuzzOnRandom() : base("Random", true, 100, 10)
        {
            _randomOdds = Get<IntegerField>("RandomOdds");
            _randomOdds.SetupSaving(7200).SetupValueClamping(1, 99999).DependsOn(_enabled).RegisterValueChangedCallback(OddsUpdated);
            _randomizePower = Get<Toggle>("RandomizePower");
            _randomizePower.SetupSaving(false).DependsOn(_enabled);
            _randomizeTime = Get<Toggle>("RandomizeTime");
            _randomizeTime.SetupSaving(false).DependsOn(_enabled);
            _periodic = Get<Toggle>("PeriodicMode");
            _periodic.SetupSaving(false).DependsOn(_enabled);
            _periodicReminder = Get<Label>("PeriodicMode-Reminder");

            Vibe.NeedsUpdate += Update;
        }
        private void OddsUpdated(ChangeEvent<int> evt)
        {
            _periodicReminder.text = $"Instead of activating randomly, this now activates exactly " +
                $"every {evt.newValue} second{(evt.newValue == 1 ? "" : "s")}.";
        }
        public override void SetToPreset(Preset preset)
        {
            base.SetToPreset(preset);
            _randomOdds.Load(preset);
            _randomizePower.Load(preset);
            _randomizeTime.Load(preset);
            _periodic.Load(preset);
        }
        private void Update(float realTime, float timerTime)
        {
            if (!Enabled || timerTime <= float.Epsilon) return;
            _timeSinceLastRoll += timerTime;
            if (Periodic && _timeSinceLastRoll > RandomOdds)
            {
                _timeSinceLastRoll -= RandomOdds;
                ActivateRandomly();
            }
            else if (!Periodic && _timeSinceLastRoll > 1)
            {
                _timeSinceLastRoll %= 1;
                RollForVibes();
            }
        }
        private void RollForVibes()
        {
            int roll = ExtHelper.rng.Next(RandomOdds);
            if (roll == 0) ActivateRandomly();
            if (roll == 1 && Gameplay.LuckyDiceTool.IsEquipped) ActivateRandomly(); //gotta debuff the best tool somehow :)
        }
        private void ActivateRandomly()
        {
            float power = RandomizePower ? Power * (float)ExtHelper.rng.NextDouble() : Power;
            float time = RandomizeTime ? Time * (float)ExtHelper.rng.NextDouble() : Time;
            Activate(power, time);
        }
    }
}
