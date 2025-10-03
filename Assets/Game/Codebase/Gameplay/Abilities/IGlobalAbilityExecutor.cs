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
    }
}