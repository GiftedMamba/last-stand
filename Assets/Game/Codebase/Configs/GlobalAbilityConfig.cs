using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Game.Gameplay.Abilities;
using Ami.BroAudio;

namespace Game.Configs
{
    /// <summary>
    /// Configuration for a global ability.
    /// Now contains a list of per-level entries (gameplay values only) and visuals at the root.
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalAbilityConfig", menuName = "Game/Configs/Global Ability", order = 2)]
    public class GlobalAbilityConfig : ScriptableObject
    {
        [Header("Ability")]
        [SerializeField] private GlobalAbility _ability;

        [Header("Levels")]
        [Tooltip("Per-level gameplay values (no visuals). Level 0 should be the first entry.")]
        [SerializeField] private List<GlobalAbilityLevelEntry> _levels = new();

        [Header("VFX")]
        [Tooltip("Optional VFX prefab to spawn on each affected target when the ability is applied. If null, no VFX will be spawned.")]
        [SerializeField] private GameObject _vfxPrefab;

        [Header("Audio")]
        [Tooltip("Sound played when this global ability is used.")]
        [SerializeField] private SoundID _useSfx;

        [Header("UI")] 
        [Tooltip("Icon to represent this global ability in UI elements (buttons, tooltips).")]
        [SerializeField] private Sprite _icon;

        public GlobalAbility Ability => _ability;
        public IReadOnlyList<GlobalAbilityLevelEntry> Levels => _levels;
        public GameObject VfxPrefab => _vfxPrefab;
        public SoundID UseSfx => _useSfx;
        public Sprite Icon => _icon;

        /// <summary>
        /// Returns level entry by index or null if out of range.
        /// </summary>
        public GlobalAbilityLevelEntry GetLevel(int index)
        {
            if (_levels == null || index < 0 || index >= _levels.Count)
                return null;
            return _levels[index];
        }
    }

    /// <summary>
    /// Serialized per-level gameplay values for a global ability.
    /// Mirrors runtime GlobalAbilityLevel but excludes visuals.
    /// </summary>
    [System.Serializable]
    public class GlobalAbilityLevelEntry
    {
        [Header("Timing")]
        [Tooltip("Cooldown in seconds between ability activations.")]
        [SerializeField, Min(0f)] private float _cooldown = 10f;

        [Tooltip("Active duration in seconds while the global ability effect persists.")]
        [SerializeField, Min(0f)] private float _duration = 5f;

        [Header("Payload")]
        [Tooltip("Generic damage value used by abilities. For Howl: percentage more damage taken when IsPercent is true. For Stun: damage dealt once on activation. For Cannon: impact damage per target inside splash.")]
        [FormerlySerializedAs("_damagePercent")]
        [SerializeField, Min(0f)] private float _damage = 0f;

        [Tooltip("Interpret Damage as percent (true) or as a flat value (false). For Howl, percent is expected; for Cannon, percent means percent of Max HP.")]
        [SerializeField] private bool _isPercent = true;

        [Tooltip("Splash radius (world units) for area-of-effect abilities like Cannon.")]
        [SerializeField, Min(0f)] private float _splash = 0f;

        public float Cooldown => _cooldown;
        public float Duration => _duration;
        public float Damage => _damage;
        public bool IsPercent => _isPercent;
        public float Splash => _splash;
    }
}
