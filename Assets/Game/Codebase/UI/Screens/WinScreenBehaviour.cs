using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Core;

namespace Game.UI.Screens
{
    /// <summary>
    /// Runtime binder for WinScreen. Holds references to star GameObjects and
    /// activates as many as the provided count. Extra stars are deactivated.
    /// Also exposes a Restart button to reload the gameplay scene, same as on the Game Over screen.
    /// Attach this to the WinScreen prefab and assign the stars list and Restart Button in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WinScreenBehaviour : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("Assign star GameObjects in the order you want them to appear (left to right).")]
        [SerializeField] private List<GameObject> _stars = new();
        [Tooltip("Optional. If assigned, clicking will restart the gameplay scene.")]
        [SerializeField] private Button _restartButton;

        [Header("Animation")]
        [Tooltip("Delay in seconds before the first star appears.")]
        [SerializeField, Min(0f)] private float _startDelay = 1f;
        [Tooltip("Delay in seconds between each star appearing.")]
        [SerializeField, Min(0f)] private float _betweenStarsDelay = 0.2f;

        private Coroutine _revealRoutine;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(Restart);
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(Restart);
        }

        /// <summary>
        /// Sets how many stars should be shown. Stars will be revealed one-by-one with delays
        /// configured in the inspector (start delay and between-stars delay).
        /// </summary>
        public void SetStarsCount(int count)
        {
            if (_stars == null || _stars.Count == 0)
                return;

            int toActivate = Mathf.Clamp(count, 0, _stars.Count);

            // Stop any previous reveal animation
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            // Ensure all stars start disabled before revealing
            for (int i = 0; i < _stars.Count; i++)
            {
                var go = _stars[i];
                if (go == null) continue;
                go.SetActive(false);
            }

            if (toActivate <= 0)
                return;

            _revealRoutine = StartCoroutine(RevealStars(toActivate));
        }

        private IEnumerator RevealStars(int count)
        {
            if (_startDelay > 0f)
                yield return new WaitForSecondsRealtime(_startDelay);

            for (int i = 0; i < count; i++)
            {
                var go = _stars[i];
                if (go != null)
                    go.SetActive(true);

                if (i < count - 1 && _betweenStarsDelay > 0f)
                    yield return new WaitForSecondsRealtime(_betweenStarsDelay);
            }

            _revealRoutine = null;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneNames.Gameplay, LoadSceneMode.Single);
        }
    }
}
