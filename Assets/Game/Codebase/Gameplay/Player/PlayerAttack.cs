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
        [SerializeField] private Animator _animator;

        [Header("Overrides (optional)")]
        [SerializeField] private GameObject _projectilePrefabOverride;
        [SerializeField, Min(0f)] private float _maxTargetRange = 50f;
        [SerializeField, Min(0.01f)] private float _hitRadius = 0.25f;

        private float _cooldown;
        private Enemy _pendingTarget;
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private const string ShootName = "Shoot"; // used for state crossfade fallback

        // Animator setup cache
        private bool _animSetupChecked;
        private bool _hasShootTriggerParam;

        // Logging guards to avoid per-frame spam
        private bool _warnedUninitialized;
        private bool _loggedNoTarget;
        private bool _loggedOutOfRange;
        private bool _loggedNoShootTriggerParam;

        private void Awake()
        {
            // Auto-find animator if not wired to help scene setup
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void EnsureAnimatorSetupChecked()
        {
            if (_animSetupChecked || _animator == null) return;
            var parameters = _animator.parameters;
            _hasShootTriggerParam = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == ShootName)
                {
                    _hasShootTriggerParam = true;
                    break;
                }
            }
            _animSetupChecked = true;
        }

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

            // Cooldown handling - attack continuously by cooldown regardless of target death
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            // Acquire target
            var origin = _firePoint != null ? _firePoint.position : transform.position;
            var closest = _enemyRegistry.FindClosest(origin);
            if (closest == null || closest.IsDead)
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

            // Attack: trigger animation and defer actual firing to animation event
            float atkSpeed = Mathf.Max(0.01f, _config.BaseAttackSpeed);
            _cooldown = 1f / atkSpeed; // start cooldown immediately when we commit to an attack

            if (_animator != null)
            {
                _pendingTarget = closest;
                EnsureAnimatorSetupChecked();
                if (_hasShootTriggerParam)
                {
                    _animator.SetTrigger(ShootHash); // Trigger parameter present
                }
                else
                {
                    if (!_loggedNoShootTriggerParam)
                    {
                        GameLogger.LogWarning("PlayerAttack: Animator has no Trigger parameter named 'Shoot'. Falling back to CrossFade to state 'Shoot'.");
                        _loggedNoShootTriggerParam = true;
                    }
                    // CrossFade to a state named "Shoot" on base layer as a fallback
                    _animator.CrossFade(ShootName, 0.025f, 0, 0f);
                }
            }
            else
            {
                // Fallback if animator is not assigned: fire immediately
                FireAt(closest);
            }
        }

        // Animation Event handler. Add an animation event named "Shoot" in the attack animation to call this.
        public void Shoot()
        {
            // Try to fire at the pending target; if it's null or dead by the time the event fires, reacquire a new closest target.
            var origin = _firePoint != null ? _firePoint.position : transform.position;
            var target = _pendingTarget;
            if (target == null || target.IsDead)
            {
                target = _enemyRegistry != null ? _enemyRegistry.FindClosest(origin) : null;
            }

            if (target != null && !target.IsDead)
            {
                FireAt(target);
            }

            _pendingTarget = null; // clear either way to avoid repeated shots
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
            int pierceCount = Mathf.Max(0, _config.BasePierceCount);
            proj.Init(target, speed, dmg, _hitRadius, pierceCount);
        }
    }
}
