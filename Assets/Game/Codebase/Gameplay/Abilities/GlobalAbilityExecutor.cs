using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;
using VContainer;
using Game.Presentation.Camera;
using Game.Gameplay.Towers;

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
        private GameObject _stunVfxPrefab;

        // Howl state: enemies take extra damage while active
        private float _howlUntil;
        private float _howlPercent;
        private Coroutine _howlRoutine;
        private readonly HashSet<Enemy> _howlAffected = new HashSet<Enemy>();
        private GameObject _howlVfxPrefab;

        // Animator parameter IDs (centralized here for now; can be moved to a shared holder later)
        private static readonly int StunnedHash = Animator.StringToHash("Stunned");

        // Optional camera shake injected from scene (GameplayScope registers CameraShake in hierarchy)
        private CameraShake _cameraShake;

        // Shoied (shield) state: towers invulnerable while active
        private float _shoiedUntil;
        private Coroutine _shoiedRoutine;
        private GameObject _shoiedVfxPrefab;
        private readonly HashSet<TowerHealth> _shoiedVfxApplied = new HashSet<TowerHealth>();

        [Inject]
        public void Construct(CameraShake cameraShake)
        {
            _cameraShake = cameraShake;
        }

        public void StunForSeconds(float durationSeconds, float damageOnce, bool isPercent, GameObject vfxPrefab)
        {
            if (durationSeconds <= 0f)
                return;

            _stunVfxPrefab = vfxPrefab;

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

            // Spawn VFX on captured enemies once at activation
            if (_stunVfxPrefab != null)
            {
                foreach (var e in _stunnedEnemies)
                {
                    if (e == null || e.IsDead) continue;
                    SpawnVfxOn(e.transform, _stunVfxPrefab, durationSeconds);
                }
            }

            // Deal damage once on activation (to the currently captured enemies)
            if (damageOnce > 0f)
            {
                DealDamageOnceToCapturedStunned(damageOnce, isPercent);
            }

            // Start coroutine if not running
            if (_stunRoutine == null)
            {
                _stunRoutine = StartCoroutine(StunCoroutine());
            }
        }

        private void DealDamageOnceToCapturedStunned(float damageOnce, bool isPercent)
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

                float dmg = damageOnce;
                if (isPercent)
                {
                    // Interpret percentage as percent of Max HP
                    dmg = Mathf.Max(0f, e.MaxHp) * Mathf.Max(0f, damageOnce) * 0.01f;
                }
                if (dmg > 0f)
                    e.TakeDamage(dmg);
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _stunnedEnemies.Remove(toRemove[i]);
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

        public void ApplyHowl(float durationSeconds, float value, bool isPercent, GameObject vfxPrefab)
        {
            if (durationSeconds <= 0f)
                return;

            // Only percent mode is supported for Howl. If not percent, ignore the value.
            if (!isPercent || value <= 0f)
                return;

            _howlVfxPrefab = vfxPrefab;

            float now = Time.time;
            float newUntil = now + durationSeconds;
            _howlPercent = Mathf.Max(0f, value);
            if (_howlUntil < newUntil)
                _howlUntil = newUntil;

            CaptureCurrentEnemiesIntoHowlSetAndApply();

            if (_howlRoutine == null)
                _howlRoutine = StartCoroutine(HowlCoroutine());
        }

        private void CaptureCurrentEnemiesIntoHowlSetAndApply()
        {
            if (_enemyRegistry == null) return;
            var list = _enemyRegistry.Enemies;
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e == null || e.IsDead) continue;
                if (_howlAffected.Add(e))
                {
                    e.SetDamageTakenBonusPercent(_howlPercent);
                    // Spawn VFX once for newly affected enemy
                    if (_howlVfxPrefab != null)
                        SpawnVfxOn(e.transform, _howlVfxPrefab, Mathf.Max(0f, _howlUntil - Time.time));
                }
                else
                {
                    // Refresh value in case percent changed between activations
                    e.SetDamageTakenBonusPercent(_howlPercent);
                }
            }
        }
        
        public void ApplyShoied(float durationSeconds, GameObject vfxPrefab)
        {
            if (durationSeconds <= 0f)
                return;
            float newUntil = Time.time + durationSeconds;
            if (_shoiedUntil < newUntil)
                _shoiedUntil = newUntil;

            _shoiedVfxPrefab = vfxPrefab;

            // Reset tracking set for this activation
            _shoiedVfxApplied.Clear();
            
            // Apply immediately
            SetAllTowersInvulnerable(true);

            if (_shoiedRoutine == null)
                _shoiedRoutine = StartCoroutine(ShoiedCoroutine());
        }

        private void SetAllTowersInvulnerable(bool value)
        {
            // Non-alloc array of TowerHealth in scene (exclude inactive to avoid touching prefabs)
            var towers = FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < towers.Length; i++)
            {
                var t = towers[i];
                if (t == null) continue;
                if (!t.IsDead)
                {
                    t.SetInvulnerable(value);
                    if (value && _shoiedVfxPrefab != null)
                    {
                        // ensure we only spawn once per tower per activation
                        if (_shoiedVfxApplied.Add(t))
                        {
                            SpawnVfxOn(t.transform, _shoiedVfxPrefab, Mathf.Max(0f, _shoiedUntil - Time.time));
                        }
                    }
                }
            }
        }

        private IEnumerator ShoiedCoroutine()
        {
            // Ensure all current towers are invulnerable at start
            SetAllTowersInvulnerable(true);
            while (Time.time < _shoiedUntil)
            {
                // In case towers were spawned or toggled, re-assert
                SetAllTowersInvulnerable(true);
                yield return null;
            }
            // Clear
            SetAllTowersInvulnerable(false);
            _shoiedVfxApplied.Clear();
            _shoiedRoutine = null;
            _shoiedUntil = 0f;
        }

        private void SpawnVfxOn(Transform target, GameObject prefab, float autoDestroySeconds)
        {
            if (prefab == null || target == null) return;
            var vfx = Instantiate(prefab, target.position, target.rotation, target);
            if (autoDestroySeconds > 0f)
                Destroy(vfx, autoDestroySeconds);
        }
        
        private IEnumerator HowlCoroutine()
        {
            // Ensure initial application
            CaptureCurrentEnemiesIntoHowlSetAndApply();

            while (Time.time < _howlUntil)
            {
                // Apply to any newly spawned enemies
                CaptureCurrentEnemiesIntoHowlSetAndApply();
                yield return null;
            }

            // Clear effect from all affected enemies
            if (_howlAffected.Count > 0)
            {
                List<Enemy> toRemove = null;
                foreach (var e in _howlAffected)
                {
                    if (e == null || e.IsDead)
                    {
                        toRemove ??= new List<Enemy>();
                        toRemove.Add(e);
                        continue;
                    }
                    e.ResetDamageTakenBonus();
                }
                if (toRemove != null)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                        _howlAffected.Remove(toRemove[i]);
                }
                _howlAffected.Clear();
            }

            _howlRoutine = null;
            _howlUntil = 0f;
        }
    }
}