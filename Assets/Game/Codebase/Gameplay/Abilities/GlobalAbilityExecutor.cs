using System.Collections;
using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Scene-level executor for global abilities that need to affect runtime scene objects.
    /// Responsible for applying stun (stop) effect to all enemies via EnemyRegistry.
    /// </summary>
    [AddComponentMenu("Game/Gameplay/Abilities/Global Ability Executor")]
    public class GlobalAbilityExecutor : MonoBehaviour, IGlobalAbilityExecutor
    {
        [Header("Scene References")]
        [SerializeField] private EnemyRegistry _enemyRegistry;

        private float _stunUntil;
        private Coroutine _stunRoutine;

        public void StunForSeconds(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return;

            float now = Time.time;
            float newUntil = now + durationSeconds;

            // Extend if already active
            if (_stunUntil < newUntil)
                _stunUntil = newUntil;

            // If not running, start, and also enforce immediate stop once
            if (_stunRoutine == null)
            {
                _stunUntil = newUntil; // ensure set
                _stunRoutine = StartCoroutine(StunCoroutine());
            }
            else
            {
                // Already running; ensure current enemies are stopped immediately
                ForceStopAllEnemies();
            }
        }

        private IEnumerator StunCoroutine()
        {
            // Immediately stop everyone
            ForceStopAllEnemies();

            while (Time.time < _stunUntil)
            {
                // Enforce stop on newly spawned enemies too
                ForceStopAllEnemies();
                yield return null;
            }

            // Resume movement for all remaining enemies
            ResumeAllEnemies();
            _stunRoutine = null;
            _stunUntil = 0f;
        }

        private void ForceStopAllEnemies()
        {
            if (_enemyRegistry == null)
            {
                GameLogger.LogWarning("GlobalAbilityExecutor: EnemyRegistry is not assigned; cannot apply stun.");
                return;
            }

            var list = _enemyRegistry.Enemies;
            if (list == null) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e == null || e.IsDead) continue;
                if (e.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                }
            }
        }

        private void ResumeAllEnemies()
        {
            if (_enemyRegistry == null) return;
            var list = _enemyRegistry.Enemies;
            if (list == null) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e == null || e.IsDead) continue;
                if (e.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                }
            }
        }
    }
}