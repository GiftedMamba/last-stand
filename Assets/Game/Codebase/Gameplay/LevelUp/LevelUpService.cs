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
        private readonly IHeroAbilityService _heroAbilityService;

        public LevelUpService(IScreenService screenService, IPlayerLevelService playerLevelService, GlobalAbilityCatalog catalog, IGlobalAbilityService abilityService, IHeroAbilityService heroAbilityService)
        {
            _screenService = screenService;
            _playerLevelService = playerLevelService;
            _catalog = catalog;
            _abilityService = abilityService;
            _heroAbilityService = heroAbilityService;
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

                // Choose a random GLOBAL ability from those that can still be upgraded
                var configs = _catalog != null ? _catalog.Configs : null;
                GlobalAbility chosen = Game.Gameplay.Abilities.GlobalAbility.Stun;
                bool hasUpgradeableGlobal = false;
                if (configs != null && configs.Count > 0 && _abilityService != null)
                {
                    var upgradable = new System.Collections.Generic.List<GlobalAbility>();
                    for (int i = 0; i < configs.Count; i++)
                    {
                        var cfg = configs[i];
                        if (cfg == null) continue;
                        var level = _abilityService.GetCurrentLevel(cfg.Ability);
                        int currentIndex = level != null ? level.LevelIndex : 0;
                        int maxIndex = (cfg.Levels == null || cfg.Levels.Count == 0) ? 0 : cfg.Levels.Count - 1;
                        if (currentIndex < maxIndex)
                        {
                            upgradable.Add(cfg.Ability);
                        }
                    }

                    if (upgradable.Count > 0)
                    {
                        int idx = Random.Range(0, upgradable.Count);
                        chosen = upgradable[idx];
                        hasUpgradeableGlobal = true;
                    }
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
                    if (!hasUpgradeableGlobal)
                    {
                        inc.SetInteractable(false);
                    }
                    else
                    {
                        inc.SetInteractable(true);
                    }
                }

                // Initialize HERO ability increase button if present on the screen
                var heroInc = instance.GetComponentInChildren<UIIncreaseHeroAbilityLevelButton>(true);
                if (heroInc != null)
                {
                    // Pick a hero ability that can still be upgraded. For now, iterate known enum values (except Unknown).
                    HeroAbilityType heroType = HeroAbilityType.Unknown;
                    bool hasUpgradeableHero = false;
                    if (_heroAbilityService == null)
                    {
                        GameLogger.LogError("LevelUpService: IHeroAbilityService is not available. Hero ability button will not function.");
                    }
                    else
                    {
                        // Simple enumeration over known types; extend when new abilities are added.
                        var candidates = new System.Collections.Generic.List<HeroAbilityType>
                        {
                            HeroAbilityType.SplitShot,
                            HeroAbilityType.Pierce
                        };
                        var upgradableHero = new System.Collections.Generic.List<HeroAbilityType>();
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            var t = candidates[i];
                            if (_heroAbilityService.CanIncrease(t))
                                upgradableHero.Add(t);
                        }
                        if (upgradableHero.Count > 0)
                        {
                            heroType = upgradableHero[Random.Range(0, upgradableHero.Count)];
                            hasUpgradeableHero = true;
                        }
                    }

                    // Initialize with the selected (or fallback Unknown) type
                    heroInc.Init(heroType == HeroAbilityType.Unknown ? HeroAbilityType.SplitShot : heroType, _heroAbilityService);
                    heroInc.SetInteractable(hasUpgradeableHero);
                }
            }
        }
    }
}
