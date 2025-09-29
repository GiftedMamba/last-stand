using System;

namespace Game.Gameplay.Health
{
    /// <summary>
    /// Generic health interface usable by UI and gameplay. Implement on any unit that has HP.
    /// </summary>
    public interface IHealth
    {
        int MaxHp { get; }
        int CurrentHp { get; }
        bool IsDead { get; }

        event Action<int, int> OnDamaged; // (amount, currentHp)
        event Action OnDied;
    }
}
