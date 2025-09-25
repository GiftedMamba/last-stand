using System;
using System.Collections.Generic;
using Game.Gameplay.Towers;
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
        private readonly HashSet<TowerHealth> _subscribed = new();
        private int _aliveCount;
        private bool _gameOverShown;

        [Inject]
        public void Construct(IScreenService screenService)
        {
            _screenService = screenService;
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
            GameOverShown?.Invoke();
            if (_screenService == null)
            {
                Game.Core.GameLogger.LogError("GameOverController: IScreenService is not injected. Game Over screen cannot be shown. Ensure GameplayScope registers ScreenService and the controller is registered for injection.");
                return;
            }
            _screenService.Show(_gameOverScreenPrefabName);
        }
    }
}
