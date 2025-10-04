using Game.Gameplay.Abilities;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Runtime value object describing a specific level of a global ability.
    /// Mirrors numeric/bool data from GlobalAbilityConfig but excludes any visuals (VFX, sprites, etc.).
    /// </summary>
    public sealed class GlobalAbilityLevel
    {
        public GlobalAbility Ability { get; }
        public int LevelIndex { get; }
        public float Cooldown { get; }
        public float Duration { get; }
        public float Damage { get; }
        public bool IsPercent { get; }
        public float Splash { get; }

        public GlobalAbilityLevel(GlobalAbility ability, int levelIndex, float cooldown, float duration, float damage, bool isPercent, float splash)
        {
            Ability = ability;
            LevelIndex = levelIndex;
            Cooldown = cooldown;
            Duration = duration;
            Damage = damage;
            IsPercent = isPercent;
            Splash = splash;
        }
    }
}