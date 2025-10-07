using System;
using System.Collections.Generic;
using Game.Gameplay.Enemies;

namespace Game.Gameplay.Waves
{
    public interface IWaveService : IDisposable
    {
        /// <summary>
        /// Returns the currently allowed enemy types for spawning according to the active wave.
        /// Empty when no wave is active or waves finished.
        /// </summary>
        IReadOnlyList<EnemyType> AllowedTypes { get; }

        /// <summary>
        /// Remaining time in seconds for the current wave. Returns 0 when there is no active wave or waves are finished.
        /// </summary>
        float CurrentWaveRemaining { get; }

        /// <summary>
        /// Current wave number (1-based). Returns 0 when there is no active wave or waves are finished.
        /// </summary>
        int CurrentWaveNumber { get; }

        /// <summary>
        /// Total number of waves configured. Returns 0 when there is no wave configuration.
        /// </summary>
        int TotalWaves { get; }

        /// <summary>
        /// True when the last wave has completed.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Current spawn period (seconds between spawns) defined by active wave's pacing,
        /// linearly interpolated between StartSpawnPeriod and EndSpawnPeriod over the wave's progress.
        /// Returns a positive value; falls back to a sane default when no active wave.
        /// </summary>
        float CurrentSpawnPeriod { get; }

        /// <summary>
        /// Invoked once when all waves are finished.
        /// </summary>
        event Action Finished;
    }
}
