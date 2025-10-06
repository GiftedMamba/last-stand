using System.Collections.Generic;
using Game.Configs;
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
        private PlayerConfig _playerConfig;

        public void RegisterConfig(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

        public bool CanIncrease(HeroAbilityType abilityType)
        {
            if (abilityType == HeroAbilityType.Unknown)
                return false;

            int current = GetLevel(abilityType);
            int maxIndex = GetMaxIndex(abilityType);
            return current < maxIndex;
        }

        public void IncreaseLevel(HeroAbilityType abilityType)
        {
            if (abilityType == HeroAbilityType.Unknown)
            {
                GameLogger.LogWarning("HeroAbilityService: Attempted to increase level for Unknown ability type.");
                return;
            }

            // Respect max level based on config if available
            int current = 0;
            _levels.TryGetValue(abilityType, out current);
            int maxIndex = GetMaxIndex(abilityType);
            if (current >= maxIndex)
            {
                GameLogger.Log($"HeroAbilityService: {abilityType} already at max level {current}.");
                return;
            }

            current++;
            _levels[abilityType] = current;
            GameLogger.Log($"HeroAbilityService: Increased {abilityType} to level {current}.");
        }

        public int GetLevel(HeroAbilityType abilityType)
        {
            return _levels.TryGetValue(abilityType, out var lvl) ? (lvl < 0 ? 0 : lvl) : 0;
        }

        private int GetMaxIndex(HeroAbilityType abilityType)
        {
            // Default max index 0 (meaning only level 0 exists) when no config
            if (_playerConfig == null)
                return 0;

            var abilities = _playerConfig.Abilities;
            if (abilities == null)
                return 0;

            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null || ab.Type != abilityType)
                    continue;
                var levels = ab.Levels;
                if (levels == null || levels.Count == 0)
                    return 0;
                return levels.Count - 1;
            }

            // Ability not found in config => no upgrades defined
            return 0;
        }
    }
}
