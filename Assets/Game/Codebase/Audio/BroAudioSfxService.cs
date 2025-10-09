using UnityEngine;
using Ami.BroAudio;

namespace Game.Audio
{
    /// <summary>
    /// BroAudio-backed implementation of IAudioService.
    /// </summary>
    public sealed class BroAudioSfxService : IAudioService
    {
        public void PlaySfx(SoundID sound, Vector3 position)
        {
            if (sound.ID <= 0) return; // invalid id safeguard
            BroAudio.Play(sound, position);
        }

        public void PlaySfx(SoundID sound, Transform followTarget)
        {
            if (sound.ID <= 0) return; // invalid id safeguard
            if (followTarget == null)
            {
                BroAudio.Play(sound);
            }
            else
            {
                BroAudio.Play(sound, followTarget);
            }
        }
    }
}
