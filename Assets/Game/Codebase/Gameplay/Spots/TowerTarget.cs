using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Spots
{
    /// <summary>
    /// Place this on each defense spot/tower target point. Enemies will pick a random one
    /// and set it as their NavMesh destination.
    /// </summary>
    public class TowerTarget : MonoBehaviour
    {
        private static readonly List<TowerTarget> _all = new();
        public static IReadOnlyList<TowerTarget> All => _all;

        public Vector3 Position => transform.position;

        private void OnEnable()
        {
            if (!_all.Contains(this))
                _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        public static TowerTarget GetRandom()
        {
            if (_all.Count == 0) return null;
            int i = Random.Range(0, _all.Count);
            return _all[i];
        }
    }
}
