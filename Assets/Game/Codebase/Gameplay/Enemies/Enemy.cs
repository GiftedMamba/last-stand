using Game.Configs;
using Game.Core;
using Game.Gameplay.Spots;
using Game.Gameplay.Health;
using UnityEngine;
using UnityEngine.AI;

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

        private bool _isDying;

        // Animator parameter IDs
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

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
