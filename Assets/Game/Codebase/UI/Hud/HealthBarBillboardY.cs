using UnityEngine;

namespace Game.UI.Hud
{
    /// <summary>
    /// Rotates the attached transform to face the active camera around Y axis only.
    /// Keeps X and Z rotation locked (no pitch/roll), so the health bar remains upright.
    /// Attach to world-space health bar root (e.g., the bar container transform).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarBillboardY : MonoBehaviour
    {
        [Header("Camera Binding")]
        [SerializeField] private Camera _camera; // optional; if null will use Camera.main each frame (safe for scene swaps)
        [Tooltip("If true, will face away from the camera (useful if your bar's forward faces backwards). Usually leave OFF.")]
        [SerializeField] private bool _invert;

        private void LateUpdate()
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return;

            // Direction towards camera, but flattened on XZ plane to lock pitch/roll
            Vector3 dir = cam.transform.position - transform.position;
            dir.y = 0f;

            // If we are exactly under/over camera on Y or same position, fall back to camera forward projected to XZ.
            if (dir.sqrMagnitude < 0.000001f)
            {
                dir = cam.transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.000001f)
                    return;
            }

            if (_invert)
                dir = -dir;

            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn if this object is on a Canvas set to Screen Space, as billboarding is for World Space canvases.
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                // Only log in editor to avoid runtime noise.
                Debug.LogWarning($"{nameof(HealthBarBillboardY)} should be used with a World Space Canvas for proper billboarding.", this);
            }
        }
#endif
    }
}
