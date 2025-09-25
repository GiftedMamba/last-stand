using UnityEngine;

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

        public GameObject PlayerPrefab => _playerPrefab;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float BaseDamage => _baseDamage;
        public float BaseAttackSpeed => _baseAttackSpeed;
        public float BaseProjectileSpeed => _baseProjectileSpeed;
    }
}
