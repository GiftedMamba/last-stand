using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Enemies;
using Game.Gameplay.Abilities;
using UnityEngine;
using VContainer;

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
        private bool _warnedSplitShotCap;

        // Animator speed override handling for attack animation length adjustments
        private bool _animSpeedOverridden;
        private float _storedAnimatorSpeed = 1f;

        // Runtime ability levels tracked here (not in config). Missing entries default to 0.
        private readonly Dictionary<HeroAbilityType, int> _abilityLevels = new();

        // Injected hero ability service (optional). When available, we read current levels from here.
        private IHeroAbilityService _heroAbilityService;

        [Inject]
        public void Construct(IHeroAbilityService heroAbilityService)
        {
            _heroAbilityService = heroAbilityService;
        }

        public void SetAbilityLevel(HeroAbilityType type, int level)
        {
            if (level < 0) level = 0;
            _abilityLevels[type] = level;
        }

        public int GetAbilityLevel(HeroAbilityType type)
        {
            // Prefer live levels from the injected service when available
            if (_heroAbilityService != null)
            {
                int lvl = _heroAbilityService.GetLevel(type);
                return lvl < 0 ? 0 : lvl;
            }

            // Fallback to local runtime dictionary
            if (_abilityLevels != null && _abilityLevels.TryGetValue(type, out var localLvl))
                return localLvl < 0 ? 0 : localLvl;
            return 0;
        }

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

            // Inform hero ability service about config so it can determine max levels
            if (_heroAbilityService != null && _config != null)
            {
                _heroAbilityService.RegisterConfig(_config);
            }

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
            float atkMul = GetAttackSpeedMultiplierFromAbilities();
            if (atkMul <= 0f) atkMul = 1f;
            float atkSpeed = Mathf.Max(0.01f, _config.BaseAttackSpeed * atkMul);
            _cooldown = 1f / atkSpeed; // start cooldown immediately when we commit to an attack

            if (_animator != null)
            {
                _pendingTarget = closest;

                // Adjust animator speed so animation length scales inversely with multiplier
                float desiredAnimSpeed = 1f / Mathf.Max(0.0001f, atkMul);
                if (!_animSpeedOverridden)
                {
                    _storedAnimatorSpeed = _animator.speed;
                    _animSpeedOverridden = true;
                }
                _animator.speed = desiredAnimSpeed;

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

            // Restore animator speed after attack event processed
            if (_animator != null && _animSpeedOverridden)
            {
                _animator.speed = _storedAnimatorSpeed;
                _animSpeedOverridden = false;
            }
        }

        private int GetSplitShotCountFromAbilities()
        {
            // Default to 1 shot if ability/levels not configured
            if (_config == null)
                return 1;

            var abilities = _config.Abilities;
            if (abilities == null)
                return 1;

            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null || ab.Type != HeroAbilityType.SplitShot)
                    continue;

                var levels = ab.Levels;
                if (levels == null || levels.Count == 0)
                    return 1;

                int idx = GetAbilityLevel(HeroAbilityType.SplitShot);
                if (idx < 0) idx = 0;
                if (idx >= levels.Count) idx = levels.Count - 1;

                float raw = levels[idx] != null ? levels[idx].Value : 1f;
                int count = Mathf.FloorToInt(raw);
                if (count <= 0)
                    count = 1; // ensure at least one shot
                if (count > 5)
                {
                    if (!_warnedSplitShotCap)
                    {
                        GameLogger.LogWarning($"PlayerAttack: SplitShot value {count} exceeds maximum of 5. Capping to 5.");
                        _warnedSplitShotCap = true;
                    }
                    count = 5;
                }
                return count;
            }

            return 1;
        }

        private int GetPierceCountFromAbilities()
        {
            if (_config == null)
                return 0;

            var abilities = _config.Abilities;
            if (abilities == null)
                return 0;

            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null || ab.Type != HeroAbilityType.Pierce)
                    continue;

                var levels = ab.Levels;
                if (levels == null || levels.Count == 0)
                    return 0;

                int idx = GetAbilityLevel(HeroAbilityType.Pierce);
                if (idx < 0) idx = 0;
                if (idx >= levels.Count) idx = levels.Count - 1;

                float value = levels[idx] != null ? levels[idx].Value : 0f;
                int pierce = Mathf.Max(0, Mathf.FloorToInt(value));
                return pierce;
            }

            return 0;
        }

        private float GetCritChancePercentFromAbilities()
        {
            if (_config == null)
                return 0f;

            var abilities = _config.Abilities;
            if (abilities == null)
                return 0f;

            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null || ab.Type != HeroAbilityType.Crit)
                    continue;

                var levels = ab.Levels;
                if (levels == null || levels.Count == 0)
                    return 0f;

                int idx = GetAbilityLevel(HeroAbilityType.Crit);
                if (idx < 0) idx = 0;
                if (idx >= levels.Count) idx = levels.Count - 1;

                float percent = levels[idx] != null ? levels[idx].Value : 0f; // value interpreted as percent (e.g., 10 = 10%)
                if (percent < 0f) percent = 0f;
                if (percent > 100f) percent = 100f;
                return percent;
            }

            return 0f;
        }

        private float GetAttackSpeedMultiplierFromAbilities()
        {
            // Default multiplier is 1 (no change) if config or ability not present
            if (_config == null)
                return 1f;

            var abilities = _config.Abilities;
            if (abilities == null)
                return 1f;

            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null || ab.Type != HeroAbilityType.AttackSpeed)
                    continue;

                var levels = ab.Levels;
                if (levels == null || levels.Count == 0)
                    return 1f;

                int idx = GetAbilityLevel(HeroAbilityType.AttackSpeed);
                if (idx < 0) idx = 0;
                if (idx >= levels.Count) idx = levels.Count - 1;

                float mul = levels[idx] != null ? levels[idx].Value : 1f; // interpret value as multiplier (e.g., 1.5 = +50% speed)
                if (mul <= 0f) mul = 1f;
                return mul;
            }

            return 1f;
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
            var baseRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            var projectilePrefab = _projectilePrefabOverride != null ? _projectilePrefabOverride : _config.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                GameLogger.LogError("PlayerAttack: No projectile prefab assigned (override or in PlayerConfig).");
                return;
            }

            // Determine spread based on SplitShot ability (1..5). Angles order matches level mapping.
            int count = GetSplitShotCountFromAbilities();
            int[] angleOrder = { 0, -15, 15, -35, 35 };

            float speed = Mathf.Max(0f, _config.BaseProjectileSpeed);
            float baseDamage = Mathf.Max(0f, _config.BaseDamage);
            int pierceCount = GetPierceCountFromAbilities();

            // Crit parameters
            float critChancePercent = GetCritChancePercentFromAbilities();
            float critChance01 = Mathf.Clamp01(critChancePercent / 100f);
            float critMul = _config != null ? Mathf.Max(1f, _config.CritMultiplier) : 2f;

            for (int i = 0; i < count && i < angleOrder.Length; i++)
            {
                int angle = angleOrder[i];
                var rot = Quaternion.AngleAxis(angle, Vector3.up) * baseRot;
                var go = Instantiate(projectilePrefab, spawnPos, rot);
                var proj = go.GetComponent<Projectile>();
                if (proj == null)
                {
                    GameLogger.LogError("PlayerAttack: Projectile prefab missing Projectile component.");
                    Destroy(go);
                    continue;
                }

                // Apply Split Shot damage reduction for additional arrows (index > 0)
                float dmgMultiplier = 1f;
                if (_config != null)
                {
                    float per = Mathf.Clamp01(_config.PercentToDecreaseForSplitShot / 100f);
                    // Each additional arrow beyond the first loses 'per' damage; i is 0-based loop index
                    dmgMultiplier = Mathf.Clamp01(1f - (i * per));
                }

                // Base damage after split-shot penalty
                float dmg = baseDamage * dmgMultiplier;

                // Decide crit per projectile (apply crit after split-shot scaling)
                if (critChance01 > 0f && Random.value < critChance01)
                {
                    dmg *= critMul;
                    GameLogger.Log($"Crit! Damage x{critMul:0.##} => {dmg:0.##}");
                }

                // For the central shot (0 deg), use the target snapshot; for side shots, fire forward without target snapshot
                Enemy initTarget = angle == 0 ? target : null;
                proj.Init(initTarget, speed, dmg, _hitRadius, pierceCount);
                if (_config != null)
                {
                    proj.SetMaxLifetime(Mathf.Max(0f, _config.BaseProjectileLifetime));
                }
            }
        }
    }
}
