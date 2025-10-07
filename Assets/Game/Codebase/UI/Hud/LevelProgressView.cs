using Game.Core;
using Game.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using DG.Tweening;

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

        [Header("Animation")]
        [SerializeField] private float _fillTime = 0.2f;

        private IPlayerLevelService _playerLevel;
        private bool _subscribed;
        private Tween _fillTween;
        private bool _levelUpAnimating;

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

            KillFillTween();
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
            if (_levelUpAnimating)
                return;
            RefreshProgressOnly();
        }

        private void OnLevelChanged(PlayerLevel.LevelChangedEvent _)
        {
            if (_playerLevel == null)
                return;

            // Update level label immediately
            if (_levelText != null)
            {
                _levelText.text = _playerLevel.Level.ToString();
            }

            // Animate progress: fill to end, reset, then to remainder
            AnimateLevelUpTransition();
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
            if (_levelUpAnimating)
                return;

            if (_progressImage != null)
            {
                float target = _playerLevel.LevelProgress01;
                // Ensure valid range
                target = Mathf.Clamp01(target);
                // At max level, make sure it's visually full
                if (_playerLevel.IsMaxLevel)
                    target = 1f;

                if (_fillTime <= 0f)
                {
                    KillFillTween();
                    _progressImage.fillAmount = target;
                    return;
                }

                // Animate from current to target
                KillFillTween();
                _fillTween = _progressImage
                    .DOFillAmount(target, _fillTime)
                    .SetEase(Ease.Linear);
            }
        }

        private void AnimateLevelUpTransition()
        {
            if (_progressImage == null)
                return;

            float target = _playerLevel.LevelProgress01;
            target = Mathf.Clamp01(target);
            if (_playerLevel.IsMaxLevel)
                target = 1f;

            KillFillTween();

            if (_fillTime <= 0f)
            {
                // Instant: show just the new target
                _progressImage.fillAmount = target;
                return;
            }

            _levelUpAnimating = true;
            float half = _fillTime * 0.5f;

            var seq = DOTween.Sequence();
            seq.Append(_progressImage.DOFillAmount(1f, Mathf.Max(0.01f, half)).SetEase(Ease.Linear));
            seq.AppendCallback(() => { if (_progressImage != null) _progressImage.fillAmount = 0f; });
            seq.Append(_progressImage.DOFillAmount(target, Mathf.Max(0.01f, half)).SetEase(Ease.Linear));
            seq.OnComplete(() =>
            {
                _levelUpAnimating = false;
                _fillTween = null;
            });

            _fillTween = seq;
        }

        private void KillFillTween()
        {
            if (_fillTween != null && _fillTween.IsActive())
            {
                _fillTween.Kill();
                _fillTween = null;
            }
            _levelUpAnimating = false;
        }
    }
}
