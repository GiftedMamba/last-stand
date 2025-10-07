using Game.Configs;
using Game.Core;
using Game.Gameplay.Waves;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Spawns a boss defined by the current wave configuration in the last second of the wave.
    /// Place this component in the scene to define the spawn origin (its transform).
    /// </summary>
    public class BossSpawner : MonoBehaviour
    {
        [Header("Spawn Options")]
        [Tooltip("Optional random radius around this transform to place the boss. 0 to use exact transform position.")]
        [SerializeField, Min(0f)] private float _spawnRadius = 0f;

        private IWaveService _waveService;
        private WaveConfig _waveConfig;
        private IEnemyConfigProvider _configProvider;
        private IObjectResolver _resolver;
        private EnemyRegistry _enemyRegistry;

        // Track which wave we already spawned a boss for to avoid duplicates
        private int _lastWaveBossSpawned = 0; // 1-based wave number; 0 = none

        [Inject]
        public void InjectServices(IWaveService waveService, WaveConfig waveConfig, IEnemyConfigProvider configProvider, IObjectResolver resolver, EnemyRegistry enemyRegistry)
        {
            _waveService = waveService;
            _waveConfig = waveConfig;
            _configProvider = configProvider;
            _resolver = resolver;
            _enemyRegistry = enemyRegistry;
        }

        private void Update()
        {
            if (_waveService == null || _waveConfig == null || _configProvider == null)
                return;
            if (_waveService.IsFinished)
                return;

            int waveNumber = _waveService.CurrentWaveNumber; // 1-based; 0 if none
            if (waveNumber <= 0) return;
            if (_lastWaveBossSpawned == waveNumber) return; // already spawned for this wave

            var waves = _waveConfig.Waves;
            if (waves == null || waveNumber > waves.Count) return;

            var current = waves[waveNumber - 1];
            var bossType = current.BossType;
            if (bossType == EnemyType.Unknown) return; // no boss for this wave

            // Spawn in the last second of the wave
            if (_waveService.CurrentWaveRemaining <= 1.0f)
            {
                var cfg = _configProvider.GetRandomForType(bossType);
                if (cfg == null)
                {
                    GameLogger.LogWarning($"BossSpawner: No EnemyConfig found for boss type '{bossType}'. Skipping boss spawn.");
                    _lastWaveBossSpawned = waveNumber; // avoid retry spam within the same wave
                    return;
                }

                Vector3 pos = transform.position;
                if (_spawnRadius > 0f)
                {
                    var offset = Random.insideUnitCircle * _spawnRadius;
                    pos += new Vector3(offset.x, 0f, offset.y);
                }

                SpawnBoss(cfg, pos, transform.rotation);
                _lastWaveBossSpawned = waveNumber;
            }
        }

        private void SpawnBoss(EnemyConfig config, Vector3 position, Quaternion rotation)
        {
            if (config == null || config.EnemyPrefab == null)
            {
                GameLogger.LogError("BossSpawner: Invalid EnemyConfig for boss spawn.");
                return;
            }

            var go = _resolver != null
                ? _resolver.Instantiate(config.EnemyPrefab, position, rotation)
                : UnityEngine.Object.Instantiate(config.EnemyPrefab, position, rotation);
            go.name = config.EnemyPrefab.name;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                GameLogger.LogError("BossSpawner: Spawned prefab missing Enemy component.");
                UnityEngine.Object.Destroy(go);
                return;
            }

            enemy.InitializeFromConfig(config);

            // Ensure XP awarder exists and injected
            var xpAwarder = go.GetComponent<EnemyXpAwarder>();
            if (xpAwarder == null)
            {
                xpAwarder = go.AddComponent<EnemyXpAwarder>();
                if (_resolver != null)
                {
                    _resolver.Inject(xpAwarder);
                }
            }

            // Register in enemy registry if available
            if (_enemyRegistry != null)
            {
                var handle = go.GetComponent<EnemyRegistryHandle>();
                if (handle == null) handle = go.AddComponent<EnemyRegistryHandle>();
                handle.Init(_enemyRegistry, enemy);
                _enemyRegistry.Add(enemy);
            }

            GameLogger.Log($"BossSpawner: Spawned boss '{config.name}' ({enemy.Type}) at {position}.");
        }
    }
}
