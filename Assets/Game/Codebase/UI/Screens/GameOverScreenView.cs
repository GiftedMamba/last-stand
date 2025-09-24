using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    /// <summary>
    /// Presenter for Game Over screen. Expose Restart button and scene to load.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOverScreenView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Button _restartButton;

        [Header("Config")]
        [SerializeField] private string _gameplaySceneName = "Gameplay";

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

        public void Restart()
        {
            if (string.IsNullOrWhiteSpace(_gameplaySceneName))
                return;

            SceneManager.LoadScene(_gameplaySceneName, LoadSceneMode.Single);
        }
    }
}
