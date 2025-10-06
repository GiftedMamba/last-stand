using System;
using System.Collections.Generic;
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
        [SerializeField] private List<HeroAbilityLevel> _levels = new();

        public HeroAbilityType Type => _type;
        public IReadOnlyList<HeroAbilityLevel> Levels => _levels;
    }
}
