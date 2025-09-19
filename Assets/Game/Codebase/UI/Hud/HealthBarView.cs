using Game.Gameplay.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    /// <summary>
    /// Binds a UI Image (filled) to a TowerHealth source. Assign this script to your health bar prefab
    /// and hook up the Fill Image reference in the inspector. The image's fillAmount will decrease as HP goes down.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TowerHealth _health; // optional; will auto-find in parents if null
        [SerializeField] private Image _fillImage;    // Image.type should be Filled (Horizontal/Vertical/Radial)

        [Header("Visuals")]
        [SerializeField] private Gradient _colorOverHealth; // optional; if set, color changes by percentage
        [SerializeField, Range(0f, 1f)] private float _minVisibleFill = 0.0001f; // avoid showing 0 until dead

        private void Reset()
        {
            // Try auto-bind image on same object
            if (_fillImage == null)
                _fillImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (_health == null)
                _health = GetComponentInParent<TowerHealth>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += OnHealthChanged;
                _health.OnDied += OnDied;
            }
            Sync();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnHealthChanged;
                _health.OnDied -= OnDied;
            }
        }

        public void Bind(TowerHealth health)
        {
            if (_health == health) return;

            if (enabled && _health != null)
            {
                _health.OnDamaged -= OnHealthChanged;
                _health.OnDied -= OnDied;
            }

            _health = health;

            if (enabled && _health != null)
            {
                _health.OnDamaged += OnHealthChanged;
                _health.OnDied += OnDied;
            }

            Sync();
        }

        private void OnHealthChanged(int amount, int current)
        {
            Sync();
        }

        private void OnDied()
        {
            Sync();
        }

        private void Sync()
        {
            if (_fillImage == null)
                return;

            float pct = 1f;
            if (_health != null && _health.MaxHp > 0)
                pct = Mathf.Clamp01(_health.CurrentHp / (float)_health.MaxHp);

            // keep a tiny fill when not dead to avoid sudden disappearance if desired
            float shown = (_health != null && _health.CurrentHp <= 0) ? 0f : Mathf.Max(_minVisibleFill, pct);
            _fillImage.fillAmount = shown;

            if (_colorOverHealth != null)
            {
                _fillImage.color = _colorOverHealth.Evaluate(pct);
            }
        }
    }
}
