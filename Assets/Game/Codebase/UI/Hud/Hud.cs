using UnityEngine;
using VContainer;
using Game.Gameplay.Waves;
using TMPro;

namespace Game.UI.Hud
{
    /// <summary>
    /// Simple HUD that displays the current wave countdown timer.
    /// Assign a TextMeshPro text field in the inspector.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _waveTimerText;

        private IWaveService _waveService;

        [Inject]
        public void Construct(IWaveService waveService)
        {
            _waveService = waveService;
        }

        private void Update()
        {
            if (_waveTimerText == null)
                return;

            float remaining = _waveService != null ? _waveService.CurrentWaveRemaining : 0f;
            _waveTimerText.text = FormatToMinutesSeconds(remaining);
        }

        private static string FormatToMinutesSeconds(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.RoundToInt(seconds);
            int minutes = total / 60;
            int secs = total % 60;
            return string.Format("{0:00}:{1:00}", minutes, secs);
        }
    }
}
