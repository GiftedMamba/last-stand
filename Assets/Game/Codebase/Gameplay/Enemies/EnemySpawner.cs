using System.Collections;
using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using Game.Gameplay.GameOver;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.Waves;

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

        [Header("Random Loop Spawning")]
        [SerializeField] private bool _spawnRandomLoop = false;
        [SerializeField, Min(0.1f)] private float _delaySeconds = 2f;
        [SerializeField, Min(0f)] private float _timeDisplacement = 0f; // per-spawner offset range [0..timeDisplacement]

        [Header("Waves (optional)")]
        [SerializeField] private bool _useWaveConfig = false;
        [SerializeField] private WaveConfig _waveConfig;
        [SerializeField, Min(0.1f)] private float _waveSpawnDelay = 2f;

        [Header("Runtime References")]
        [SerializeField] private EnemyRegistry _enemyRegistry;

        private Coroutine _loopCoroutine;
        private Coroutine _wavesCoroutine;
        private Coroutine _waveServiceCoroutine;
        private bool _stopped;
        private float _desyncOffset; // computed once per spawner

        private System.Collections.Generic.Dictionary<EnemyType, System.Collections.Generic.List<EnemyConfig>> _configsByType;

        private IObjectResolver _resolver;
        private IWaveService _waveService;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }
 
        [Inject]
        public void SetWaveService(Game.Gameplay.Waves.IWaveService waveService)
        {
            _waveService = waveService;
        }

         private void OnEnable()
         {
             GameOverController.GameOverShown += HandleGameOver;
         }
         
         private void Start()
         {
            // Fallback: if DI didn't run, try to grab a resolver from nearest LifetimeScope
            if (_resolver == null)
            {
                var scope = GetComponentInParent<VContainer.Unity.LifetimeScope>();
                if (scope != null)
                    _resolver = scope.Container;
            }

            if (GameOverController.IsGameOver)
            {
                _stopped = true;
                return;
            }

            // Build type map for fast lookup in any mode
            BuildTypeMap();

            // Centralized WaveService takes precedence if available
            if (_waveService != null && !_waveService.IsFinished)
            {
                _waveServiceCoroutine = StartCoroutine(SpawnWithWaveService());
                return;
            }

            // Legacy per-spawner WaveConfig mode
            if (_useWaveConfig && _waveConfig != null && _waveConfig.Waves != null && _waveConfig.Waves.Count > 0)
            {
                _wavesCoroutine = StartCoroutine(SpawnWavesLoop());
                return;
            }

            if (_autoSpawnOnStart)
            {
                SpawnAuto();
            }

            if (_spawnRandomLoop)
            {
                // compute per-spawner offset once to desync spawners without delaying the very first spawn
                _desyncOffset = _timeDisplacement > 0f ? Random.Range(0f, _timeDisplacement) : 0f;
                _loopCoroutine = StartCoroutine(SpawnRandomLoop());
            }
        }

        private void OnDisable()
        {
            GameOverController.GameOverShown -= HandleGameOver;
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
            if (_wavesCoroutine != null)
            {
                StopCoroutine(_wavesCoroutine);
                _wavesCoroutine = null;
            }
            if (_waveServiceCoroutine != null)
            {
                StopCoroutine(_waveServiceCoroutine);
                _waveServiceCoroutine = null;
            }
        }

        private void HandleGameOver()
        {
            _stopped = true;
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
            if (_wavesCoroutine != null)
            {
                StopCoroutine(_wavesCoroutine);
                _wavesCoroutine = null;
            }
            if (_waveServiceCoroutine != null)
            {
                StopCoroutine(_waveServiceCoroutine);
                _waveServiceCoroutine = null;
            }
        }

        public void SpawnAuto()
        {
            if (_stopped) return;
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

        private IEnumerator SpawnRandomLoop()
        {
            if (_enemyConfigs.Count == 0)
            {
                GameLogger.LogWarning("EnemySpawner: No enemy configs assigned for random loop.");
                yield break;
            }

            // Initial desync before the very first spawn to avoid simultaneous starts across spawners
            if (!_stopped)
            {
                float upper = Mathf.Max(0.1f, _timeDisplacement);
                float initialDelay = Random.Range(0.1f, upper);
                yield return new WaitForSeconds(initialDelay);
            }

            // First spawn after desync delay
            if (!_stopped)
            {
                int firstIdx = Random.Range(0, _enemyConfigs.Count);
                var firstCfg = _enemyConfigs[firstIdx];
                if (firstCfg != null)
                {
                    Vector3 firstPos = transform.position;
                    if (_spawnRadius > 0f)
                    {
                        var offset = Random.insideUnitCircle * _spawnRadius;
                        firstPos += new Vector3(offset.x, 0f, offset.y);
                    }
                    Spawn(firstCfg, firstPos, transform.rotation);
                }
            }

            // Subsequent spawns use base delay plus per-spawner desync offset
            while (!_stopped)
            {
                float wait = Mathf.Max(0.01f, _delaySeconds + _desyncOffset);
                yield return new WaitForSeconds(wait);

                int idx = Random.Range(0, _enemyConfigs.Count);
                var cfg = _enemyConfigs[idx];
                if (cfg == null)
                {
                    GameLogger.LogWarning("EnemySpawner: Encountered null EnemyConfig in list; skipping.");
                }
                else
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
        }

        private void BuildTypeMap()
        {
            if (_configsByType == null)
                _configsByType = new System.Collections.Generic.Dictionary<EnemyType, System.Collections.Generic.List<EnemyConfig>>();
            else
                _configsByType.Clear();

            if (_enemyConfigs == null) return;

            foreach (var cfg in _enemyConfigs)
            {
                if (cfg == null) continue;
                var type = cfg.Type;
                if (!_configsByType.TryGetValue(type, out var list))
                {
                    list = new System.Collections.Generic.List<EnemyConfig>();
                    _configsByType[type] = list;
                }
                list.Add(cfg);
            }
        }

        private EnemyConfig GetRandomConfigForTypes(System.Collections.Generic.IReadOnlyList<EnemyType> types)
        {
            if (types == null || types.Count == 0 || _configsByType == null) return null;

            // Try a few random attempts
            int attempts = types.Count;
            for (int i = 0; i < attempts; i++)
            {
                var t = types[Random.Range(0, types.Count)];
                if (_configsByType.TryGetValue(t, out var list) && list != null && list.Count > 0)
                {
                    return list[Random.Range(0, list.Count)];
                }
            }

            // Fallback deterministic scan
            foreach (var t in types)
            {
                if (_configsByType.TryGetValue(t, out var list) && list != null && list.Count > 0)
                {
                    return list[0];
                }
            }

            GameLogger.LogWarning("EnemySpawner: No EnemyConfig matches any of the wave's EnemyTypes.");
            return null;
        }

        private IEnumerator SpawnWavesLoop()
        {
            var waves = _waveConfig != null ? _waveConfig.Waves : null;
            if (waves == null || waves.Count == 0)
            {
                GameLogger.LogWarning("EnemySpawner: Wave mode enabled but WaveConfig has no waves.");
                yield break;
            }

            for (int i = 0; i < waves.Count && !_stopped; i++)
            {
                var wave = waves[i];
                float duration = Mathf.Max(0.01f, wave.Time);
                float elapsed = 0f;

                var types = (System.Collections.Generic.IReadOnlyList<EnemyType>)wave.EnemyTypes;

                float delay = Mathf.Max(0.01f, _waveSpawnDelay);
                float spawnTimer = 0f; // spawn immediately on first tick

                while (!_stopped && elapsed < duration)
                {
                    if (spawnTimer <= 0f)
                    {
                        var cfg = GetRandomConfigForTypes(types);
                        if (cfg != null)
                        {
                            Vector3 pos = transform.position;
                            if (_spawnRadius > 0f)
                            {
                                var offset = Random.insideUnitCircle * _spawnRadius;
                                pos += new Vector3(offset.x, 0f, offset.y);
                            }
                            Spawn(cfg, pos, transform.rotation);
                        }
                        spawnTimer = delay;
                    }

                    yield return null;
                    float dt = Time.deltaTime;
                    elapsed += dt;
                    spawnTimer -= dt;
                }
                // Wave finished: stop spawning for this wave and move to next
            }

            if (!_stopped)
            {
                GameLogger.Log("You win!");
            }

            _wavesCoroutine = null;
        }

        private IEnumerator SpawnWithWaveService()
        {
            float delay = Mathf.Max(0.01f, _waveSpawnDelay);
            float spawnTimer = 0f; // allow immediate spawn when wave provides types

            while (!_stopped && _waveService != null && !_waveService.IsFinished)
            {
                var types = _waveService.AllowedTypes;
                if (types != null && types.Count > 0)
                {
                    if (spawnTimer <= 0f)
                    {
                        var cfg = GetRandomConfigForTypes(types);
                        if (cfg != null)
                        {
                            Vector3 pos = transform.position;
                            if (_spawnRadius > 0f)
                            {
                                var offset = Random.insideUnitCircle * _spawnRadius;
                                pos += new UnityEngine.Vector3(offset.x, 0f, offset.y);
                            }
                            Spawn(cfg, pos, transform.rotation);
                        }
                        spawnTimer = delay;
                    }
                }
                else
                {
                    // No allowed types right now; reset timer so we spawn immediately when types appear
                    spawnTimer = 0f;
                }

                yield return null;
                float dt = Time.deltaTime;
                spawnTimer -= dt;
            }

            _waveServiceCoroutine = null;
        }

        public Enemy Spawn(EnemyConfig config) => Spawn(config, transform.position, transform.rotation);

        public Enemy Spawn(EnemyConfig config, Vector3 position, Quaternion rotation)
        {
            if (_stopped) return null;
            if (config == null || config.EnemyPrefab == null)
            {
                GameLogger.LogError("EnemySpawner: Config or prefab is null.");
                return null;
            }

            var go = _resolver != null
                ? _resolver.Instantiate(config.EnemyPrefab, position, rotation)
                : Instantiate(config.EnemyPrefab, position, rotation);
            go.name = config.EnemyPrefab.name;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                GameLogger.LogError("EnemySpawner: Spawned prefab missing Enemy component.");
                Destroy(go);
                return null;
            }

            enemy.InitializeFromConfig(config);

            // Ensure XP awarder is present
            var xpAwarder = go.GetComponent<EnemyXpAwarder>();
            if (xpAwarder == null)
            {
                xpAwarder = go.AddComponent<EnemyXpAwarder>();
                if (_resolver != null)
                {
                    // Ensure DI on the newly added component
                    _resolver.Inject(xpAwarder);
                }
            }

            // Register with EnemyRegistry if assigned
            if (_enemyRegistry != null)
            {
                var handle = go.GetComponent<EnemyRegistryHandle>();
                if (handle == null) handle = go.AddComponent<EnemyRegistryHandle>();
                handle.Init(_enemyRegistry, enemy);
                // Ensure registration even if OnEnable ran before Init assigned the registry
                _enemyRegistry.Add(enemy);
            }
            return enemy;
        }
    }
}
