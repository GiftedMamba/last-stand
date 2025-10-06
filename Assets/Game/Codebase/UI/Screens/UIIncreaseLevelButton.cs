using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Game.Configs;

namespace Game.UI.Screens
{
    /// <summary>
    /// Button on LevelUpScreen that increases a specific global ability level when clicked.
    /// It should be initialized at runtime via Init(selectedAbility, service).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIIncreaseLevelButton : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _iconImage; // assign in Inspector to display icon from config

        private Button _button;
        private IGlobalAbilityService _service;
        private GlobalAbility _ability;
        private bool _initialized;
        private GlobalAbilityCatalog _catalog;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }

            // Try resolve catalog from nearest LifetimeScope (DI) so we can fetch icons
            var scope = GetComponentInParent<LifetimeScope>();
            if (scope != null)
            {
                scope.Container.TryResolve(out _catalog);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Init(GlobalAbility ability, IGlobalAbilityService service)
        {
            _ability = ability;
            _service = service;
            _initialized = true;

            if (_label != null)
            {
                _label.text = ability.ToString();
            }

            // Apply icon from config if bindings and catalog are available
            if (_iconImage != null && _catalog != null)
            {
                var cfg = _catalog.Get(ability);
                if (cfg != null && cfg.Icon != null)
                {
                    _iconImage.sprite = cfg.Icon;
                    _iconImage.enabled = true;
                    _iconImage.preserveAspect = true;
                }
            }
        }

        private void OnClicked()
        {
            if (!_initialized || _service == null)
                return;

            _service.IncreaseLevel(_ability);

            // After a successful selection/upgrade, close the LevelUp screen
            var screen = GetComponentInParent<LevelUpScreenBehaviour>(true);
            if (screen != null)
            {
                Destroy(screen.gameObject);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }

            // Optional: gray out label if present
            if (_label != null)
            {
                _label.alpha = interactable ? 1f : 0.5f;
            }
        }
    }
}
