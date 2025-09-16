using Game.Configs;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Spawns the Player prefab from PlayerConfig under the scene object tagged "PlayerSpawnPoint".
    /// Place this component in the Gameplay scene and assign PlayerConfig in the inspector.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        private const string SpawnTag = "PlayerSpawnPoint";

        [SerializeField] private PlayerConfig _playerConfig;

        private void Start()
        {
            if (_playerConfig == null)
            {
                GameLogger.LogError("PlayerSpawner: PlayerConfig is not assigned.");
                return;
            }

            if (_playerConfig.PlayerPrefab == null)
            {
                GameLogger.LogError("PlayerSpawner: Player prefab is not set in PlayerConfig.");
                return;
            }

            var spawnPoint = GameObject.FindWithTag(SpawnTag);
            if (spawnPoint == null)
            {
                GameLogger.LogError($"PlayerSpawner: No GameObject found with tag '{SpawnTag}'. Please add one to the scene.");
                return;
            }

            // Instantiate as a child of the spawn point, preserving intended world pose.
            var prefab = _playerConfig.PlayerPrefab;
            var instance = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation, spawnPoint.transform);
            instance.name = prefab.name; // keep clean name without (Clone)

            GameLogger.Log("PlayerSpawner: Player spawned at spawn point.");
        }
    }
}
