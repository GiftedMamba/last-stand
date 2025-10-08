using System.Collections.Generic;
using Game.Configs;
using Game.Gameplay.Enemies;
using Game.Gameplay.Spots;
using Game.Gameplay.Abilities;
using UnityEngine;
using VContainer;

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
        private IGlobalAbilityExecutor _abilityExecutor;

        [Inject]
        public void Construct(IGlobalAbilityExecutor abilityExecutor)
        {
            _abilityExecutor = abilityExecutor;
        }

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

            // Boss special rule: on death/explosion, damage all towers; half damage if shield active
            if (cfg != null && cfg.IsBoss && dmg > 0)
            {
                int actualDamage = dmg;
                if (_abilityExecutor != null && _abilityExecutor.IsShoiedActive)
                {
                    actualDamage = Mathf.CeilToInt(dmg * 0.5f);
                }

                var allTargets = TowerTarget.All;
                if (allTargets != null && allTargets.Count > 0)
                {
                    // Snapshot tower healths to avoid mutating the underlying collection while applying damage
                    var targetsToDamage = new List<TowerHealth>(allTargets.Count);
                    for (int i = 0; i < allTargets.Count; i++)
                    {
                        var target = allTargets[i];
                        if (target == null) continue;
                        var th = target.GetComponent<TowerHealth>() ?? target.GetComponentInParent<TowerHealth>();
                        if (th != null)
                        {
                            targetsToDamage.Add(th);
                        }
                    }

                    // Apply damage after snapshot so removals from TowerTarget list don't affect iteration
                    for (int i = 0; i < targetsToDamage.Count; i++)
                    {
                        var th = targetsToDamage[i];
                        if (th != null)
                        {
                            th.TakeDamagePiercingShield(actualDamage);
                        }
                    }
                }
            }
            else
            {
                // Non-boss default behavior: damage only this tower
                if (_towerHealth == null)
                {
                    // Attempt to locate dynamically in case attached later
                    _towerHealth = GetComponent<TowerHealth>() ?? GetComponentInParent<TowerHealth>();
                }

                if (_towerHealth != null && dmg > 0)
                {
                    _towerHealth.TakeDamage(dmg);
                }
            }

            // Spawn enemy-specific explosion prefab if assigned
            if (cfg != null && cfg.ExplosionPrefab != null)
            {
                Instantiate(cfg.ExplosionPrefab, enemy.transform.position, Quaternion.identity);
            }

            // Enemy explodes/dies after contacting tower
            enemy.Die();
        }
    }
}
