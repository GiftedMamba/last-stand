using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Simple homing projectile that travels towards a target and applies damage on contact.
    /// Uses non-physics distance check for simplicity.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 10f;
        [SerializeField, Min(0f)] private float _damage = 5f;
        [SerializeField, Min(0f)] private float _hitRadius = 0.2f;
        [SerializeField, Min(0f)] private float _maxLifetime = 5f;

        private Enemy _target;
        private float _life;

        public void Init(Enemy target, float speed, float damage, float hitRadius = 0.2f)
        {
            _target = target;
            _speed = Mathf.Max(0f, speed);
            _damage = Mathf.Max(0f, damage);
            _hitRadius = Mathf.Max(0.01f, hitRadius);
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life >= _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (_target == null)
            {
                // Move forward and expire if no target
                transform.position += transform.forward * (_speed * Time.deltaTime);
                return;
            }

            Vector3 toTarget = _target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist <= _hitRadius)
            {
                // hit
                _target.TakeDamage(_damage);
                Destroy(gameObject);
                return;
            }

            if (dist > 0.001f)
            {
                Vector3 dir = toTarget / dist;
                transform.position += dir * (_speed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
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
