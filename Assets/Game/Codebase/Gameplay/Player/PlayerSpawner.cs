using Game.Configs;
using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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
        [SerializeField] private Game.Gameplay.Enemies.EnemyRegistry _enemyRegistry;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

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

            // Inject dependencies into the spawned hierarchy if resolver is available
            if (_resolver != null)
            {
                _resolver.InjectGameObject(instance);
            }

            // Try wire PlayerAttack
            var attack = instance.GetComponentInChildren<PlayerAttack>();
            if (attack != null)
            {
                attack.Init(_playerConfig, _enemyRegistry);
            }
            else
            {
                GameLogger.LogWarning("PlayerSpawner: Spawned player has no PlayerAttack component.");
            }

            GameLogger.Log("PlayerSpawner: Player spawned at attached spawn point.");
        }
    }
}
