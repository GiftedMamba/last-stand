using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    public class GameRunner : MonoBehaviour
    {
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private void Start() => RunGame();

        private void RunGame()
        {
            if (string.IsNullOrWhiteSpace(_gameplaySceneName))
            {
                GameLogger.LogError("Gameplay scene name is not set on GameRunner.");
                return;
            }

            GameLogger.Log($"Launching game. Loading scene: '{_gameplaySceneName}'");
            SceneManager.LoadSceneAsync(_gameplaySceneName, LoadSceneMode.Single);
        }
    }
}
