using Game.Configs;
using Game.Core;

namespace Game.Gameplay.Abilities
{
    public class GlobalAbilityService : IGlobalAbilityService
    {
        private readonly GlobalAbilityCatalog _catalog;
        private readonly IGlobalAbilityExecutor _executor;

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

            GameLogger.Log($"[GlobalAbilityService] Triggered {ability} | Cooldown={config.Cooldown}s, Duration={config.Duration}s");

            switch (ability)
            {
                case GlobalAbility.Stun:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Stun effect.");
                        return;
                    }
                    _executor.StunForSeconds(config.Duration);
                    break;
                case GlobalAbility.Howl:
                    if (_executor == null)
                    {
                        GameLogger.LogWarning("[GlobalAbilityService] No executor available to apply Howl effect.");
                        return;
                    }
                    _executor.ApplyHowl(config.Duration, config.DamagePercent);
                    break;
                default:
                    // Other abilities to be implemented later
                    break;
            }
        }
    }
}