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

        private readonly Dictionary<EnemyType, List<EnemyConfig>> _byType = new();
        private bool _built;

        public IReadOnlyList<EnemyConfig> Configs => _configs;

        private void BuildIfNeeded()
        {
            if (_built)
                return;
            _byType.Clear();
            if (_configs != null)
            {
                foreach (var cfg in _configs)
                {
                    if (cfg == null) continue;
                    var t = cfg.Type;
                    if (!_byType.TryGetValue(t, out var list))
                    {
                        list = new List<EnemyConfig>();
                        _byType[t] = list;
                    }
                    list.Add(cfg);
                }
            }
            _built = true;
        }

        public EnemyConfig GetRandomAny()
        {
            BuildIfNeeded();
            if (_configs == null || _configs.Count == 0) return null;
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
            foreach (var t in types)
            {
                var cfg = GetRandomForType(t);
                if (cfg != null) return cfg;
            }
            return null;
        }
    }
}
