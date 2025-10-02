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

        public EnemyConfig Config => _config;
        public int CurrentHp => _currentHp;
        public int MaxHp => _config != null ? Mathf.Max(1, _config.MaxHp) : Mathf.Max(1, _currentHp);
        public bool IsDead => _currentHp <= 0;
        public int Armor => _armor;

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
            int dmg = Mathf.CeilToInt(baseDamage * (1f - reduction));
            int prev = _currentHp;
            _currentHp -= Mathf.Max(1, dmg); // at least 1 dmg
            int dealt = Mathf.Max(0, prev - _currentHp);
            OnDamaged?.Invoke(dealt, _currentHp);

            if (_currentHp <= 0)
                Die();
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
                _animator.SetTrigger("Dead");
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
