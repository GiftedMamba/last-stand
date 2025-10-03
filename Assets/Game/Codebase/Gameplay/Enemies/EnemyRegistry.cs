using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Scene-level registry of active enemies. Non-static to comply with guidelines.
    /// Place a single instance in the scene and reference it from spawners/attackers.
    /// </summary>
    public class EnemyRegistry : MonoBehaviour
    {
        private readonly List<Enemy> _enemies = new List<Enemy>(64);

        public IReadOnlyList<Enemy> Enemies => _enemies;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Enemy enemy)
        {
            if (enemy == null) return;
            if (!_enemies.Contains(enemy))
                _enemies.Add(enemy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(Enemy enemy)
        {
            if (enemy == null) return;
            _enemies.Remove(enemy);
        }

        /// <summary>
        /// Finds the closest enemy to the given position. Returns null if none.
        /// Skips enemies that are null or dead to avoid targeting corpses during death animations.
        /// Also prunes null or dead entries from the registry as we encounter them.
        /// </summary>
        public Enemy FindClosest(Vector3 position)
        {
            Enemy closest = null;
            float bestSqr = float.PositiveInfinity;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || e.IsDead)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                float sqr = (e.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    closest = e;
                }
            }
            return closest;
        }
    }
}
