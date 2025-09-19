using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Spawns enemies using EnemyConfig. Place on a spawn point or a manager object.
    /// Requires EnemyConfig assets with Enemy-prefab assigned (with Enemy component).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Configs (assign in Inspector)")]
        [SerializeField] private List<EnemyConfig> _enemyConfigs = new();

        [Header("Options")]
        [SerializeField] private bool _autoSpawnOnStart = false;
        [SerializeField, Min(0)] private int _autoConfigIndex = 0;
        [SerializeField, Min(1)] private int _autoCount = 1;
        [SerializeField, Min(0f)] private float _spawnRadius = 0f; // random within radius if > 0

        private void Start()
        {
            if (_autoSpawnOnStart)
            {
                SpawnAuto();
            }
        }

        public void SpawnAuto()
        {
            if (_enemyConfigs.Count == 0)
            {
                GameLogger.LogWarning("EnemySpawner: No enemy configs assigned.");
                return;
            }

            int idx = Mathf.Clamp(_autoConfigIndex, 0, _enemyConfigs.Count - 1);
            var cfg = _enemyConfigs[idx];
            for (int i = 0; i < _autoCount; i++)
            {
                Vector3 pos = transform.position;
                if (_spawnRadius > 0f)
                {
                    var offset = Random.insideUnitCircle * _spawnRadius;
                    pos += new Vector3(offset.x, 0f, offset.y);
                }
                Spawn(cfg, pos, transform.rotation);
            }
        }

        public Enemy Spawn(EnemyConfig config) => Spawn(config, transform.position, transform.rotation);

        public Enemy Spawn(EnemyConfig config, Vector3 position, Quaternion rotation)
        {
            if (config == null || config.EnemyPrefab == null)
            {
                GameLogger.LogError("EnemySpawner: Config or prefab is null.");
                return null;
            }

            var go = Instantiate(config.EnemyPrefab, position, rotation);
            go.name = config.EnemyPrefab.name;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                GameLogger.LogError("EnemySpawner: Spawned prefab missing Enemy component.");
                Destroy(go);
                return null;
            }

            enemy.InitializeFromConfig(config);
            return enemy;
        }
    }
}
