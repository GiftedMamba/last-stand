using System;
using System.Collections;
using System.Collections.Generic;
using Game.Configs;
using Game.Gameplay.Enemies;
using Game.Gameplay.Towers;
using Game.Gameplay.Waves;
using Game.UI.Screens;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.GameOver
{
    /// <summary>
    /// Listens to multiple towers and shows the Game Over screen based on a lose condition.
    /// Assign all tower health components via inspector.
    /// </summary>
    public sealed class GameOverController : MonoBehaviour
    {
        public static event Action GameOverShown;
        public static bool IsGameOver { get; private set; }

        public enum LoseCondition
        {
            AnyTowerDestroyed,
            AllTowersDestroyed
        }

        [Header("Config")]
        [SerializeField] private string _gameOverScreenPrefabName = "GameOverScreen";
        [SerializeField] private LoseCondition _loseCondition = LoseCondition.AllTowersDestroyed;

        [Header("Bindings")]
        [SerializeField] private TowerHealth[] _towers;

        private IScreenService _screenService;
        private EnemyRegistry _enemyRegistry;
        private WaveConfig _waveConfig;
        private IWaveService _waveService;

        private readonly HashSet<TowerHealth> _subscribed = new();
        private int _aliveCount;
        private bool _gameOverShown;
        private Coroutine _delayRoutine;

        public int AliveTowersCount => _aliveCount;

        [Inject]
        public void Construct(IScreenService screenService, EnemyRegistry enemyRegistry, WaveConfig waveConfig, IWaveService waveService)
        {
            _screenService = screenService;
            _enemyRegistry = enemyRegistry;
            _waveConfig = waveConfig;
            _waveService = waveService;
        }

        private void OnEnable()
        {
            IsGameOver = false;
            _subscribed.Clear();
            _aliveCount = 0;

            // Validate towers assignment early
            if (_towers == null || _towers.Length == 0)
            {
                Game.Core.GameLogger.LogError("GameOverController: No towers assigned in inspector. Please assign at least one TowerHealth reference.");
                return;
            }

            foreach (var t in _towers)
            {
                if (t == null) continue;
                if (_subscribed.Add(t))
                {
                    t.OnDied += OnTowerDied;
                }
                if (!t.IsDead) _aliveCount++;
            }

            if (_subscribed.Count == 0)
            {
                Game.Core.GameLogger.LogError("GameOverController: All assigned tower references are null. Please assign valid TowerHealth components.");
                return;
            }

            // Evaluate initial state in case some towers are already dead (e.g., in tests or specific setups)
            EvaluateLoseCondition(initialCheck: true);
        }

        private void OnDisable()
        {
            foreach (var t in _subscribed)
            {
                if (t != null) t.OnDied -= OnTowerDied;
            }
            _subscribed.Clear();

            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }
        }

        private void OnTowerDied()
        {
            if (_gameOverShown) return;
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            EvaluateLoseCondition();
        }

        private void EvaluateLoseCondition(bool initialCheck = false)
        {
            if (_gameOverShown) return;

            switch (_loseCondition)
            {
                case LoseCondition.AnyTowerDestroyed:
                    // If this is an event (not initial) we already know at least one tower died
                    if (!initialCheck || AnyTowerCurrentlyDead())
                    {
                        ShowGameOver();
                    }
                    break;
                case LoseCondition.AllTowersDestroyed:
                    if (_aliveCount <= 0 && _subscribed.Count > 0)
                    {
                        ShowGameOver();
                    }
                    break;
            }
        }

        private bool AnyTowerCurrentlyDead()
        {
            foreach (var t in _subscribed)
            {
                if (t != null && t.IsDead) return true;
            }
            return false;
        }

        private void ShowGameOver()
        {
            if (_gameOverShown) return;
            _gameOverShown = true;
            IsGameOver = true;
            try { GameOverShown?.Invoke(); } catch { /* ignore */ }

            if (_screenService == null)
            {
                Game.Core.GameLogger.LogError("GameOverController: IScreenService is not injected. Game Over screen cannot be shown. Ensure GameplayScope registers ScreenService and the controller is registered for injection.");
                return;
            }

            // Apply same delay conditions as WinScreen: wait until all enemies are cleared and ClearDelaySeconds elapsed
            float delay = 0f;
            try
            {
                if (_waveConfig != null && _waveConfig.Waves != null && _waveConfig.Waves.Count > 0 && _waveService != null)
                {
                    int currentWaveIndex = Mathf.Clamp(_waveService.CurrentWaveNumber - 1, 0, _waveConfig.Waves.Count - 1);
                    delay = Mathf.Max(0f, _waveConfig.Waves[currentWaveIndex].ClearDelaySeconds);
                }
            }
            catch { /* ignore and use 0 delay */ }

            bool needWaitEnemies = false;
            try
            {
                needWaitEnemies = _enemyRegistry != null && _enemyRegistry.Enemies != null && _enemyRegistry.Enemies.Count > 0;
            }
            catch { /* default false */ }

            if (delay > 0f || needWaitEnemies)
            {
                if (_delayRoutine != null) StopCoroutine(_delayRoutine);
                _delayRoutine = StartCoroutine(DelayAndShow(delay));
            }
            else
            {
                _screenService.Show(_gameOverScreenPrefabName);
            }
        }

        private IEnumerator DelayAndShow(float delaySeconds)
        {
            float elapsed = 0f;
            // Wait for delay
            while (elapsed < delaySeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Then wait until all enemies are cleared
            while (true)
            {
                int count = 0;
                try { count = (_enemyRegistry != null && _enemyRegistry.Enemies != null) ? _enemyRegistry.Enemies.Count : 0; }
                catch { count = 0; }
                if (count <= 0) break;
                yield return null;
            }

            _screenService.Show(_gameOverScreenPrefabName);
            _delayRoutine = null;
        }
    }
}
