using Game.Configs;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Spawns the Player prefab from PlayerConfig at the position/rotation of the GameObject this
    /// component is attached to. No tags or scene lookups required.
    /// Place this component on the desired spawn point and assign PlayerConfig in the inspector.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
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

            // Instantiate as a child of this transform, preserving intended world pose.
            var prefab = _playerConfig.PlayerPrefab;
            var instance = Instantiate(prefab, transform.position, transform.rotation, transform);
            instance.name = prefab.name; // keep clean name without (Clone)

            GameLogger.Log("PlayerSpawner: Player spawned at attached spawn point.");
        }
    }
}
