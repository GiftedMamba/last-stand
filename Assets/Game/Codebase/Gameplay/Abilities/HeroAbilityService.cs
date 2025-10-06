using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Minimal implementation of IHeroAbilityService that tracks hero ability levels in-memory.
    /// Intended to be driven by UI (LevelUpScreen). Gameplay systems can query this service later.
    /// </summary>
    public sealed class HeroAbilityService : IHeroAbilityService
    {
        private readonly Dictionary<HeroAbilityType, int> _levels = new();

        public void IncreaseLevel(HeroAbilityType abilityType)
        {
            if (abilityType == HeroAbilityType.Unknown)
            {
                GameLogger.LogWarning("HeroAbilityService: Attempted to increase level for Unknown ability type.");
                return;
            }

            int current = 0;
            _levels.TryGetValue(abilityType, out current);
            current++;
            _levels[abilityType] = current;

            GameLogger.Log($"HeroAbilityService: Increased {abilityType} to level {current}.");
        }

        public int GetLevel(HeroAbilityType abilityType)
        {
            return _levels.TryGetValue(abilityType, out var lvl) ? (lvl < 0 ? 0 : lvl) : 0;
        }
    }
}
