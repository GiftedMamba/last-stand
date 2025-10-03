using Game.UI;
using Game.UI.Screens;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.GameOver;
using Game.Configs;
using Game.Gameplay.Abilities;

namespace Game.Scopes
{
    public class GameplayScope : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField] private UIRoot _uiRoot;
        [SerializeField] private GlobalAbilityCatalog _globalAbilityCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            if (_uiRoot == null)
            {
                Game.Core.GameLogger.LogError("GameplayScope: UIRoot is not assigned. Screens will not be shown.");
            }
            else
            {
                // Register ScreenService with UIRoot
                builder.RegisterInstance<IScreenService>(new ScreenService(_uiRoot.Root));
            }

            // Global Abilities wiring
            if (_globalAbilityCatalog == null)
            {
                Game.Core.GameLogger.LogError("GameplayScope: GlobalAbilityCatalog is not assigned. Global abilities will still log but without config data.");
            }
            else
            {
                // Make catalog available for constructor injection
                builder.RegisterInstance(_globalAbilityCatalog);
            }
            
            // Register executor from scene hierarchy (needs EnemyRegistry reference)
            builder.RegisterComponentInHierarchy<GlobalAbilityExecutor>().As<IGlobalAbilityExecutor>();
            // Register ability service as a global singleton
            builder.Register<IGlobalAbilityService, GlobalAbilityService>(Lifetime.Singleton);

            // Ensure scene components with [Inject] receive dependencies
            builder.RegisterComponentInHierarchy<GameOverController>();
            builder.RegisterComponentInHierarchy<Game.UI.GlobalAbilityButton>();
        }
    }
}