using System.Collections.Generic;
using UnityEngine;
using Game.Gameplay.Abilities;

namespace Game.Configs
{
    /// <summary>
    /// Player configuration. Use this ScriptableObject to define base player combat parameters
    /// and references used to spawn or initialize the player.
    /// Create via: Assets > Create > Game/Configs/Player Config.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Configs/Player Config", order = 0)]
    public class PlayerConfig : ScriptableObject
    {
        [Header("References")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Combat Base Stats")]
        [SerializeField, Min(0f)] private float _baseDamage = 10f;
        [SerializeField, Min(0f)] private float _baseAttackSpeed = 1f; // attacks per second
        [SerializeField, Min(0f)] private float _baseProjectileSpeed = 10f; // units per second
        [SerializeField, Range(1,5)] private int _attackCount = 1; // number of simultaneous shots (1..5)

        [Header("Abilities")]
        [SerializeField] private List<HeroAbility> _abilities = new();

        [Header("Progression")]
        [Tooltip("Each entry is the experience required to reach the next level from the current level. Index 0 = XP to go from level 1 to level 2, and so on.")]
        [SerializeField] private int[] _experienceToNextLevel = new int[0];

        public GameObject PlayerPrefab => _playerPrefab;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float BaseDamage => _baseDamage;
        public float BaseAttackSpeed => _baseAttackSpeed;
        public float BaseProjectileSpeed => _baseProjectileSpeed;
        public int AttackCount => _attackCount;
        public IReadOnlyList<HeroAbility> Abilities => _abilities;

        /// <summary>
        /// Ordered list of XP requirements to advance from level N to N+1.
        /// </summary>
        public IReadOnlyList<int> ExperienceToNextLevel => _experienceToNextLevel;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_experienceToNextLevel == null)
            {
                _experienceToNextLevel = new int[0];
                return;
            }

            for (int i = 0; i < _experienceToNextLevel.Length; i++)
            {
                if (_experienceToNextLevel[i] < 0)
                    _experienceToNextLevel[i] = 0;
            }
        }
#endif
    }
}
