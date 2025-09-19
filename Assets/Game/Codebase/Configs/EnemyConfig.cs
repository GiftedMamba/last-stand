using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Configuration for an enemy type. Create assets from the menu and assign a prefab
    /// with an Enemy component attached.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Configs/Enemy Config", order = 1)]
    public class EnemyConfig : ScriptableObject
    {
        [Header("References")]
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Stats (from GDD)")]
        [SerializeField, Min(1)] private int _maxHp = 100;
        [SerializeField, Min(0)] private int _armor = 0; // flat armor, 6% reduction per point up to 60%
        [SerializeField, Min(0f)] private float _moveSpeed = 2f; // u/s
        [SerializeField, Min(0f)] private float _dpsToSpot = 8f; // when in contact (future use)
        [SerializeField, Min(0)] private int _explodeDamageToTower = 10; // damage dealt to tower when enemy explodes on it
        [SerializeField] private bool _isBoss = false;

        public GameObject EnemyPrefab => _enemyPrefab;
        public int MaxHp => _maxHp;
        public int Armor => _armor;
        public float MoveSpeed => _moveSpeed;
        public float DpsToSpot => _dpsToSpot;
        public int ExplodeDamageToTower => _explodeDamageToTower;
        public bool IsBoss => _isBoss;
    }
}
