using Game.Configs;
using Game.Core;
using Game.Gameplay.Spots;
using Game.Gameplay.Health;
using UnityEngine;
using UnityEngine.AI;
using Ami.BroAudio;
using Game.Audio;
using VContainer;

namespace Game.Gameplay.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : MonoBehaviour, IHealth
    {
        [SerializeField] private EnemyConfig _config;

        private NavMeshAgent _agent;
        [SerializeField] private int _currentHp;
        private int _armor;
        private TowerTarget _currentTarget;
        private float _retargetTimer;

        [Header("Presentation")]
        [SerializeField] private Animator _animator;
        [Tooltip("SkinnedMeshRenderer whose material will be manipulated for hit flash.")]
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [Tooltip("How long the hit flash lasts, seconds.")]
        [SerializeField] [Min(0f)] private float _flashDuration = 0.15f;
        [Tooltip("Peak intensity of the flash written to _FlashAmount shader property.")]
        [SerializeField] [Min(0f)] private float _flashPower = 1f;

        [Header("Audio")]
        [Tooltip("Sound played when this enemy is hit by a player projectile.")]
        [SerializeField] private SoundID _hitSfx;

        private IAudioService _audio;

        private bool _isDying;

        // Animator parameter IDs
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        // Shader property IDs
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        // Flash state
        private float _flashTimer;

        // Non-serialized runtime material instance extracted from SkinnedMeshRenderer
        private Material _material;

        public EnemyConfig Config => _config;
        public int CurrentHp => _currentHp;
        public int MaxHp => _config != null ? Mathf.Max(1, _config.MaxHp) : Mathf.Max(1, _currentHp);
        public bool IsDead => _currentHp <= 0;
        public int Armor => _armor;
        public EnemyType Type => _config != null ? _config.Type : EnemyType.Unknown;

        // External damage taken multiplier (e.g., Howl); 1.0 means no change
        private float _externalDamageTakenMultiplier = 1f;

        public event System.Action<int, int> OnDamaged;
        public event System.Action OnDied;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null)
                TryGetComponent(out _animator);

            // Ensure we have a unique material instance from the SkinnedMeshRenderer
            if (_skinnedMeshRenderer == null)
            {
                TryGetComponent(out _skinnedMeshRenderer);
                if (_skinnedMeshRenderer == null)
                    _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
            if (_skinnedMeshRenderer != null)
            {
                // material returns an instance (not sharedMaterial)
                _material = _skinnedMeshRenderer.material;
            }

            // Initialize flash to zero
            if (_material != null)
            {
                _material.SetFloat(FlashAmountId, 0f);
            }
        }

        [Inject]
        public void Inject(IAudioService audio)
        {
            _audio = audio;
        }

        private void Start()
        {
            if (_config == null)
            {
                GameLogger.LogError($"Enemy '{name}': Config is not assigned.");
                enabled = false;
                return;
            }

            InitializeFromConfig(_config);
            AcquireAndMoveToTarget();

            // Initialize locomotion to idle
            if (_animator != null)
                _animator.SetFloat(SpeedHash, 0f);
        }

        private void Update()
        {
            // Periodically validate target (2 times per second) to avoid thrashing
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                _retargetTimer = 0.5f;
                if (_currentTarget == null || !_currentTarget.IsValid)
                {
                    AcquireAndMoveToTarget();
                }
                else if (_agent != null && _agent.isOnNavMesh)
                {
                    // Ensure destination is still set correctly (target may have moved)
                    _agent.SetDestination(_currentTarget.Position);
                }
            }

            // Drive locomotion blend tree via Speed parameter (0..1)
            if (_animator != null)
            {
                float vel = 0f;
                float max = 1f;
                if (_agent != null)
                {
                    vel = _agent.velocity.magnitude;
                    max = Mathf.Max(0.01f, _agent.speed);
                    if (_agent.isStopped || !_agent.isOnNavMesh)
                        vel = 0f;
                }

                float speed01 = Mathf.Clamp01(vel / max);
                // small damping for smoother transitions
                _animator.SetFloat(SpeedHash, speed01, 0.1f, Time.deltaTime);
            }

            // Flash effect: lerp _FlashAmount back to 0 over _flashDuration
            if (_flashTimer > 0f && _flashDuration > 0f && _material != null)
            {
                _flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(1f - (_flashTimer / _flashDuration));
                float value = Mathf.Lerp(_flashPower, 0f, t);
                _material.SetFloat(FlashAmountId, value);
                if (_flashTimer <= 0f)
                {
                    _material.SetFloat(FlashAmountId, 0f);
                }
            }
        }

        public void InitializeFromConfig(EnemyConfig cfg)
        {
            _config = cfg;
            _currentHp = Mathf.Max(1, cfg.MaxHp);
            _armor = Mathf.Max(0, cfg.Armor);

            if (_agent != null)
            {
                _agent.speed = Mathf.Max(0.1f, cfg.MoveSpeed);
                _agent.acceleration = Mathf.Max(4f, cfg.MoveSpeed * 4f);
                _agent.angularSpeed = 720f;
                _agent.stoppingDistance = 0.25f;
                _agent.autoBraking = true;
            }

            // Notify listeners so UI can sync initial health right after spawn
            OnDamaged?.Invoke(0, _currentHp);
        }

        private void AcquireAndMoveToTarget()
        {
            _currentTarget = TowerTarget.GetRandom();
            if (_currentTarget == null)
            {
                GameLogger.LogWarning("Enemy: No valid TowerTarget found. Enemy will idle.");
                // Optionally stop agent
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                }
                // Ensure idle animation
                if (_animator != null)
                    _animator.SetFloat(SpeedHash, 0f);
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(_currentTarget.Position);
            }
        }

        public void TakeDamage(float baseDamage, int armorPierce = 0)
        {
            if (IsDead) return;
            // Armor reduction: 6% per armor, capped at 60%
            int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPierce));
            float reduction = Mathf.Min(0.06f * effectiveArmor, 0.60f);

            // Apply external damage taken multiplier (e.g., Howl)
            float modifiedBase = Mathf.Max(0f, baseDamage) * Mathf.Max(0.01f, _externalDamageTakenMultiplier);

            int dmg = Mathf.CeilToInt(modifiedBase * (1f - reduction));
            int prev = _currentHp;
            _currentHp -= Mathf.Max(1, dmg); // at least 1 dmg
            int dealt = Mathf.Max(0, prev - _currentHp);
            OnDamaged?.Invoke(dealt, _currentHp);

            // Play impact sound if any damage was dealt
            if (dealt > 0 && _audio != null)
            {
                // Follow the enemy so the audio remains spatially correct if it moves mid-playback
                _audio.PlaySfx(_hitSfx, transform);
            }

            // Trigger flash on hit
            if (_material != null && _flashPower > 0f && _flashDuration > 0f)
            {
                _flashTimer = _flashDuration;
                _material.SetFloat(FlashAmountId, _flashPower);
            }

            if (_currentHp <= 0)
                Die();
        }

        public void SetDamageTakenBonusPercent(float percent)
        {
            // percent is additional damage taken; 50 => 1.5x
            float mult = 1f + Mathf.Max(0f, percent) * 0.01f;
            _externalDamageTakenMultiplier = mult;
        }

        public void ResetDamageTakenBonus()
        {
            _externalDamageTakenMultiplier = 1f;
        }

        public void Die()
        {
            if (_isDying)
                return;

            // Mark as dead/dying and clamp HP to zero
            _isDying = true;
            _currentHp = 0;

            // Notify listeners once
            OnDied?.Invoke();

            // Stop navigation/movement
            if (_agent != null)
            {
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                }
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }

            // Prevent further hits/physics interactions during the end animation
            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            // Play end animation if Animator is present
            if (_animator != null)
            {
                // ensure locomotion settles to idle
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetTrigger(DeadHash);
            }

            // Reset flash on death
            _flashTimer = 0f;
            if (_material != null)
            {
                _material.SetFloat(FlashAmountId, 0f);
            }

            // Delay destruction to allow animation to finish
            Destroy(gameObject, 3f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_agent != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _agent.stoppingDistance);
            }
        }
#endif
    }
}
