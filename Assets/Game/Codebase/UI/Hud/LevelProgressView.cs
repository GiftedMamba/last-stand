using Game.Core;
using Game.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using DG.Tweening;
using Game.Gameplay.Waves;

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
        [SerializeField] private TMP_Text _waveText;

        [Header("Animation")]
        [SerializeField] private float _fillTime = 0.2f;
        [SerializeField] private float _waveUpdateDelay = 0.15f;

        private IPlayerLevelService _playerLevel;
        private IWaveService _waveService;
        private bool _subscribed;
        private Tween _fillTween;
        private Tween _waveDelayTween;
        private bool _levelUpAnimating;

        [Inject]
        public void Construct(IPlayerLevelService playerLevel)
        {
            _playerLevel = playerLevel;
            TrySubscribe();
        }

        [Inject]
        public void ConstructWaves(IWaveService waveService)
        {
            _waveService = waveService;
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
            KillWaveDelay();
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

            // Only update wave text when the progress fill reaches full via animations.
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
                    if (target >= 0.999f)
                        UpdateWaveText();
                    return;
                }

                // Animate from current to target
                KillFillTween();
                _fillTween = _progressImage
                    .DOFillAmount(target, _fillTime)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (target >= 0.999f)
                            UpdateWaveText();
                    });
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
            seq.SetUpdate(true);
            seq.Append(_progressImage.DOFillAmount(1f, Mathf.Max(0.01f, half)).SetEase(Ease.Linear).SetUpdate(true));
            seq.AppendCallback(() =>
            {
                UpdateWaveText();
                if (_progressImage != null) _progressImage.fillAmount = 0f;
            });
            seq.Append(_progressImage.DOFillAmount(target, Mathf.Max(0.01f, half)).SetEase(Ease.Linear).SetUpdate(true));
            seq.OnComplete(() =>
            {
                _levelUpAnimating = false;
                _fillTween = null;
            });

            _fillTween = seq;
        }

        private void UpdateWaveText()
        {
            if (_waveText == null || _waveService == null)
                return;
            int waveNumber = _waveService.CurrentWaveNumber;
            int totalWaves = _waveService.TotalWaves;
            _waveText.text = (waveNumber > 0 && totalWaves > 0) ? ($"{waveNumber}/{totalWaves}") : string.Empty;
        }

        private void ScheduleWaveUpdate()
        {
            KillWaveDelay();
            float delay = _waveUpdateDelay < 0f ? 0f : _waveUpdateDelay;
            _waveDelayTween = DOVirtual.DelayedCall(delay, UpdateWaveText, ignoreTimeScale: true)
                .SetUpdate(true);
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

        private void KillWaveDelay()
        {
            if (_waveDelayTween != null && _waveDelayTween.IsActive())
            {
                _waveDelayTween.Kill();
                _waveDelayTween = null;
            }
        }
    }
}
