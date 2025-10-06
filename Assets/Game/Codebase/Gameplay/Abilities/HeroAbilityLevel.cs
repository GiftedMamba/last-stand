using System;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Represents a single level entry for a hero ability.
    /// Value meaning depends on ability type (e.g., bonus damage multiplier, extra projectiles, etc.).
    /// </summary>
    [Serializable]
    public class HeroAbilityLevel
    {
        [SerializeField] private float _value = 0f;

        public float Value => _value;
    }
}