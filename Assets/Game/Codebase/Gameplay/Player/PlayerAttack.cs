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

        // Logging guards to avoid per-frame spam
        private bool _warnedUninitialized;
        private bool _loggedNoTarget;
        private bool _loggedOutOfRange;

        public void Init(PlayerConfig config, EnemyRegistry registry)
        {
            _config = config;
            _enemyRegistry = registry;

            var projectileSrc = _projectilePrefabOverride != null ? _projectilePrefabOverride.name : (_config != null && _config.ProjectilePrefab != null ? _config.ProjectilePrefab.name : "null");
            int enemyCount = _enemyRegistry != null && _enemyRegistry.Enemies != null ? _enemyRegistry.Enemies.Count : -1;

            _warnedUninitialized = false;
        }

        private void Update()
        {
            // Check initialization
            if (_config == null || _enemyRegistry == null)
            {
                if (!_warnedUninitialized)
                {
                    GameLogger.LogWarning($"PlayerAttack: Not initialized. Missing {( _config == null ? "PlayerConfig" : "" )}{( _config == null && _enemyRegistry == null ? " and " : "" )}{( _enemyRegistry == null ? "EnemyRegistry" : "" )}. Expected to be wired by PlayerSpawner.Init.");
                    _warnedUninitialized = true;
                }
                return;
            }
            else if (_warnedUninitialized)
            {
                // Became initialized later
                _warnedUninitialized = false;
            }

            // Cooldown handling
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            // Acquire target
            var origin = _firePoint != null ? _firePoint.position : transform.position;
            var closest = _enemyRegistry.FindClosest(origin);
            if (closest == null)
            {
                if (!_loggedNoTarget)
                {
                    int count = _enemyRegistry.Enemies != null ? _enemyRegistry.Enemies.Count : -1;
                    _loggedNoTarget = true;
                }
                return;
            }
            else if (_loggedNoTarget)
            {
                _loggedNoTarget = false;
            }

            // Optional range check
            if (_maxTargetRange > 0f)
            {
                float sqr = (origin - closest.transform.position).sqrMagnitude;
                float maxSqr = _maxTargetRange * _maxTargetRange;
                if (sqr > maxSqr)
                {
                    if (!_loggedOutOfRange)
                    {
                        float dist = Mathf.Sqrt(sqr);
                        _loggedOutOfRange = true;
                    }
                    return;
                }
                else if (_loggedOutOfRange)
                {
                    _loggedOutOfRange = false;
                }
            }

            // Fire
            FireAt(closest);
            // Reset cooldown by attack speed (attacks per second)
            float atkSpeed = Mathf.Max(0.01f, _config.BaseAttackSpeed);
            _cooldown = 1f / atkSpeed;
        }

        private void FireAt(Enemy target)
        {
            var spawnPos = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 0.5f;

            // Aim at enemy body using its config Y offset (defaults to 0 if not available)
            float yOffset = 0f;
            if (target != null && target.Config != null)
            {
                yOffset = Mathf.Max(0f, target.Config.AimPointYOffset);
            }
            Vector3 targetPoint = target.transform.position + new Vector3(0f, yOffset, 0f);

            var toTarget = (targetPoint - spawnPos);
            var spawnRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

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

            float speed = Mathf.Max(0f, _config.BaseProjectileSpeed);
            float dmg = Mathf.Max(0f, _config.BaseDamage);
            proj.Init(target, speed, dmg, _hitRadius);
        }
    }
}
