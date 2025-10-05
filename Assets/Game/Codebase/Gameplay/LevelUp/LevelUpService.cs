using Game.Core;
using Game.Core.Player;
using Game.UI.Screens;
using VContainer.Unity;
using Game.Configs;
using Game.Gameplay.Abilities;
using UnityEngine;

namespace Game.Gameplay.LevelUp
{
    /// <summary>
    /// Non-Mono service that listens to player level changes and shows the LevelUpScreen.
    /// Registered as an entry point to avoid requiring a scene component.
    /// </summary>
    public sealed class LevelUpService : IStartable, System.IDisposable
    {
        private readonly IScreenService _screenService;
        private readonly IPlayerLevelService _playerLevelService;
        private readonly GlobalAbilityCatalog _catalog;
        private readonly IGlobalAbilityService _abilityService;

        public LevelUpService(IScreenService screenService, IPlayerLevelService playerLevelService, GlobalAbilityCatalog catalog, IGlobalAbilityService abilityService)
        {
            _screenService = screenService;
            _playerLevelService = playerLevelService;
            _catalog = catalog;
            _abilityService = abilityService;
        }

        public void Start()
        {
            if (_playerLevelService == null)
            {
                GameLogger.LogError("LevelUpService: IPlayerLevelService is not available.");
                return;
            }
            _playerLevelService.LevelChanged += OnLevelChanged;
        }

        public void Dispose()
        {
            if (_playerLevelService != null)
            {
                _playerLevelService.LevelChanged -= OnLevelChanged;
            }
        }

        private void OnLevelChanged(PlayerLevel.LevelChangedEvent evt)
        {
            if (evt.LevelsGained > 0)
            {
                if (_screenService == null)
                {
                    GameLogger.LogError("LevelUpService: IScreenService is not available. Cannot show LevelUpScreen.");
                    return;
                }
                var instance = _screenService.Show("LevelUpScreen");
                if (instance == null)
                    return;

                // Choose a random ability from available catalog entries
                var configs = _catalog != null ? _catalog.Configs : null;
                GlobalAbility chosen = Game.Gameplay.Abilities.GlobalAbility.Stun;
                if (configs != null && configs.Count > 0)
                {
                    int idx = Random.Range(0, configs.Count);
                    var cfg = configs[idx];
                    if (cfg != null)
                        chosen = cfg.Ability;
                }

                // Find or attach the increase-level button and initialize it
                var inc = instance.GetComponentInChildren<UIIncreaseLevelButton>(true);
                if (inc == null)
                {
                    // Try to find a Button in children to attach the component to
                    var anyButton = instance.GetComponentInChildren<UnityEngine.UI.Button>(true);
                    if (anyButton != null)
                    {
                        inc = anyButton.gameObject.AddComponent<UIIncreaseLevelButton>();
                    }
                    else
                    {
                        // As a last resort, attach to the root (will add a Button in Awake if missing)
                        inc = instance.AddComponent<UIIncreaseLevelButton>();
                    }
                }

                if (inc != null)
                {
                    inc.Init(chosen, _abilityService);
                }
            }
        }
    }
}
