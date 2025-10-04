using System;
using System.Collections.Generic;
using Game.Configs;

namespace Game.Core.Player
{
    /// <summary>
    /// Tracks player's level and experience using thresholds from <see cref="PlayerConfig"/>.
    /// Pure C# (no Unity dependencies) so it can be tested in EditMode or Domain.
    /// </summary>
    public sealed class PlayerLevel
    {
        public readonly struct LevelChangedEvent
        {
            public LevelChangedEvent(int newLevel, int levelsGained)
            {
                NewLevel = newLevel;
                LevelsGained = levelsGained;
            }

            public int NewLevel { get; }
            public int LevelsGained { get; }
        }

        public readonly struct ExperienceChangedEvent
        {
            public ExperienceChangedEvent(int currentIntoLevel, int requiredToNext, long totalExperience)
            {
                CurrentIntoLevel = currentIntoLevel;
                RequiredToNext = requiredToNext;
                TotalExperience = totalExperience;
            }

            public int CurrentIntoLevel { get; }
            public int RequiredToNext { get; }
            public long TotalExperience { get; }
        }

        private readonly IReadOnlyList<int> _xpToNext; // index 0 == L1->L2, ...

        private int _level;                // 1-based
        private int _xpInLevel;            // progress within current level
        private long _totalXp;             // accumulated overall XP

        public event Action<LevelChangedEvent> LevelChanged;
        public event Action<ExperienceChangedEvent> ExperienceChanged;

        public PlayerLevel(PlayerConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _xpToNext = config.ExperienceToNextLevel ?? Array.Empty<int>();
            Reset();
        }

        /// <summary>
        /// Current level, starting from 1.
        /// </summary>
        public int Level => _level;

        /// <summary>
        /// Current XP progress into the current level (resets on level-up).
        /// </summary>
        public int CurrentXpIntoLevel => _xpInLevel;

        /// <summary>
        /// Total accumulated XP (does not reset on level-up).
        /// </summary>
        public long TotalXp => _totalXp;

        /// <summary>
        /// Returns true if the player is at the maximum level (no next level defined in config).
        /// </summary>
        public bool IsMaxLevel => _level >= MaxLevel;

        /// <summary>
        /// The maximum attainable level according to the config. Equals thresholds count + 1.
        /// </summary>
        public int MaxLevel => (_xpToNext?.Count ?? 0) + 1;

        /// <summary>
        /// XP required to reach the next level from the current one. Returns 0 if at max level.
        /// </summary>
        public int XpToNextLevel => IsMaxLevel ? 0 : _xpToNext[_level - 1];

        /// <summary>
        /// Returns 0..1 progress within the current level. If at max level, returns 1 if any progress exists, otherwise 0.
        /// </summary>
        public float LevelProgress01
        {
            get
            {
                if (IsMaxLevel)
                    return _xpInLevel > 0 ? 1f : 0f;
                int req = XpToNextLevel;
                return req <= 0 ? 0f : Math.Clamp((float)_xpInLevel / req, 0f, 1f);
            }
        }

        /// <summary>
        /// Resets level to 1 and clears XP progress and totals.
        /// </summary>
        public void Reset()
        {
            _level = 1;
            _xpInLevel = 0;
            _totalXp = 0;
            ExperienceChanged?.Invoke(new ExperienceChangedEvent(_xpInLevel, XpToNextLevel, _totalXp));
            LevelChanged?.Invoke(new LevelChangedEvent(_level, 0));
        }

        /// <summary>
        /// Adds experience and handles multiple level-ups if thresholds are crossed.
        /// Returns how many levels were gained as a result of this addition.
        /// </summary>
        public int AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            _totalXp += amount;

            int levelsGained = 0;
            int remaining = amount;

            while (remaining > 0 && !IsMaxLevel)
            {
                int req = XpToNextLevel;
                int need = Math.Max(0, req - _xpInLevel);
                int take = Math.Min(remaining, need);

                _xpInLevel += take;
                remaining -= take;

                // Level up if we've met or exceeded the requirement
                if (_xpInLevel >= req && req > 0)
                {
                    _level++;
                    _xpInLevel = 0;
                    levelsGained++;
                    LevelChanged?.Invoke(new LevelChangedEvent(_level, levelsGained));
                    // Loop to process remaining XP into next levels
                }
                else
                {
                    break; // not enough to level up now
                }
            }

            // At max level, we do not accumulate progress toward a non-existent next level
            if (IsMaxLevel)
            {
                _xpInLevel = 0;
            }

            ExperienceChanged?.Invoke(new ExperienceChangedEvent(_xpInLevel, XpToNextLevel, _totalXp));
            return levelsGained;
        }
    }
}
