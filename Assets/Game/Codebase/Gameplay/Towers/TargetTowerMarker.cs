using UnityEngine;

namespace Game.Gameplay.Towers
{
    /// <summary>
    /// Marker component designating this tower as the primary target for enemies.
    /// Attach this to the tower GameObject that should be focused by enemy AI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetTowerMarker : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private Color _gizmoColor = new(1f, 0.25f, 0.25f, 0.6f);
        [SerializeField] private float _gizmoRadius = 0.5f;

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
        }
#endif
    }
}
