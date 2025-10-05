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
        [Header("Options")]
        [SerializeField] private bool _autoSpawnOnStart = false;
        [SerializeField, Min(0)] private int _autoConfigIndex = 0;
        [SerializeField, Min(1)] private int _autoCount = 1;
        [SerializeField, Min(0f)] private float _spawnRadius = 0f; // random within radius if > 0

        [Header("Random Loop Spawning")]
        [SerializeField] private bool _spawnRandomLoop = false;
        [SerializeField, Min(0.1f)] private float _delaySeconds = 2f;
        [SerializeField, Min(0f)] private float _timeDisplacement = 0f; // per-spawner offset range [0..timeDisplacement]

        [Header("Runtime References")]
        [SerializeField] private EnemyRegistry _enemyRegistry;

        private Coroutine _loopCoroutine;
        private Coroutine _waveServiceCoroutine;
        private bool _stopped;
        private float _desyncOffset; // computed once per spawner

        private IObjectResolver _resolver;
        private IWaveService _waveService;
        private IEnemyConfigProvider _configProvider;

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

        [Inject]
        public void SetConfigProvider(IEnemyConfigProvider provider)
        {
            _configProvider = provider;
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

            // compute per-spawner offset once to desync spawners
            _desyncOffset = _timeDisplacement > 0f ? Random.Range(0f, _timeDisplacement) : 0f;

            // Centralized WaveService takes precedence if available
            if (_waveService != null && !_waveService.IsFinished)
            {
                _waveServiceCoroutine = StartCoroutine(SpawnWithWaveService());
                return;
            }

            if (_autoSpawnOnStart)
            {
                SpawnAuto();
            }

            if (_spawnRandomLoop)
            {
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
            if (_waveServiceCoroutine != null)
            {
                StopCoroutine(_waveServiceCoroutine);
                _waveServiceCoroutine = null;
            }
        }

        public void SpawnAuto()
        {
            if (_stopped) return;
            if (_configProvider == null)
            {
                GameLogger.LogError("EnemySpawner: No IEnemyConfigProvider available. Assign EnemyConfigCatalog in GameplayScope.");
                return;
            }

            for (int i = 0; i < _autoCount; i++)
            {
                var cfg = _configProvider.GetRandomAny();
                if (cfg == null)
                {
                    GameLogger.LogWarning("EnemySpawner: ConfigProvider returned null config; aborting auto spawn.");
                    break;
                }
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
            if (_configProvider == null)
            {
                GameLogger.LogError("EnemySpawner: No IEnemyConfigProvider available for random loop.");
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
                var firstCfg = _configProvider.GetRandomAny();
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

                var cfg = _configProvider.GetRandomAny();
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
            }
        }


        private IEnumerator SpawnWithWaveService()
        {
            if (_configProvider == null)
            {
                GameLogger.LogError("EnemySpawner: No IEnemyConfigProvider available for wave service spawning.");
                yield break;
            }

            // Initial desync
            if (!_stopped)
            {
                float upper = Mathf.Max(0.1f, _timeDisplacement);
                float initialDelay = Random.Range(0.1f, upper);
                yield return new WaitForSeconds(initialDelay);
            }

            while (!_stopped && _waveService != null && !_waveService.IsFinished)
            {
                var types = _waveService.AllowedTypes;
                if (types != null && types.Count > 0)
                {
                    var cfg = _configProvider.GetRandomForTypes(types);
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

                    // wait for pacing delay
                    float wait = Mathf.Max(0.01f, _delaySeconds + _desyncOffset);
                    yield return new WaitForSeconds(wait);
                }
                else
                {
                    // no allowed types right now, try next frame
                    yield return null;
                }
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
