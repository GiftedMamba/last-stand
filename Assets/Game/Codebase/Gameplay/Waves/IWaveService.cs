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
        /// True when the last wave has completed.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Invoked once when all waves are finished.
        /// </summary>
        event Action Finished;
    }
}
