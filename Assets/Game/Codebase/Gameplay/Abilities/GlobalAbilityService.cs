using System.Collections.Generic;
using Game.Configs;
using Game.Core;

namespace Game.Gameplay.Abilities
{
    public class GlobalAbilityService : IGlobalAbilityService
    {
        private readonly GlobalAbilityCatalog _catalog;
        private readonly IGlobalAbilityExecutor _executor;

        // Track current level per ability (starts at 0)
        private readonly Dictionary<GlobalAbility, int> _levels = new();

        public GlobalAbilityService(GlobalAbilityCatalog catalog, IGlobalAbilityExecutor executor)
        {
            _catalog = catalog;
            _executor = executor;

            // Initialize known abilities to level 0
            if (_catalog != null && _catalog.Configs != null)
            {
                foreach (var cfg in _catalog.Configs)
                {
                    if (cfg == null) continue;
                    if (!_levels.ContainsKey(cfg.Ability))
                        _levels[cfg.Ability] = 0;
                }
            }
        }

        public void Trigger(GlobalAbility ability)
        {
            var config = _catalog != null ? _catalog.Get(ability) : null;
            if (config == null)
            {
                GameLogger.Log($"[GlobalAbilityService] Triggered {ability} (no config found in catalog)");
                return;
            }

            var levelIndex = GetLevelIndex(ability);
            var level = BuildLevel(config, levelIndex);
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
            var levelIndex = GetLevelIndex(ability);
            return BuildLevel(config, levelIndex);
        }

        public void IncreaseLevel(GlobalAbility ability)
        {
            var config = _catalog != null ? _catalog.Get(ability) : null;
            if (config == null)
            {
                GameLogger.LogWarning($"[GlobalAbilityService] IncreaseLevel called for {ability} but no config found.");
                return;
            }

            var maxIndex = (config.Levels == null || config.Levels.Count == 0) ? 0 : config.Levels.Count - 1;
            var current = GetLevelIndex(ability);
            var next = current < maxIndex ? current + 1 : maxIndex;
            _levels[ability] = next;
            GameLogger.Log($"[GlobalAbilityService] {ability} level increased: {current} -> {next}");
        }

        private int GetLevelIndex(GlobalAbility ability)
        {
            if (_levels.TryGetValue(ability, out var idx))
                return idx;
            return 0;
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