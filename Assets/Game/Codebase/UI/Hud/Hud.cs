using Game.Core;
using UnityEngine;
using VContainer;
using Game.Gameplay.Waves;
using TMPro;

namespace Game.UI.Hud
{
    /// <summary>
    /// Simple HUD that displays the current wave countdown timer and wave number.
    /// Assign TextMeshPro text fields in the inspector.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _waveTimerText;
        [SerializeField] private TMP_Text _waveNumberText;

        private IWaveService _waveService;

        [Inject]
        public void Construct(IWaveService waveService)
        {
            _waveService = waveService;
        }

        private void Update()
        {
            // Update timer text
            if (_waveTimerText != null)
            {
                float remaining = _waveService != null ? _waveService.CurrentWaveRemaining : 0f;
                _waveTimerText.text = FormatToMinutesSeconds(remaining);
            }

            // Update wave number text
            if (_waveNumberText != null)
            {
                int waveNumber = _waveService != null ? _waveService.CurrentWaveNumber : 0;
                int totalWaves = _waveService != null ? _waveService.TotalWaves : 0;
                _waveNumberText.text = (waveNumber > 0 && totalWaves > 0) ? ($"{waveNumber}/{totalWaves}") : "";
            }
        }

        private static string FormatToMinutesSeconds(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.RoundToInt(seconds);
            return total.ToString();
        }
    }
}
