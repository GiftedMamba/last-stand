using Game.Configs;
using Game.Core;

namespace Game.Gameplay.Abilities
{
    public class GlobalAbilityService : IGlobalAbilityService
    {
        private readonly GlobalAbilityCatalog _catalog;
        private readonly IGlobalAbilityExecutor _executor;

        // In the current iteration, all abilities start at level 0.
        // Future iterations can evolve this into a persisted progression map.
        private const int DefaultLevelIndex = 0;

        public GlobalAbilityService(GlobalAbilityCatalog catalog, IGlobalAbilityExecutor executor)
        {
            _catalog = catalog;
            _executor = executor;
        }

        public void Trigger(GlobalAbility ability)
        {
            var config = _catalog != null ? _catalog.Get(ability) : null;
            if (config == null)
            {
                GameLogger.Log($"[GlobalAbilityService] Triggered {ability} (no config found in catalog)");
                return;
            }

            var level = BuildLevel(config, DefaultLevelIndex);
            GameLogger.Log($"[GlobalAbilityService] Triggered {ability} | L{level.LevelIndex} Cooldown={level.Cooldown}s, Duration={level.Duration}s");

            switch (ability)
            {
                case GlobalAbility.Stun:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Stun effect.");
                        return;
                    }
                    _executor.StunForSeconds(level.Duration, level.Damage, level.IsPercent, config.VfxPrefab);
                    break;
                case GlobalAbility.Howl:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Howl effect.");
                        return;
                    }
                    _executor.ApplyHowl(level.Duration, level.Damage, level.IsPercent, config.VfxPrefab);
                    break;
                case GlobalAbility.Shield:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Shoied effect.");
                        return;
                    }
                    _executor.ApplyShoied(level.Duration, config.VfxPrefab);
                    break;
                case GlobalAbility.Cannon:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Cannon effect.");
                        return;
                    }
                    _executor.ApplyCannon(level.Duration, level.Damage, level.IsPercent, level.Splash, config.VfxPrefab);
                    break;
                default:
                    // Other abilities to be implemented later
                    break;
            }
        }

        public GlobalAbilityLevel GetCurrentLevel(GlobalAbility ability)
        {
            var config = _catalog != null ? _catalog.Get(ability) : null;
            if (config == null) return null;
            return BuildLevel(config, DefaultLevelIndex);
        }

        private static GlobalAbilityLevel BuildLevel(GlobalAbilityConfig config, int levelIndex)
        {
            var entry = config.GetLevel(levelIndex);
            if (entry == null)
            {
                // Fallback to level 0 if available
                entry = config.GetLevel(0);
            }

            if (entry == null)
            {
                // No level data defined: return zeros to avoid nulls
                return new GlobalAbilityLevel(
                    config.Ability,
                    levelIndex,
                    0f,
                    0f,
                    0f,
                    true,
                    0f
                );
            }

            return new GlobalAbilityLevel(
                config.Ability,
                levelIndex,
                entry.Cooldown,
                entry.Duration,
                entry.Damage,
                entry.IsPercent,
                entry.Splash
            );
        }
    }
}