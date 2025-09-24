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
            _subscribed.Clear();
            _aliveCount = 0;

            if (_towers != null)
            {
                foreach (var t in _towers)
                {
                    if (t == null) continue;
                    if (_subscribed.Add(t))
                    {
                        t.OnDied += OnTowerDied;
                    }
                    if (!t.IsDead) _aliveCount++;
                }
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
            if (_screenService == null) return;
            _screenService.Show(_gameOverScreenPrefabName);
        }
    }
}
