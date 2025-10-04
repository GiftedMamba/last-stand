using Game.Configs;
using Game.Core.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scopes
{
    public class ProjectScope : LifetimeScope
    {
        [Header("Configs")]
        [SerializeField] private PlayerConfig _playerConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // Register configs and services
            if (_playerConfig != null)
            {
                builder.RegisterInstance(_playerConfig);
            }

            builder.Register<IPlayerLevelService, PlayerLevelService>(Lifetime.Singleton);
        }
    }
}
