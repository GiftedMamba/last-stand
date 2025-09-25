using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Helper component added to spawned enemies to register/unregister with an EnemyRegistry.
    /// Keeps scene free of static singletons per guidelines.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRegistryHandle : MonoBehaviour
    {
        [SerializeField] private EnemyRegistry _registry;
        [SerializeField] private Enemy _enemy;

        public void Init(EnemyRegistry registry, Enemy enemy)
        {
            _registry = registry;
            _enemy = enemy;
        }

        private void OnEnable()
        {
            if (_registry != null)
            {
                if (_enemy == null) _enemy = GetComponent<Enemy>();
                _registry.Add(_enemy);
            }
        }

        private void OnDisable()
        {
            if (_registry != null)
            {
                if (_enemy == null) _enemy = GetComponent<Enemy>();
                _registry.Remove(_enemy);
            }
        }
    }
}
