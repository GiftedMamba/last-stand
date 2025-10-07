using DG.Tweening;
using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// Plays a bounce-out scale animation when the screen is enabled and a scale-down animation when closing.
    /// Uses unscaled time so it still animates while game is paused.
    /// </summary>
    public sealed class ScreenOpenBounce : MonoBehaviour
    {
        [Header("Open Animation")]
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private float _startScale = 0.75f;
        [SerializeField] private Ease _ease = Ease.OutBounce;
        [SerializeField] private bool _playOnEnable = true;

        [Header("Close Animation")]
        [SerializeField] private float _closeDuration = 0.2f;
        [SerializeField] private Ease _closeEase = Ease.InQuad;
        [SerializeField] private bool _destroyOnClose = true;

        private Transform _target;
        private Tween _tween;

        private void Awake()
        {
            ResolveTarget();
        }

        private void OnEnable()
        {
            if (_playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Play()
        {
            if (!ResolveTarget())
                return;

            KillTween();

            var rt = _target as RectTransform;
            if (rt == null)
                rt = _target.GetComponent<RectTransform>();

            var tr = (Transform)rt ?? _target;

            var start = Mathf.Max(0.01f, _startScale);

            tr.localScale = new Vector3(start, start, start);
            _tween = tr
                .DOScale(1f, Mathf.Max(0.01f, _duration))
                .SetEase(_ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Plays close scale-down animation and optionally destroys the GameObject at the end.
        /// </summary>
        public void PlayClose(bool destroyOnComplete = true, System.Action onComplete = null)
        {
            if (!ResolveTarget())
            {
                if (destroyOnComplete && _destroyOnClose)
                    Destroy(gameObject);
                onComplete?.Invoke();
                return;
            }

            KillTween();

            var rt = _target as RectTransform;
            if (rt == null)
                rt = _target.GetComponent<RectTransform>();

            var tr = (Transform)rt ?? _target;

            _tween = tr
                .DOScale(0f, Mathf.Max(0.01f, _closeDuration))
                .SetEase(_closeEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _tween = null;
                    onComplete?.Invoke();
                    if (destroyOnComplete && _destroyOnClose)
                    {
                        Destroy(gameObject);
                    }
                });
        }

        private bool ResolveTarget()
        {
            if (_target != null)
                return true;

            // Prefer explicit container providers on the same GameObject or children
            var provider = GetComponentInChildren<IScreenContainerProvider>(includeInactive: true);
            if (provider != null && provider.Container != null)
            {
                _target = provider.Container;
                return true;
            }

            // Fallback to own transform
            _target = transform;
            return _target != null;
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive())
            {
                _tween.Kill();
                _tween = null;
            }
        }
    }
}