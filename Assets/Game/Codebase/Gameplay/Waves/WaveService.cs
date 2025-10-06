using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using Game.Gameplay.Enemies;
using Game.Gameplay.GameOver;
using Game.UI.Screens;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Waves
{
    /// <summary>
    /// Centralized wave timer/logic shared by all spawners.
    /// Ticks via VContainer ITickable; exposes currently allowed enemy types.
    /// </summary>
    public sealed class WaveService : IWaveService, IStartable, ITickable
    {
        private readonly WaveConfig _config;
        private readonly IScreenService _screenService;
        private readonly GameOverController _gameOverController;
        private readonly Game.Gameplay.Enemies.EnemyRegistry _enemyRegistry;

        private readonly List<EnemyType> _allowedTypes = new();
        private int _currentWaveIndex = -1;
        private float _elapsed;
        private float _waveElapsed;
        private bool _finished;
        private bool _loggedWin;
        private bool _waitForClear;

        public WaveService(WaveConfig config, IScreenService screenService, GameOverController gameOverController, Game.Gameplay.Enemies.EnemyRegistry enemyRegistry)
        {
            _config = config;
            _screenService = screenService;
            _gameOverController = gameOverController;
            _enemyRegistry = enemyRegistry;
        }

        public IReadOnlyList<EnemyType> AllowedTypes => _allowedTypes;
        public float CurrentWaveRemaining
        {
            get
            {
                var waves = _config?.Waves;
                if (_finished || waves == null || waves.Count == 0 || _currentWaveIndex < 0)
                    return 0f;
                float currentDuration = Math.Max(0.01f, waves[_currentWaveIndex].Time);
                var rem = currentDuration - _waveElapsed;
                return rem > 0f ? rem : 0f;
            }
        }
        public bool IsFinished => _finished;
        public int CurrentWaveNumber
        {
            get
            {
                if (_finished || _currentWaveIndex < 0)
                    return 0;
                return _currentWaveIndex + 1;
            }
        }
        public event Action Finished;

        public void Start()
        {
            _elapsed = 0f;
            _waveElapsed = 0f;
            _currentWaveIndex = -1;
            _finished = false;
            _loggedWin = false;
            _waitForClear = false;
            _allowedTypes.Clear();

            // Immediately enter first wave if any
            var waves = _config?.Waves;
            if (waves == null || waves.Count == 0)
            {
                CompleteOnce();
            }
            else
            {
                EnterWave(0);
            }
        }

        public void Tick()
        {
            if (_config == null || _config.Waves == null || _config.Waves.Count == 0)
                return;

            if (_finished)
            {
                if (_waitForClear && _enemyRegistry != null)
                {
                    var list = _enemyRegistry.Enemies;
                    int alive = list != null ? list.Count : 0;
                    if (alive <= 0)
                    {
                        // All enemies cleared: show win now
                        ShowWinOnce();
                        _waitForClear = false;
                    }
                }
                return;
            }

            float dt = UnityEngine.Time.deltaTime;
            _elapsed += dt;
            _waveElapsed += dt;

            AdvanceIfNeeded();
        }

        private void AdvanceIfNeeded()
        {
            var waves = _config?.Waves;
            if (waves == null || waves.Count == 0)
            {
                // No waves configured; finish immediately
                CompleteOnce();
                return;
            }

            if (_currentWaveIndex < 0)
                return;

            // Each wave's Time is treated as its duration
            float currentDuration = Math.Max(0.01f, waves[_currentWaveIndex].Time);
            if (_waveElapsed >= currentDuration)
            {
                int nextIndex = _currentWaveIndex + 1;
                if (nextIndex < waves.Count)
                {
                    EnterWave(nextIndex);
                }
                else
                {
                    // Completed the last wave's duration
                    CompleteOnce();
                }
            }
        }

        private void EnterWave(int index)
        {
            _currentWaveIndex = index;
            _waveElapsed = 0f;
            _allowedTypes.Clear();

            var waves = _config?.Waves;
            if (waves != null && index >= 0 && index < waves.Count)
            {
                var list = waves[index].EnemyTypes;
                if (list != null && list.Count > 0)
                {
                    // Only use explicitly specified enemy types for this wave; ignore Unknown and duplicates
                    var seen = new System.Collections.Generic.HashSet<Game.Gameplay.Enemies.EnemyType>();
                    foreach (var t in list)
                    {
                        if (t == Game.Gameplay.Enemies.EnemyType.Unknown) continue;
                        if (seen.Add(t))
                            _allowedTypes.Add(t);
                    }
                }
            }

            // Diagnostics to help verify allowed types in Editor
            if (_allowedTypes.Count > 0)
            {
                try
                {
                    GameLogger.Log($"WaveService: Entered wave {index}. Allowed: {string.Join(", ", _allowedTypes)}");
                }
                catch { /* ignore formatting issues */ }
            }
            else
            {
                GameLogger.LogWarning($"WaveService: Entered wave {index} but no allowed enemy types are configured.");
            }
        }

        private void CompleteOnce()
        {
            if (_finished) return;
            _finished = true;
            _allowedTypes.Clear();

            // Decide whether to wait for all enemies to be cleared before showing WinScreen
            int alive = 0;
            if (_enemyRegistry != null && _enemyRegistry.Enemies != null)
                alive = _enemyRegistry.Enemies.Count;

            if (alive > 0)
            {
                // Defer showing win screen until enemies are cleared
                _waitForClear = true;
            }
            else
            {
                ShowWinOnce();
            }
        }

        private void ShowWinOnce()
        {
            if (_loggedWin)
                return;

            _loggedWin = true;
            // Show Win Screen
            if (_screenService != null)
            {
                var instance = _screenService.Show("WinScreen");
                if (instance != null)
                {
                    var win = instance.GetComponentInChildren<WinScreenBehaviour>(true);
                    if (win != null)
                    {
                        int stars = 0;
                        if (_gameOverController != null)
                        {
                            stars = Mathf.Max(0, _gameOverController.AliveTowersCount);
                        }
                        win.SetStarsCount(stars);
                    }
                    else
                    {
                        GameLogger.LogWarning("WaveService: WinScreen shown but WinScreenBehaviour not found. Stars will not be initialized.");
                    }
                }
            }
            else
            {
                GameLogger.LogWarning("WaveService: IScreenService not available. Cannot show WinScreen.");
            }

            try { Finished?.Invoke(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            _allowedTypes.Clear();
            Finished = null;
        }
    }
}
