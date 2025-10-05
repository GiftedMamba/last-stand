using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Gameplay.Enemies;

namespace Game.Configs
{
    /// <summary>
    /// ScriptableObject containing a list of waves for a level/session.
    /// Each wave defines the time it becomes active, the enemy types that can spawn during the wave,
    /// and an optional boss type for that wave.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Game/Configs/Wave Config", order = 2)]
    public class WaveConfig : ScriptableObject
    {
        [SerializeField]
        private List<Wave> _waves = new();

        public IReadOnlyList<Wave> Waves => _waves;

        [Serializable]
        public class Wave
        {
            [Tooltip("Game time in seconds when this wave starts or becomes active.")]
            [Min(0f)]
            public float Time;

            [Tooltip("Enemy types that may spawn during this wave.")]
            public List<EnemyType> EnemyTypes = new();

            [Tooltip("Boss type for this wave (use an enemy type that represents a boss). Unknown means no boss.")]
            public EnemyType BossType = EnemyType.Unknown;
        }
    }
}
