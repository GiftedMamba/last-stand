using Game.Core.Player;
using Game.Gameplay.Abilities;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// <summary>
    /// Resets run-scoped progression when Gameplay scene starts.
    /// Ensures player level and ability levels are reset after a defeat/restart.
    /// Registered as an EntryPoint in GameplayScope.
    /// </summary>
    public sealed class RunResetService : IStartable
    {
        private readonly IPlayerLevelService _playerLevel;
        private readonly IGlobalAbilityService _globalAbilityService;
        private readonly IHeroAbilityService _heroAbilityService;

        public RunResetService(IPlayerLevelService playerLevel,
            IGlobalAbilityService globalAbilityService,
            IHeroAbilityService heroAbilityService)
        {
            _playerLevel = playerLevel;
            _globalAbilityService = globalAbilityService;
            _heroAbilityService = heroAbilityService;
        }

        public void Start()
        {
            // Reset player progression
            _playerLevel?.Reset();

            // Reset all ability progressions
            _globalAbilityService?.Reset();
            _heroAbilityService?.Reset();
        }
    }
}
