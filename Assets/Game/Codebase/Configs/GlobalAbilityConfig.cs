using Game.Gameplay.Abilities;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Configuration for a global ability.
    /// Contains the ability type, its cooldown, and active duration.
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

        public GlobalAbility Ability => _ability;
        public float Cooldown => _cooldown;
        public float Duration => _duration;
    }
}
