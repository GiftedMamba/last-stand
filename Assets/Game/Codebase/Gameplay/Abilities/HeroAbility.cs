using System;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Data container describing a single hero ability.
    /// Extend with ability-specific parameters as needed (e.g., levels, values).
    /// </summary>
    [Serializable]
    public class HeroAbility
    {
        [SerializeField] private HeroAbilityType _type = HeroAbilityType.Unknown;

        public HeroAbilityType Type => _type;
    }
}
