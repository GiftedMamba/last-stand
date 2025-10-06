using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private Button _button;
        private IHeroAbilityService _service;
        private HeroAbilityType _abilityType;
        private bool _initialized;

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
        }

        private void OnClicked()
        {
            if (!_initialized || _service == null)
                return;

            if (_service.CanIncrease(_abilityType))
                _service.IncreaseLevel(_abilityType);
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
