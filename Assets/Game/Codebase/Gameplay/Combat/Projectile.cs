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

        /// <summary>
        /// Allows external systems (e.g., PlayerAttack) to override the maximum lifetime in seconds.
        /// </summary>
        public void SetMaxLifetime(float seconds)
        {
            _maxLifetime = Mathf.Max(0f, seconds);
        }

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

            // Move towards snapshot without overshooting. When reaching it, switch to forward flight without turning back.
            if (dist > 0.001f)
            {
                float step = _speed * Time.deltaTime;

                if (step >= dist)
                {
                    // Snap to target position this frame and process hit without flipping direction back
                    Vector3 dir = dist > 0.0001f ? toTarget / dist : transform.forward;
                    transform.position = _targetPosition;
                    // Do not rotate towards the snapshot again; keep current forward to continue past the target
                    if (TryHitAny())
                    {
                        Destroy(gameObject);
                        return;
                    }
                    _hasSnapshot = false;
                    return;
                }
                else
                {
                    Vector3 dir = toTarget / dist;
                    transform.position += dir * step;
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    // After moving, try to hit any enemy within radius
                    if (TryHitAny())
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            else
            {
                // Already at snapshot
                if (TryHitAny())
                {
                    Destroy(gameObject);
                    return;
                }
                _hasSnapshot = false;
                return;
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
                
                // If we've hit the maximum number of enemies allowed by pierce count, destroy projectile
                if (_hitEnemies.Count >= _pierceCount)
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
