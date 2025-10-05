using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Core;
using Game.Gameplay.Enemies;
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

        private readonly List<EnemyType> _allowedTypes = new();
        private int _currentWaveIndex = -1;
        private float _elapsed;
        private float _waveElapsed;
        private bool _finished;
        private bool _loggedWin;

        public WaveService(WaveConfig config)
        {
            _config = config;
        }

        public IReadOnlyList<EnemyType> AllowedTypes => _allowedTypes;
        public bool IsFinished => _finished;
        public event Action Finished;

        public void Start()
        {
            _elapsed = 0f;
            _waveElapsed = 0f;
            _currentWaveIndex = -1;
            _finished = false;
            _loggedWin = false;
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
            if (_finished || _config == null || _config.Waves == null || _config.Waves.Count == 0)
                return;

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
                    _allowedTypes.AddRange(list);
                }
            }
        }

        private void CompleteOnce()
        {
            if (_finished) return;
            _finished = true;
            _allowedTypes.Clear();
            if (!_loggedWin)
            {
                GameLogger.Log("You win!");
                _loggedWin = true;
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
