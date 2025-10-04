using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// Simple helper that pauses the game (Time.timeScale = 0) when the screen is active
    /// and restores normal time scale (1) when the screen is destroyed/closed.
    /// Attach to modal screens like LevelUp or GameOver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenPauseOnOpen : MonoBehaviour
    {
        private bool _paused;

        private void OnEnable()
        {
            // Pause the game when this modal opens
            Time.timeScale = 0f;
            _paused = true;
        }

        private void OnDisable()
        {
            // Do not resume here because some flows briefly disable/enable during transitions.
            // Resume is handled in OnDestroy to ensure the modal is actually closed.
        }

        private void OnDestroy()
        {
            if (_paused)
            {
                // Resume normal time scale when the modal goes away
                Time.timeScale = 1f;
                _paused = false;
            }
        }
    }
}
