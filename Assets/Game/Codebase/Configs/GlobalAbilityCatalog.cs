using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Abilities;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Catalog to hold references to all GlobalAbilityConfig assets.
    /// Link your ability configs here via Inspector. Provides simple lookup by ability enum.
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalAbilityCatalog", menuName = "Game/Configs/Global Ability Catalog", order = 3)]
    public class GlobalAbilityCatalog : ScriptableObject
    {
        [SerializeField]
        private List<GlobalAbilityConfig> _configs = new();

        public IReadOnlyList<GlobalAbilityConfig> Configs => _configs;

        /// <summary>
        /// Finds a config by ability type. Returns null if not found.
        /// </summary>
        public GlobalAbilityConfig Get(GlobalAbility ability)
        {
            // Linear search is fine for small lists; can optimize later if needed.
            return _configs.FirstOrDefault(c => c != null && c.Ability == ability);
        }
    }
}
