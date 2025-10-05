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
        [SerializeField] private EnemyConfigCatalog _enemyConfigCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            if (_uiRoot == null)
            {
                Game.Core.GameLogger.LogError("GameplayScope: UIRoot is not assigned. Screens will not be shown.");
            }
            else
            {
                builder.RegisterInstance<IScreenService>(new ScreenService(_uiRoot.Root));
            }

            if (_globalAbilityCatalog == null)
            {
                Game.Core.GameLogger.LogError("GameplayScope: GlobalAbilityCatalog is not assigned. Global abilities will still log but without config data.");
            }
            else
            {
                builder.RegisterInstance(_globalAbilityCatalog);
            }
            
            builder.RegisterComponentInHierarchy<GlobalAbilityExecutor>().As<IGlobalAbilityExecutor>();
            builder.Register<IGlobalAbilityService, GlobalAbilityService>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<CameraShake>();

            builder.RegisterComponentInHierarchy<GameOverController>();
            builder.RegisterComponentInHierarchy<Game.UI.GlobalAbilityButton>();

            // Level up service as entry point singleton
            builder.RegisterEntryPoint<LevelUpService>(Lifetime.Singleton);

            // Wave system
            if (_waveConfig != null)
            {
                builder.RegisterInstance(_waveConfig);
                builder
                    .RegisterEntryPoint<WaveService>(Lifetime.Singleton)
                    .As<IWaveService>()
                    .AsSelf();
            }
            else
            {
                Game.Core.GameLogger.LogWarning("GameplayScope: WaveConfig is not assigned. WaveService will not be registered.");
            }

            // Enemy Config Catalog / Provider
            if (_enemyConfigCatalog != null)
            {
                builder.RegisterInstance(_enemyConfigCatalog).As<IEnemyConfigProvider>();
            }
            else
            {
                Game.Core.GameLogger.LogError("GameplayScope: EnemyConfigCatalog is not assigned. Spawners will not be able to spawn enemies.");
            }

            builder.RegisterComponentInHierarchy<EnemySpawner>();
        }
    }
}
