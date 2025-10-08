using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using Game.Gameplay.Enemies;
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
        private readonly Game.Gameplay.Enemies.EnemyRegistry _enemyRegistry;

        private readonly List<EnemyType> _allowedTypes = new();
        private int _currentWaveIndex = -1;
        private float _elapsed;
        private float _waveElapsed;
        private bool _finished;
        private bool _loggedWin;
        private bool _waitForClear;
        
        // Win screen delay after all waves done (uses last wave's ClearDelaySeconds)
        private float _winDelaySeconds;
        private float _winDelayElapsed;

        // Inter-wave timeout state
        private bool _betweenWaves;
        private float _betweenElapsed;

        // Early clear state (when all enemies are dead during an active wave)
        private float _clearElapsed;

        public WaveService(WaveConfig config, IScreenService screenService, Game.Gameplay.Enemies.EnemyRegistry enemyRegistry)
        {
            _config = config;
            _screenService = screenService;
            _enemyRegistry = enemyRegistry;
        }

        public IReadOnlyList<EnemyType> AllowedTypes => _allowedTypes;
        public float CurrentWaveRemaining
        {
            get
            {
                var waves = _config?.Waves;
                if (_finished || waves == null || waves.Count == 0)
                    return 0f;

                if (_betweenWaves)
                {
                    float timeout = Mathf.Max(0f, _config != null ? _config.TimeBetweenWaves : 0f);
                    float remBw = timeout - _betweenElapsed;
                    return remBw > 0f ? remBw : 0f;
                }

                if (_currentWaveIndex < 0)
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
        public int TotalWaves
        {
            get
            {
                var waves = _config?.Waves;
                return waves != null ? waves.Count : 0;
            }
        }

        public float CurrentSpawnPeriod
        {
            get
            {
                var waves = _config?.Waves;
                if (waves == null || waves.Count == 0 || _currentWaveIndex < 0 || _finished)
                {
                    return 2f; // sane default if no active wave
                }

                var wave = waves[_currentWaveIndex];
                float duration = Math.Max(0.01f, wave.Time);
                float t = Mathf.Clamp01(duration > 0f ? _waveElapsed / duration : 0f);
                float start = Mathf.Max(0.01f, wave.StartSpawnPeriod);
                float end = Mathf.Max(0.01f, wave.EndSpawnPeriod);
                return Mathf.Lerp(start, end, t);
            }
        }

        public event Action Finished;

        public void Start()
        {
            _elapsed = 0f;
            _waveElapsed = 0f;
            _betweenElapsed = 0f;
            _betweenWaves = false;
            _currentWaveIndex = -1;
            _finished = false;
            _loggedWin = false;
            _waitForClear = false;
            _winDelaySeconds = 0f;
            _winDelayElapsed = 0f;
            _allowedTypes.Clear();
            _clearElapsed = 0f;

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
                        // Wait additional delay before showing win, according to last wave ClearDelaySeconds
                        _winDelayElapsed += UnityEngine.Time.deltaTime;
                        if (_winDelayElapsed >= Mathf.Max(0f, _winDelaySeconds))
                        {
                            ShowWinOnce();
                            _waitForClear = false;
                        }
                    }
                    else
                    {
                        // Enemies reappeared: reset wait timer
                        _winDelayElapsed = 0f;
                    }
                }
                return;
            }

            float dt = UnityEngine.Time.deltaTime;
            _elapsed += dt;

            if (_betweenWaves)
            {
                _betweenElapsed += dt;
            }
            else
            {
                _waveElapsed += dt;

                // Track early-clear timer: when no alive enemies are present during the wave
                if (_enemyRegistry != null)
                {
                    var list = _enemyRegistry.Enemies;
                    int aliveNonDead = 0;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var e = list[i];
                            if (e != null && !e.IsDead)
                                aliveNonDead++;
                        }
                    }

                    if (aliveNonDead <= 0)
                        _clearElapsed += dt;
                    else
                        _clearElapsed = 0f;
                }
                else
                {
                    // Without a registry, we cannot know; disable early-clear timer
                    _clearElapsed = 0f;
                }
            }

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

            if (_currentWaveIndex < 0 && !_betweenWaves)
                return;

            if (_betweenWaves)
            {
                float timeout = Mathf.Max(0f, _config != null ? _config.TimeBetweenWaves : 0f);
                if (_betweenElapsed >= timeout)
                {
                    int nextIndex = _currentWaveIndex + 1;
                    if (nextIndex < waves.Count)
                    {
                        EnterWave(nextIndex);
                    }
                    else
                    {
                        // No next wave; complete
                        CompleteOnce();
                    }
                }
                return;
            }

            // Early clear check: if no enemies for configured delay, end wave early
            var currentWave = waves[_currentWaveIndex];
            float clearDelay = Mathf.Max(0f, currentWave.ClearDelaySeconds);
            if (clearDelay > 0f && _clearElapsed >= clearDelay)
            {
                int nextIndexEarly = _currentWaveIndex + 1;
                if (nextIndexEarly < waves.Count)
                {
                    EnterBetweenWaves();
                }
                else
                {
                    CompleteOnce();
                }
                return;
            }

            // Each wave's Time is treated as its duration
            float currentDuration = Math.Max(0.01f, currentWave.Time);
            if (_waveElapsed >= currentDuration)
            {
                int nextIndex = _currentWaveIndex + 1;
                if (nextIndex < waves.Count)
                {
                    EnterBetweenWaves();
                }
                else
                {
                    // Completed the last wave's duration
                    CompleteOnce();
                }
            }
        }

        private void EnterBetweenWaves()
        {
            _betweenWaves = true;
            _betweenElapsed = 0f;
            _clearElapsed = 0f;
            _allowedTypes.Clear(); // pause spawns during intermission
        }

        private void EnterWave(int index)
        {
            _betweenWaves = false;
            _betweenElapsed = 0f;

            _currentWaveIndex = index;
            _waveElapsed = 0f;
            _clearElapsed = 0f;
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
            _betweenWaves = false;
            _allowedTypes.Clear();

            // Decide whether to wait for all enemies to be cleared before showing WinScreen
            int alive = 0;
            if (_enemyRegistry != null && _enemyRegistry.Enemies != null)
                alive = _enemyRegistry.Enemies.Count;

            // Determine additional delay for WinScreen based on last wave's ClearDelaySeconds
            _winDelaySeconds = 0f;
            _winDelayElapsed = 0f;
            var waves = _config?.Waves;
            if (waves != null && waves.Count > 0)
            {
                int idx = _currentWaveIndex;
                if (idx < 0) idx = waves.Count - 1; // fallback to last
                idx = Mathf.Clamp(idx, 0, waves.Count - 1);
                _winDelaySeconds = Mathf.Max(0f, waves[idx].ClearDelaySeconds);
            }

            if (alive > 0 || _winDelaySeconds > 0f)
            {
                // Defer showing win screen until enemies are cleared and delay has elapsed
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
                        // Stars number is determined by remaining towers; not available here without creating a DI cycle.
                        // Set to 0 here; actual presentation can adjust if needed.
                        win.SetStarsCount(0);
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
