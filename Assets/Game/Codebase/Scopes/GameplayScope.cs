using Game.UI;
using Game.UI.Screens;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.GameOver;

namespace Game.Scopes
{
    public class GameplayScope : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField] private UIRoot _uiRoot;

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

            // Ensure scene components with [Inject] receive dependencies
            builder.RegisterComponentInHierarchy<GameOverController>();
        }
    }
}