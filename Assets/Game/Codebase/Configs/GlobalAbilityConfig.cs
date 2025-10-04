using Game.Gameplay.Abilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Configs
{
    /// <summary>
    /// Configuration for a global ability.
    /// Contains the ability type, its cooldown, active duration, and generic payload fields.
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalAbilityConfig", menuName = "Game/Configs/Global Ability", order = 2)]
    public class GlobalAbilityConfig : ScriptableObject
    {
        [Header("Ability")]
        [SerializeField] private GlobalAbility _ability;

        [Header("Timing")]
        [Tooltip("Cooldown in seconds between ability activations.")]
        [SerializeField, Min(0f)] private float _cooldown = 10f;

        [Tooltip("Active duration in seconds while the global ability effect persists.")]
        [SerializeField, Min(0f)] private float _duration = 5f;

        [Header("Payload")]
        [Tooltip("Generic damage value used by abilities. For Howl: percentage more damage taken when IsPercent is true. For Stun: damage dealt once on activation.")]
        [FormerlySerializedAs("_damagePercent")]
        [SerializeField, Min(0f)] private float _damage = 0f;
        [Tooltip("Interpret Damage as percent (true) or as a flat value (false). For Howl, percent is expected.")]
        [SerializeField] private bool _isPercent = true;

        public GlobalAbility Ability => _ability;
        public float Cooldown => _cooldown;
        public float Duration => _duration;
        public float Damage => _damage;
        public bool IsPercent => _isPercent;
    }
}
