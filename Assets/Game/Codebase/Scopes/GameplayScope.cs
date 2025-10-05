using Game.UI;
using Game.UI.Screens;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.GameOver;
using Game.Configs;
using Game.Gameplay.Abilities;
using Game.Presentation.Camera;
using Game.Gameplay.Enemies;
using Game.Gameplay.LevelUp;
using Game.Gameplay.Waves;

namespace Game.Scopes
{
    public class GameplayScope : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField] private UIRoot _uiRoot;
        [SerializeField] private GlobalAbilityCatalog _globalAbilityCatalog;
        [SerializeField] private WaveConfig _waveConfig;

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

            // Register CameraShake from hierarchy so it can be injected where needed
            builder.RegisterComponentInHierarchy<CameraShake>();

            // Ensure scene components with [Inject] receive dependencies
            builder.RegisterComponentInHierarchy<GameOverController>();
            builder.RegisterComponentInHierarchy<Game.UI.GlobalAbilityButton>();

            // Level up screen trigger as an entry point service
            builder.RegisterEntryPoint<LevelUpService>();

            // Wave system
            if (_waveConfig != null)
            {
                builder.RegisterInstance(_waveConfig);
                builder.Register<WaveService>(Lifetime.Singleton).As<IWaveService>().AsSelf();
                builder.RegisterEntryPoint<WaveService>();
            }
            else
            {
                Game.Core.GameLogger.LogWarning("GameplayScope: WaveConfig is not assigned. WaveService will not be registered.");
            }

            // Allow EnemySpawner to receive IObjectResolver for DI instantiation of enemies
            builder.RegisterComponentInHierarchy<EnemySpawner>();
        }
    }
}