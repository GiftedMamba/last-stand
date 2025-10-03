using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;
using VContainer;
using Game.Presentation.Camera;

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
        private readonly HashSet<Enemy> _stunnedEnemies = new HashSet<Enemy>();

        // Animator parameter IDs (centralized here for now; can be moved to a shared holder later)
        private static readonly int StunnedHash = Animator.StringToHash("Stunned");

        // Optional camera shake injected from scene (GameplayScope registers CameraShake in hierarchy)
        private CameraShake _cameraShake;

        [Inject]
        public void Construct(CameraShake cameraShake)
        {
            _cameraShake = cameraShake;
        }

        public void StunForSeconds(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return;

            // Trigger camera shake once per stun activation; repeated calls will extend current shake in component
            _cameraShake?.StartShake();

            float now = Time.time;
            float newUntil = now + durationSeconds;

            // Extend if already active
            if (_stunUntil < newUntil)
                _stunUntil = newUntil;

            // Capture a snapshot of currently present enemies into the stunned set
            CaptureCurrentEnemiesIntoStunSet();
            // Immediately enforce stop on newly added enemies
            ForceStopStunnedEnemies();

            // Start coroutine if not running
            if (_stunRoutine == null)
            {
                _stunRoutine = StartCoroutine(StunCoroutine());
            }
        }

        private IEnumerator StunCoroutine()
        {
            // Ensure currently captured enemies are stopped at coroutine start
            ForceStopStunnedEnemies();

            while (Time.time < _stunUntil)
            {
                // Keep tracked enemies stopped (in case other systems toggled agents)
                ForceStopStunnedEnemies();
                yield return null;
            }

            // Resume only the enemies that were stunned
            ResumeStunnedEnemies();
            _stunnedEnemies.Clear();
            _stunRoutine = null;
            _stunUntil = 0f;
        }

        private void CaptureCurrentEnemiesIntoStunSet()
        {
            if (_enemyRegistry == null) return;
            var list = _enemyRegistry.Enemies;
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e == null || e.IsDead) continue;
                _stunnedEnemies.Add(e);
            }
        }

        private void ForceStopStunnedEnemies()
        {
            if (_stunnedEnemies.Count == 0) return;

            List<Enemy> toRemove = null;
            foreach (var e in _stunnedEnemies)
            {
                if (e == null || e.IsDead)
                {
                    toRemove ??= new List<Enemy>();
                    toRemove.Add(e);
                    continue;
                }

                if (e.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    if (agent.isOnNavMesh)
                        agent.isStopped = true;
                }

                if (e.TryGetComponent<Animator>(out var animator))
                {
                    animator.SetBool(StunnedHash, true);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _stunnedEnemies.Remove(toRemove[i]);
            }
        }

        private void ResumeStunnedEnemies()
        {
            if (_stunnedEnemies.Count == 0) return;

            List<Enemy> toRemove = null;
            foreach (var e in _stunnedEnemies)
            {
                if (e == null || e.IsDead)
                {
                    toRemove ??= new List<Enemy>();
                    toRemove.Add(e);
                    continue;
                }

                if (e.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    if (agent.isOnNavMesh)
                        agent.isStopped = false;
                }

                if (e.TryGetComponent<Animator>(out var animator))
                {
                    animator.SetBool(StunnedHash, false);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _stunnedEnemies.Remove(toRemove[i]);
            }
        }
    }
}