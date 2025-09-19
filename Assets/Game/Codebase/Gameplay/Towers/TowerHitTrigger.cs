using Game.Configs;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Towers
{
    /// <summary>
    /// Attach to a Tower or its child with a Collider (set IsTrigger on the collider or use collision).
    /// When an Enemy enters the trigger/collides, the enemy will explode: it damages the tower and is destroyed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TowerHitTrigger : MonoBehaviour
    {
        private TowerHealth _towerHealth;

        private void Awake()
        {
            // Find TowerHealth on self or parent
            _towerHealth = GetComponent<TowerHealth>() ?? GetComponentInParent<TowerHealth>();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryExplodeEnemy(other.transform);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryExplodeEnemy(collision.transform);
        }

        private void TryExplodeEnemy(Transform t)
        {
            if (t == null) return;
            var enemy = t.GetComponentInParent<Enemy>();
            if (enemy == null) return;

            int dmg = 0;
            EnemyConfig cfg = enemy.Config;
            if (cfg != null)
            {
                dmg = Mathf.Max(0, cfg.ExplodeDamageToTower);
            }

            if (_towerHealth == null)
            {
                // Attempt to locate dynamically in case attached later
                _towerHealth = GetComponent<TowerHealth>() ?? GetComponentInParent<TowerHealth>();
            }

            if (_towerHealth != null && dmg > 0)
            {
                _towerHealth.TakeDamage(dmg);
            }

            // Enemy explodes/dies after contacting tower
            enemy.Die();
        }
    }
}
