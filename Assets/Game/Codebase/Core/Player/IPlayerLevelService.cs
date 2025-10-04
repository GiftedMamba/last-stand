using System;

namespace Game.Core.Player
{
    /// <summary>
    /// Abstraction to access and mutate player level progression.
    /// Implemented as a VContainer-registered singleton service.
    /// </summary>
    public interface IPlayerLevelService
    {
        int Level { get; }
        int CurrentXpIntoLevel { get; }
        long TotalXp { get; }
        int XpToNextLevel { get; }
        int MaxLevel { get; }
        bool IsMaxLevel { get; }
        float LevelProgress01 { get; }

        /// <summary>
        /// Adds XP to the player. Returns number of levels gained.
        /// </summary>
        int AddExperience(int amount);

        /// <summary>
        /// Resets progression to level 1.
        /// </summary>
        void Reset();

        event Action<PlayerLevel.LevelChangedEvent> LevelChanged;
        event Action<PlayerLevel.ExperienceChangedEvent> ExperienceChanged;
    }
}
