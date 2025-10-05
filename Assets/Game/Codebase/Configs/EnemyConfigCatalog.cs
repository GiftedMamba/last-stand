using System.Collections.Generic;
using UnityEngine;
using Game.Gameplay.Enemies;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "EnemyConfigCatalog", menuName = "Game/Configs/Enemy Config Catalog", order = 3)]
    public class EnemyConfigCatalog : ScriptableObject, IEnemyConfigProvider
    {
        [SerializeField]
        private List<EnemyConfig> _configs = new();

        // Map of available configs per type (only configs with a valid prefab)
        private readonly Dictionary<EnemyType, List<EnemyConfig>> _byType = new();

        public IReadOnlyList<EnemyConfig> Configs => _configs;

        private void OnEnable()
        {
            // In case domain reload is disabled, ensure we rebuild fresh each play session
            _byType.Clear();
        }

        private void BuildIfNeeded()
        {
            // Rebuild when the map is empty. This is robust against domain-reload-disabled play mode.
            if (_byType.Count > 0)
                return;

            _byType.Clear();
            if (_configs == null || _configs.Count == 0)
                return;

            for (int i = 0; i < _configs.Count; i++)
            {
                var cfg = _configs[i];
                if (cfg == null) continue;
                if (cfg.EnemyPrefab == null) continue; // only spawnable configs

                var t = cfg.Type;
                if (!_byType.TryGetValue(t, out var list))
                {
                    list = new List<EnemyConfig>();
                    _byType[t] = list;
                }
                list.Add(cfg);
            }
        }

        public EnemyConfig GetRandomAny()
        {
            BuildIfNeeded();
            if (_configs == null || _configs.Count == 0) return null;

            // Prefer picking from built map so we only return spawnable configs
            if (_byType.Count > 0)
            {
                // Flatten one random bucket
                var keys = ListPool<EnemyType>.Get();
                try
                {
                    foreach (var kv in _byType)
                        keys.Add(kv.Key);
                    if (keys.Count == 0) return null;
                    var randomKey = keys[Random.Range(0, keys.Count)];
                    var list = _byType[randomKey];
                    if (list == null || list.Count == 0) return null;
                    return list[Random.Range(0, list.Count)];
                }
                finally
                {
                    ListPool<EnemyType>.Release(keys);
                }
            }

            // Fallback to raw list (may include configs without prefabs)
            int idx = Random.Range(0, _configs.Count);
            return _configs[idx];
        }

        public EnemyConfig GetRandomForType(EnemyType type)
        {
            BuildIfNeeded();
            if (_byType.TryGetValue(type, out var list) && list != null && list.Count > 0)
            {
                int idx = Random.Range(0, list.Count);
                return list[idx];
            }
            return null;
        }

        public EnemyConfig GetRandomForTypes(IReadOnlyList<EnemyType> types)
        {
            BuildIfNeeded();
            if (types == null || types.Count == 0)
                return GetRandomAny();

            // Try a few random attempts for diversity
            int attempts = Mathf.Clamp(types.Count, 1, 8);
            for (int i = 0; i < attempts; i++)
            {
                var t = types[Random.Range(0, types.Count)];
                var cfg = GetRandomForType(t);
                if (cfg != null) return cfg;
            }

            // Fallback deterministic scan
            for (int i = 0; i < types.Count; i++)
            {
                var cfg = GetRandomForType(types[i]);
                if (cfg != null) return cfg;
            }
            return null;
        }
    }

    // Lightweight list pool to avoid allocations when picking any type
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>();
        public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>();
        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}
