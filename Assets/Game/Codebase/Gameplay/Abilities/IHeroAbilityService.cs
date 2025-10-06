using Game.Configs;
using Game.Gameplay.Abilities;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Service that manages hero-specific ability levels at runtime.
    /// Implementations should store current levels and apply side effects if needed.
    /// </summary>
    public interface IHeroAbilityService
    {
        void IncreaseLevel(HeroAbilityType abilityType);
        int GetLevel(HeroAbilityType abilityType);
        void RegisterConfig(PlayerConfig playerConfig);
        bool CanIncrease(HeroAbilityType abilityType);
        void Reset();
    }
}