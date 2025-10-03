using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Gameplay.Abilities;

namespace Game.UI
{
    /// <summary>
    /// Attach this to a Unity UI Button.
    /// Hook the Button.onClick to call OnClick from the Inspector.
    /// Triggers a global ability by enum ID via GlobalAbilityService.
    /// </summary>
    [AddComponentMenu("Game/UI/Global Ability Button")]
    public class GlobalAbilityButton : MonoBehaviour
    {
        [Header("Ability Trigger")] 
        [Tooltip("Ability to trigger.")]
        [SerializeField] private GlobalAbility _ability = GlobalAbility.Stun;

        private IGlobalAbilityService _service;

        [Inject]
        public void Construct(IGlobalAbilityService service)
        {
            _service = service;
        }

        /// <summary>
        /// Invoke from a UI Button OnClick() event.
        /// </summary>
        public void OnClick()
        {
            _service?.Trigger(_ability);
        }
    }
}
