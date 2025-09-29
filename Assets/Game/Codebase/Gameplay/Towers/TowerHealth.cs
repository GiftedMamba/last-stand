using System;
using Game.Core;
using UnityEngine;
using Game.Gameplay.Health;

namespace Game.Gameplay.Towers
{
    /// <summary>
    /// Basic health component for towers/spots. Holds HP and exposes TakeDamage/Heal.
    /// Attach this to the root Tower GameObject. Other components (like TowerHitTrigger)
    /// will search on self or in parents for this component to apply damage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerHealth : MonoBehaviour, IHealth
    {
        [SerializeField, Min(1)] private int _maxHp = 100;
        [SerializeField, Min(0)] private int _currentHp = 100;

        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public bool IsDead => _currentHp <= 0;

        public event Action<int, int> OnDamaged; // (amount, currentHp)
        public event Action OnDied;

        private void OnValidate()
        {
            _maxHp = Mathf.Max(1, _maxHp);
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
        }

        private void Awake()
        {
            if (_currentHp <= 0)
                _currentHp = _maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            int prev = _currentHp;
            _currentHp = Mathf.Max(0, _currentHp - amount);
            OnDamaged?.Invoke(prev - _currentHp, _currentHp);

            GameLogger.Log($"TowerHealth: Took {amount} damage. HP {_currentHp}/{_maxHp}.");

            if (_currentHp == 0)
            {
                OnDied?.Invoke();
                // Destroy the tower GameObject when HP reaches zero
                Destroy(gameObject);
                // Later: dispatch domain event or inform a game over service
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
        }

        public void SetMaxHp(int maxHp, bool fill = true)
        {
            _maxHp = Mathf.Max(1, maxHp);
            if (fill) _currentHp = _maxHp;
            else _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
        }
    }
}
