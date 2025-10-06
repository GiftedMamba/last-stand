using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Game.Configs;

namespace Game.UI.Screens
{
    /// <summary>
    /// Button on LevelUpScreen that increases a specific hero ability level when clicked.
    /// Should be initialized at runtime via Init(selectedAbilityType, service).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIIncreaseHeroAbilityLevelButton : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _iconImage; // assign in Inspector to display icon from hero ability config

        private Button _button;
        private IHeroAbilityService _service;
        private HeroAbilityType _abilityType;
        private bool _initialized;
        private PlayerConfig _playerConfig;

        [Inject]
        public void Construct(PlayerConfig playerConfig)
        {
            _playerConfig = playerConfig;
        }

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
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Init(HeroAbilityType abilityType, IHeroAbilityService service)
        {
            _abilityType = abilityType;
            _service = service;
            _initialized = true;

            if (_label != null)
            {
                _label.text = abilityType.ToString();
            }

            // Apply icon from PlayerConfig if available
            if (_iconImage != null && _playerConfig != null)
            {
                var abilities = _playerConfig.Abilities;
                if (abilities != null)
                {
                    for (int i = 0; i < abilities.Count; i++)
                    {
                        var ab = abilities[i];
                        if (ab != null && ab.Type == abilityType && ab.Icon != null)
                        {
                            _iconImage.sprite = ab.Icon;
                            _iconImage.enabled = true;
                            _iconImage.preserveAspect = true;
                            break;
                        }
                    }
                }
            }
        }

        private void OnClicked()
        {
            if (!_initialized || _service == null)
                return;

            if (_service.CanIncrease(_abilityType))
            {
                _service.IncreaseLevel(_abilityType);

                // After a successful selection/upgrade, close the LevelUp screen
                var screen = GetComponentInParent<LevelUpScreenBehaviour>(true);
                if (screen != null)
                {
                    Destroy(screen.gameObject);
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (_label != null)
            {
                _label.alpha = interactable ? 1f : 0.5f;
            }
        }
    }
}
