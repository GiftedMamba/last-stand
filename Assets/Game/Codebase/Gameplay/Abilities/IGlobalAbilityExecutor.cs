namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Executes global ability effects that require scene access (coroutines, registries, etc.).
    /// Kept separate from the pure service to avoid MonoBehaviour in the service and to respect layering.
    /// </summary>
    public interface IGlobalAbilityExecutor
    {
        /// <summary>
        /// Stops all enemies for the specified duration (in seconds) and immediately deals damage once.
        /// Consecutive calls extend the stop duration if the new end time is later.
        /// </summary>
        /// <param name="durationSeconds">Duration in seconds. Non-positive values are ignored for stun.</param>
        /// <param name="damageOnce">Damage to deal once at activation. If zero or negative, no damage is dealt.</param>
        /// <param name="isPercent">If true, damageOnce is interpreted as a percentage of enemy Max HP; otherwise flat.</param>
        void StunForSeconds(float durationSeconds, float damageOnce, bool isPercent);
        
        /// <summary>
        /// Applies Howl effect: while active, enemies take extra damage (percentage if isPercent is true).
        /// </summary>
        /// <param name="durationSeconds">How long the effect should last.</param>
        /// <param name="value">When isPercent is true: percentage bonus damage taken (e.g., 50 for +50%).</param>
        /// <param name="isPercent">Interpret value as percent when true. Non-percent currently ignored for Howl.</param>
        void ApplyHowl(float durationSeconds, float value, bool isPercent);
    }
}