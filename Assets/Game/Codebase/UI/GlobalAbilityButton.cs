using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.Abilities;
using Game.Configs;

namespace Game.UI
{
    /// <summary>
    /// Attach this to a Unity UI Button.
    /// Hook the Button.onClick to call OnClick from the Inspector.
    /// Triggers a global ability by enum ID via GlobalAbilityService and visualizes cooldown via an Image fill.
    /// Also disables the button while the cooldown is active so it cannot be clicked.
    /// </summary>
    [AddComponentMenu("Game/UI/Global Ability Button")]
    public class GlobalAbilityButton : MonoBehaviour
    {
        [Header("Ability Trigger")] 
        [Tooltip("Ability to trigger.")]
        [SerializeField] private GlobalAbility _ability = GlobalAbility.Stun;

        [Header("Cooldown Visual")]
        [Tooltip("UI Image used as cooldown tint. Image.type should be Filled.")]
        [SerializeField] private Image _cooldownTint;

        private IGlobalAbilityService _service;
        private GlobalAbilityCatalog _catalog;
        private Coroutine _cooldownRoutine;
        private Button _button;

        [Inject]
        public void Construct(IGlobalAbilityService service, GlobalAbilityCatalog catalog)
        {
            _service = service;
            _catalog = catalog;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            // Ensure default state is no cooldown tint visible
            if (_cooldownTint != null)
                _cooldownTint.fillAmount = 0f;

            if (_button != null && _cooldownRoutine == null)
                _button.interactable = true;
        }

        /// <summary>
        /// Invoke from a UI Button OnClick() event.
        /// </summary>
        public void OnClick()
        {
            // Ignore clicks during cooldown
            if (_cooldownRoutine != null)
                return;

            _service?.Trigger(_ability);
            StartCooldownVisual();
        }

        private void StartCooldownVisual()
        {
            // Prefer level-aware cooldown from service; fallback to catalog for safety
            float cooldown = 0f;
            if (_service != null)
            {
                var level = _service.GetCurrentLevel(_ability);
                if (level != null)
                    cooldown = Mathf.Max(0f, level.Cooldown);
            }
            if (cooldown <= 0f && _catalog != null)
            {
                var config = _catalog.Get(_ability);
                if (config != null)
                {
                    var level0 = config.GetLevel(0);
                    if (level0 != null)
                        cooldown = Mathf.Max(0f, level0.Cooldown);
                }
            }

            // Disable button during cooldown
            if (_button != null)
                _button.interactable = false;

            // Immediately show full tint if provided
            if (_cooldownTint != null)
                _cooldownTint.fillAmount = 1f;

            if (cooldown <= 0f)
            {
                // No cooldown configured, clear immediately and re-enable button
                if (_cooldownTint != null)
                    _cooldownTint.fillAmount = 0f;
                if (_button != null)
                    _button.interactable = true;
                return;
            }

            if (_cooldownRoutine != null)
                StopCoroutine(_cooldownRoutine);

            _cooldownRoutine = StartCoroutine(CooldownRoutine(cooldown));
        }

        private System.Collections.IEnumerator CooldownRoutine(float cooldown)
        {
            float elapsed = 0f;
            while (elapsed < cooldown)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cooldown);
                if (_cooldownTint != null)
                    _cooldownTint.fillAmount = 1f - t;
                yield return null;
            }
            if (_cooldownTint != null)
                _cooldownTint.fillAmount = 0f;
            if (_button != null)
                _button.interactable = true;
            _cooldownRoutine = null;
        }
    }
}
