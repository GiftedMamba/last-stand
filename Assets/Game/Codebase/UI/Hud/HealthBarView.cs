using Game.Gameplay.Health;
using UnityEngine;
using UnityEngine.UI;
using TNRD;

namespace Game.UI.Hud
{
    /// <summary>
    /// Binds a UI Image (filled) to any IHealth source (tower, enemy, etc.). Assign this script to your health bar prefab
    /// and hook up the Fill Image reference in the inspector. The image's fillAmount will decrease as HP goes down.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private SerializableInterface<IHealth> _healthSource; // Serializable interface ref; can assign any component implementing IHealth
        [SerializeField] private Image _fillImage;    // Image.type should be Filled (Horizontal/Vertical/Radial)

        private IHealth _health;

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
            if (_healthSource == null || _healthSource.Value == null)
            {
                // Find any component in parents that implements IHealth and store it into the serializable wrapper
                var candidates = GetComponentsInParent<MonoBehaviour>(true);
                foreach (var mb in candidates)
                {
                    if (mb is IHealth h)
                    {
                        _healthSource = new SerializableInterface<IHealth>(h);
                        break;
                    }
                }
            }

            _health = _healthSource != null ? _healthSource.Value : null;
        }

        private void OnEnable()
        {
            if (_health == null && _healthSource != null)
            {
                _health = _healthSource.Value;
            }

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

        public void Bind(IHealth health)
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
