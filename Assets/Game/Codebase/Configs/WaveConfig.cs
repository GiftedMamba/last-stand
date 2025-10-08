using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Gameplay.Enemies;

namespace Game.Configs
{
    /// <summary>
    /// ScriptableObject containing a list of waves for a level/session.
    /// Each wave defines its duration, the enemy types that can spawn during the wave,
    /// an optional boss type for that wave, and spawn pacing parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Game/Configs/Wave Config", order = 2)]
    public class WaveConfig : ScriptableObject
    {
        [Header("Global Settings")]
        [Tooltip("Time (seconds) to wait between waves before the next one starts.")]
        [Min(0f)] public float TimeBetweenWaves = 10f;

        [SerializeField]
        private List<Wave> _waves = new();

        public IReadOnlyList<Wave> Waves => _waves;

        [Serializable]
        public class Wave
        {
            [Tooltip("Duration of this wave in seconds.")]
            [Min(0f)]
            public float Time;

            [Tooltip("Enemy types that may spawn during this wave.")]
            public List<EnemyType> EnemyTypes = new();

            [Tooltip("Boss type for this wave (use an enemy type that represents a boss). Unknown means no boss.")]
            public EnemyType BossType = EnemyType.Unknown;

            [Header("Spawn Pacing")]
            [Tooltip("Spawn period (seconds between spawns) at the start of the wave.")]
            [Min(0.01f)]
            public float StartSpawnPeriod = 2f;

            [Tooltip("Spawn period (seconds between spawns) at the end of the wave.")]
            [Min(0.01f)]
            public float EndSpawnPeriod = 1f;

            [Header("Early Clear")]
            [Tooltip("When all enemies are dead before the wave duration ends, wait this delay before ending the wave early. 0 disables early clear.")]
            [Min(0f)]
            public float ClearDelaySeconds = 3f;
        }
    }
}
