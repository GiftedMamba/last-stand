using Game.Configs;
using Game.Core;

namespace Game.Gameplay.Abilities
{
    public class GlobalAbilityService : IGlobalAbilityService
    {
        private readonly GlobalAbilityCatalog _catalog;

        public GlobalAbilityService(GlobalAbilityCatalog catalog)
        {
            _catalog = catalog;
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
        }
    }
}