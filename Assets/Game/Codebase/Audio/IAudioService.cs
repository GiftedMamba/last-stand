using UnityEngine;
using Ami.BroAudio;

namespace Game.Audio
{
    /// <summary>
    /// Abstraction over audio playback to keep game logic decoupled from specific audio middleware.
    /// Implemented using BroAudio as per project guidelines.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// Play a one-shot SFX at world position.
        /// </summary>
        void PlaySfx(SoundID sound, Vector3 position);

        /// <summary>
        /// Play a one-shot SFX following the given transform.
        /// </summary>
        void PlaySfx(SoundID sound, Transform followTarget);
    }
}
