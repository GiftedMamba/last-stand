using Game.Core;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Simple input adapter (old Input system) that lets the player click/tap an enemy
    /// to set it as the manual target for PlayerAttack. Works with both mouse and touch.
    /// Requirements: Enemies are on layer "Enemy" and have a collider (Capsule) and Rigidbody.
    /// </summary>
    public sealed class PlayerTargetSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAttack _playerAttack;
        [SerializeField] private Camera _camera;

        [Header("Settings")]
        [Tooltip("Layer used for enemies. Defaults to the layer named 'Enemy'.")]
        [SerializeField] private LayerMask _enemyLayerMask;

        private const string EnemyLayerName = "Enemy";

        private void Awake()
        {
            if (_playerAttack == null)
                _playerAttack = GetComponentInChildren<PlayerAttack>();

            if (_camera == null)
                _camera = Camera.main;

            // If mask not set in inspector, try to default to 'Enemy' layer
            if (_enemyLayerMask.value == 0)
            {
                int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
                if (enemyLayer >= 0)
                {
                    _enemyLayerMask = 1 << enemyLayer;
                }
                else
                {
                    // fallback to everything (so at least it works if user didn't set layer properly)
                    _enemyLayerMask = Physics.DefaultRaycastLayers;
                }
            }
        }

        private void Update()
        {
            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectAt(Input.mousePosition);
            }

            // Touch (first finger)
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    TrySelectAt(t.position);
                }
            }
        }

        private void TrySelectAt(Vector2 screenPos)
        {
            if (_playerAttack == null || _camera == null)
                return;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 1000f, _enemyLayerMask, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    _playerAttack.SetManualTarget(enemy);
                    return;
                }
            }

            // If clicked empty space, we can optionally clear manual target to resume auto-targeting
            // Uncomment if desired:
            // _playerAttack.ClearManualTarget();
        }
    }
}
