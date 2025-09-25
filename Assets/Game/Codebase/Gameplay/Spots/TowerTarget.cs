using System.Collections.Generic;
using Game.Gameplay.Towers;
using UnityEngine;

namespace Game.Gameplay.Spots
{
    /// <summary>
    /// Place this on each defense spot/tower target point. Enemies will pick a random one
    /// and set it as their NavMesh destination.
    /// If linked TowerHealth dies, this target disables itself to be excluded from selection.
    /// </summary>
    public class TowerTarget : MonoBehaviour
    {
        private static readonly List<TowerTarget> _all = new();
        public static IReadOnlyList<TowerTarget> All => _all;

        [SerializeField] private TowerHealth _towerHealth; // optional link; auto-found in parents if null

        public Vector3 Position => transform.position;
        public bool IsValid => isActiveAndEnabled && (_towerHealth == null || !_towerHealth.IsDead);

        private void Awake()
        {
            if (_towerHealth == null)
            {
                _towerHealth = GetComponent<TowerHealth>() ?? GetComponentInParent<TowerHealth>();
            }
        }

        private void OnEnable()
        {
            if (!_all.Contains(this))
                _all.Add(this);

            if (_towerHealth != null)
            {
                // subscribe to death to disable target
                _towerHealth.OnDied += HandleTowerDied;
                // If already dead (e.g., prefab mis-order), disable immediately
                if (_towerHealth.IsDead)
                {
                    enabled = false; // triggers OnDisable to remove from list
                }
            }
        }

        private void OnDisable()
        {
            _all.Remove(this);
            if (_towerHealth != null)
            {
                _towerHealth.OnDied -= HandleTowerDied;
            }
        }

        private void HandleTowerDied()
        {
            // Disable this target so enemies won't select it anymore
            if (this != null && isActiveAndEnabled)
                enabled = false;
        }

        public static TowerTarget GetRandom()
        {
            // Filter invalid targets on the fly
            for (int i = _all.Count - 1; i >= 0; i--)
            {
                var t = _all[i];
                if (t == null || !t.IsValid)
                    _all.RemoveAt(i);
            }

            if (_all.Count == 0) return null;
            int idx = Random.Range(0, _all.Count);
            return _all[idx];
        }
    }
}
