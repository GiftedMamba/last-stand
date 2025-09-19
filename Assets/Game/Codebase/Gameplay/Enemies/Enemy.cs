using Game.Configs;
using Game.Core;
using Game.Gameplay.Spots;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Gameplay.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;

        private NavMeshAgent _agent;
        private int _currentHp;
        private int _armor;

        public EnemyConfig Config => _config;
        public int CurrentHp => _currentHp;
        public int Armor => _armor;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
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
        }

        private void AcquireAndMoveToTarget()
        {
            var target = TowerTarget.GetRandom();
            if (target == null)
            {
                GameLogger.LogWarning("Enemy: No TowerTarget found in scene. Enemy will idle.");
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(target.Position);
            }
        }

        public void TakeDamage(float baseDamage, int armorPierce = 0)
        {
            // Armor reduction: 6% per armor, capped at 60%
            int effectiveArmor = Mathf.Max(0, _armor - Mathf.Max(0, armorPierce));
            float reduction = Mathf.Min(0.06f * effectiveArmor, 0.60f);
            int dmg = Mathf.CeilToInt(baseDamage * (1f - reduction));
            _currentHp -= Mathf.Max(1, dmg); // at least 1 dmg

            if (_currentHp <= 0)
                Die();
        }

        private void Die()
        {
            // TODO: notify systems via event/bus later
            Destroy(gameObject);
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
