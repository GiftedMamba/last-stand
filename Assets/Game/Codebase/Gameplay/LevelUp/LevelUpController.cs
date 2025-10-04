using Game.Core;
using Game.Core.Player;
using Game.UI.Screens;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.LevelUp
{
    /// <summary>
    /// Listens to player level changes and shows the LevelUpScreen when the player gains a level.
    /// The screen prefab must be placed at Resources/Screens/LevelUpScreen.
    /// </summary>
    public sealed class LevelUpController : MonoBehaviour
    {
        private IScreenService _screenService;
        private IPlayerLevelService _playerLevelService;

        [Inject]
        public void Construct(IScreenService screenService, IPlayerLevelService playerLevelService)
        {
            _screenService = screenService;
            _playerLevelService = playerLevelService;
        }

        private void OnEnable()
        {
            if (_playerLevelService != null)
            {
                _playerLevelService.LevelChanged += OnLevelChanged;
            }
            else
            {
                GameLogger.LogError("LevelUpController: IPlayerLevelService is not available. Level up screen won't be shown.");
            }
        }

        private void OnDisable()
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
                    GameLogger.LogError("LevelUpController: IScreenService is not available. Cannot show LevelUpScreen.");
                    return;
                }

                _screenService.Show("LevelUpScreen");
            }
        }
    }
}
