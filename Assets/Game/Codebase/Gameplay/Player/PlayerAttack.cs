using Game.Configs;
using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Attacks the closest enemy using a projectile based on PlayerConfig stats.
    /// Keep this as a thin behaviour; no static usage; references are assigned by PlayerSpawner.
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Config & References")]
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private EnemyRegistry _enemyRegistry;
        [SerializeField] private Transform _firePoint;

        [Header("Overrides (optional)")]
        [SerializeField] private GameObject _projectilePrefabOverride;
        [SerializeField, Min(0f)] private float _maxTargetRange = 50f;
        [SerializeField, Min(0.01f)] private float _hitRadius = 0.25f;

        private float _cooldown;

        public void Init(PlayerConfig config, EnemyRegistry registry)
        {
            _config = config;
            _enemyRegistry = registry;
        }

        private void Update()
        {
            if (_config == null || _enemyRegistry == null)
                return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
                return;

            var closest = _enemyRegistry.FindClosest(_firePoint != null ? _firePoint.position : transform.position);
            if (closest == null)
                return;

            // Optional range check
            if (_maxTargetRange > 0f)
            {
                float sqr = ((_firePoint != null ? _firePoint.position : transform.position) - closest.transform.position).sqrMagnitude;
                if (sqr > _maxTargetRange * _maxTargetRange)
                    return;
            }

            FireAt(closest);
            // Reset cooldown by attack speed (attacks per second)
            float atkSpeed = Mathf.Max(0.01f, _config.BaseAttackSpeed);
            _cooldown = 1f / atkSpeed;
        }

        private void FireAt(Enemy target)
        {
            var spawnPos = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 0.5f;
            var spawnRot = Quaternion.LookRotation((target.transform.position - spawnPos).normalized, Vector3.up);

            var projectilePrefab = _projectilePrefabOverride != null ? _projectilePrefabOverride : _config.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                GameLogger.LogError("PlayerAttack: No projectile prefab assigned (override or in PlayerConfig).");
                return;
            }

            var go = Instantiate(projectilePrefab, spawnPos, spawnRot);
            var proj = go.GetComponent<Projectile>();
            if (proj == null)
            {
                GameLogger.LogError("PlayerAttack: Projectile prefab missing Projectile component.");
                Destroy(go);
                return;
            }

            proj.Init(target, Mathf.Max(0f, _config.BaseProjectileSpeed), Mathf.Max(0f, _config.BaseDamage), _hitRadius);
        }
    }
}
