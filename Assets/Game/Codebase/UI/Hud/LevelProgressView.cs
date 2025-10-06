using Game.Core;
using Game.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Game.UI.Hud
{
    /// <summary>
    /// Displays the player's level and progress toward the next level.
    /// Updates whenever experience changes (and also when level changes for safety).
    /// Robust against early OnEnable before DI: subscribes once DI is available or resolves from parent LifetimeScope.
    /// </summary>
    [AddComponentMenu("Game/UI/HUD/Level Progress View")]
    public sealed class LevelProgressView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _progressImage;

        private IPlayerLevelService _playerLevel;
        private bool _subscribed;

        [Inject]
        public void Construct(IPlayerLevelService playerLevel)
        {
            _playerLevel = playerLevel;
            TrySubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && _playerLevel != null)
            {
                _playerLevel.ExperienceChanged -= OnExperienceChanged;
                _playerLevel.LevelChanged -= OnLevelChanged;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed)
                return;

            if (_playerLevel == null)
            {
                // Defer until DI provides the service strictly via VContainer injection as per guidelines
                return;
            }

            _playerLevel.ExperienceChanged += OnExperienceChanged;
            _playerLevel.LevelChanged += OnLevelChanged;
            _subscribed = true;
            RefreshAll();
        }

        private void OnExperienceChanged(PlayerLevel.ExperienceChangedEvent _)
        {
            RefreshProgressOnly();
        }

        private void OnLevelChanged(PlayerLevel.LevelChangedEvent _)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_playerLevel == null)
                return;

            // Display current level (no +1)
            if (_levelText != null)
            {
                int displayLevel = _playerLevel.Level;
                _levelText.text = displayLevel.ToString();
            }

            RefreshProgressOnly();
        }

        private void RefreshProgressOnly()
        {
            if (_playerLevel == null)
                return;

            if (_progressImage != null)
            {
                float fill = _playerLevel.LevelProgress01;
                // Ensure valid range
                fill = Mathf.Clamp01(fill);
                // At max level, make sure it's visually full
                if (_playerLevel.IsMaxLevel)
                    fill = 1f;

                _progressImage.fillAmount = fill;
            }
        }
    }
}
