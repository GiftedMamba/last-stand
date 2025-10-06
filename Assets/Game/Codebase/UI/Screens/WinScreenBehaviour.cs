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
        /// Sets how many stars should be shown (activated). Values less than 0 are clamped to 0.
        /// Values greater than the number of available stars are clamped to the max.
        /// </summary>
        public void SetStarsCount(int count)
        {
            if (_stars == null || _stars.Count == 0)
                return;

            int toActivate = Mathf.Clamp(count, 0, _stars.Count);
            for (int i = 0; i < _stars.Count; i++)
            {
                var go = _stars[i];
                if (go == null) continue;
                go.SetActive(i < toActivate);
            }
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneNames.Gameplay, LoadSceneMode.Single);
        }
    }
}
