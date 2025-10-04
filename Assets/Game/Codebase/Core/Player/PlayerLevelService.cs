using System;
using Game.Configs;

namespace Game.Core.Player
{
    /// <summary>
    /// Runtime service that wraps the pure PlayerLevel and exposes it via DI.
    /// No static state or events.
    /// </summary>
    public sealed class PlayerLevelService : IPlayerLevelService
    {
        private readonly PlayerLevel _level;

        public event Action<PlayerLevel.LevelChangedEvent> LevelChanged
        {
            add => _level.LevelChanged += value;
            remove => _level.LevelChanged -= value;
        }

        public event Action<PlayerLevel.ExperienceChangedEvent> ExperienceChanged
        {
            add => _level.ExperienceChanged += value;
            remove => _level.ExperienceChanged -= value;
        }

        public PlayerLevelService(PlayerConfig config)
        {
            _level = new PlayerLevel(config);
        }

        public int Level => _level.Level;
        public int CurrentXpIntoLevel => _level.CurrentXpIntoLevel;
        public long TotalXp => _level.TotalXp;
        public int XpToNextLevel => _level.XpToNextLevel;
        public int MaxLevel => _level.MaxLevel;
        public bool IsMaxLevel => _level.IsMaxLevel;
        public float LevelProgress01 => _level.LevelProgress01;

        public int AddExperience(int amount) => _level.AddExperience(amount);
        public void Reset() => _level.Reset();
    }
}
