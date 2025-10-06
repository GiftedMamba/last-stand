using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Data container describing a single hero ability.
    /// Extend with ability-specific parameters as needed (e.g., levels, values).
    /// Note: current level is tracked at runtime (e.g., by PlayerAttack), not in this config.
    /// </summary>
    [Serializable]
    public class HeroAbility
    {
        [SerializeField] private HeroAbilityType _type = HeroAbilityType.Unknown;
        [SerializeField] private List<HeroAbilityLevel> _levels = new();
        
        [Header("UI")] 
        [Tooltip("Icon to represent this hero ability in UI elements (choice cards, tooltips).")]
        [SerializeField] private Sprite _icon;

        public HeroAbilityType Type => _type;
        public IReadOnlyList<HeroAbilityLevel> Levels => _levels;
        public Sprite Icon => _icon;
    }
}
