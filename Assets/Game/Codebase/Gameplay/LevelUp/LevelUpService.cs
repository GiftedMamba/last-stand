using Game.Core;
using Game.Core.Player;
using Game.UI.Screens;
using VContainer.Unity;

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

        public LevelUpService(IScreenService screenService, IPlayerLevelService playerLevelService)
        {
            _screenService = screenService;
            _playerLevelService = playerLevelService;
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
                _screenService.Show("LevelUpScreen");
            }
        }
    }
}
