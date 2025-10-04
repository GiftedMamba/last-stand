namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Executes global ability effects that require scene access (coroutines, registries, etc.).
    /// Kept separate from the pure service to avoid MonoBehaviour in the service and to respect layering.
    /// </summary>
    public interface IGlobalAbilityExecutor
    {
        /// <summary>
        /// Stops all enemies for the specified duration (in seconds).
        /// Consecutive calls extend the stop duration if the new end time is later.
        /// </summary>
        /// <param name="durationSeconds">Duration in seconds. Non-positive values are ignored.</param>
        void StunForSeconds(float durationSeconds);
        
        /// <summary>
        /// Applies Howl effect: while active, enemies take extra percentage damage.
        /// </summary>
        /// <param name="durationSeconds">How long the effect should last.</param>
        /// <param name="damagePercent">Percentage bonus damage taken (e.g., 50 for +50%).</param>
        void ApplyHowl(float durationSeconds, float damagePercent);
    }
}