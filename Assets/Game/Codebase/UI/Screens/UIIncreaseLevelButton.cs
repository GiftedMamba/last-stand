using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private Button _button;
        private IGlobalAbilityService _service;
        private GlobalAbility _ability;
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

        public void Init(GlobalAbility ability, IGlobalAbilityService service)
        {
            _ability = ability;
            _service = service;
            _initialized = true;

            if (_label != null)
            {
                _label.text = ability.ToString();
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
