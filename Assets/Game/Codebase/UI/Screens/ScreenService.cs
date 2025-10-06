using Game.Core;
using UnityEngine;

namespace Game.UI.Screens
{
    public interface IScreenService
    {
        GameObject Show(string prefabName);
    }

    /// <summary>
    /// Minimal screen service that loads screen prefabs from Resources/Screens
    /// and parents them under the provided UIRoot.
    /// </summary>
    public sealed class ScreenService : IScreenService
    {
        private readonly Transform _uiRoot;

        public ScreenService(Transform uiRoot)
        {
            _uiRoot = uiRoot;
        }

        public GameObject Show(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
            {
                GameLogger.LogError("ScreenService.Show called with empty prefabName");
                return null;
            }

            var prefab = Resources.Load<GameObject>($"Screens/{prefabName}");
            if (prefab == null)
            {
                GameLogger.LogError($"ScreenService: Could not find prefab 'Screens/{prefabName}' in Resources.");
                return null;
            }

            var instance = Object.Instantiate(prefab, _uiRoot, worldPositionStays: false);
            instance.name = prefab.name; // clean instance name

            // Ensure LevelUpScreen can be closed via its OK button without prefab changes
            if (prefabName == "LevelUpScreen")
            {
                if (instance.GetComponent<LevelUpScreenBehaviour>() == null)
                {
                    instance.AddComponent<LevelUpScreenBehaviour>();
                }
            }

            // Pause the game while modal screens are shown
            if (prefabName == "LevelUpScreen" || prefabName == "GameOverScreen" || prefabName == "WinScreen")
            {
                if (instance.GetComponent<ScreenPauseOnOpen>() == null)
                {
                    instance.AddComponent<ScreenPauseOnOpen>();
                }
            }

            return instance;
        }
    }
}
