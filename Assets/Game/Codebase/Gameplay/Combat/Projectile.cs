using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Projectile that flies towards the snapshot position of the target at fire time (non-homing).
    /// Uses non-physics distance check for simplicity.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 10f;
        [SerializeField, Min(0f)] private float _damage = 5f;
        [SerializeField, Min(0f)] private float _hitRadius = 0.2f;
        [SerializeField, Min(0f)] private float _maxLifetime = 5f;

        private Enemy _target; // only used to apply damage if still within radius when reaching snapshot point
        private Vector3 _targetPosition;
        private bool _hasSnapshot;
        private float _life;
        private int _pierceCount; // how many enemies this projectile can pierce through
        private HashSet<Enemy> _hitEnemies; // tracks which enemies have been hit to avoid multiple hits

        public void Init(Enemy target, float speed, float damage, float hitRadius = 0.2f, int pierceCount = 0)
        {
            _target = target;
            _speed = Mathf.Max(0f, speed);
            _damage = Mathf.Max(0f, damage);
            _hitRadius = Mathf.Max(0.01f, hitRadius);
            _pierceCount = Mathf.Max(0, pierceCount);
            _hitEnemies = new HashSet<Enemy>();

            if (_target != null)
            {
                float yOffset = 0f;
                if (_target.Config != null)
                {
                    yOffset = Mathf.Max(0f, _target.Config.AimPointYOffset);
                }
                _targetPosition = _target.transform.position + new Vector3(0f, yOffset, 0f); // snapshot at fire time with body offset
                _hasSnapshot = true;
            }
            else
            {
                _targetPosition = transform.position + transform.forward * 1000f; // arbitrary far point
                _hasSnapshot = false;
            }
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life >= _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!_hasSnapshot)
            {
                // No target snapshot: move forward and expire naturally
                transform.position += transform.forward * (_speed * Time.deltaTime);
                // After moving, try to hit any enemy within radius
                if (TryHitAny())
                {
                    Destroy(gameObject);
                    return;
                }
                // keep current rotation
                return;
            }

            Vector3 toTarget = _targetPosition - transform.position;
            float dist = toTarget.magnitude;
            if (dist <= _hitRadius)
            {
                // Reached destination. First, try to hit any enemy in range
                if (TryHitAny())
                {
                    Destroy(gameObject);
                    return;
                }
                // Fallback to original behavior: apply to intended target if still within radius
                if (_target != null)
                {
                    float currentDistToEnemy = Vector3.Distance(_target.transform.position, transform.position);
                    if (currentDistToEnemy <= _hitRadius)
                    {
                        _target.TakeDamage(_damage);
                    }
                }
                Destroy(gameObject);
                return;
            }

            if (dist > 0.001f)
            {
                Vector3 dir = toTarget / dist;
                transform.position += dir * (_speed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                // After moving, try to hit any enemy within radius
                if (TryHitAny())
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private bool TryHitAny()
        {
            // Check for any enemy colliders within hit radius
            var hits = Physics.OverlapSphere(transform.position, _hitRadius, ~0, QueryTriggerInteraction.Collide);
            bool hitAnyNewEnemy = false;
            
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                var enemy = col.GetComponentInParent<Enemy>();
                if (enemy == null) continue;
                if (enemy.IsDead) continue;
                
                // Skip if we've already hit this enemy
                if (_hitEnemies.Contains(enemy)) continue;
                
                // Hit the enemy and track it
                enemy.TakeDamage(_damage);
                _hitEnemies.Add(enemy);
                hitAnyNewEnemy = true;
                
                // If we've hit the maximum number of enemies (pierce count + 1), destroy projectile
                if (_hitEnemies.Count >= _pierceCount + 1)
                {
                    return true;
                }
            }
            
            // Only destroy if we hit new enemies and exceeded pierce count
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _hitRadius);
        }
#endif
    }
}
