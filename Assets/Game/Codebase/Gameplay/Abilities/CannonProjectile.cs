using System;
using UnityEngine;
using Game.Gameplay.Enemies;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Simple parabolic projectile that travels from start to target over time and invokes an impact callback.
    /// Visual is optional; by default a small sphere is created as a child for visibility.
    /// Explodes when its arc crosses the ground, not when it hits mid-air objects.
    /// </summary>
    [AddComponentMenu("Game/Gameplay/Abilities/Cannon Projectile")]
    public class CannonProjectile : MonoBehaviour
    {
        private Vector3 _start;
        private Vector3 _target;
        private float _arcHeight;
        private float _duration;
        private float _elapsed;
        private Action<Vector3> _onImpact;
        private Transform _visual;
        private LayerMask _groundMask;

        /// <summary>
        /// Initialize the projectile.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="target">Target (impact) position.</param>
        /// <param name="arcHeight">Additional height at the peak of the arc.</param>
        /// <param name="duration">Flight time in seconds.</param>
        /// <param name="onImpact">Callback invoked at impact position.</param>
        /// <param name="groundMask">Mask used to detect ground collision during flight.</param>
        public void Init(Vector3 start, Vector3 target, float arcHeight, float duration, Action<Vector3> onImpact, LayerMask groundMask)
        {
            _start = start;
            _target = target;
            _arcHeight = Mathf.Max(0f, arcHeight);
            _duration = Mathf.Max(0.001f, duration);
            _onImpact = onImpact;
            _groundMask = groundMask;

            // If there is a Rigidbody attached (from a visual prefab), neutralize physics to avoid spawn snapping
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = start;
            }

            transform.position = _start;

            // Create a simple visual if none provided in the prefab
            if (transform.childCount == 0)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * 0.3f;
                var col = sphere.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                _visual = sphere.transform;
            }
            else
            {
                _visual = transform.GetChild(0);
            }
        }

        private void Update()
        {
            // Compute next position along the arc
            Vector3 prevPos = transform.position;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            Vector3 nextPos = Vector3.Lerp(_start, _target, t);
            float parabola = 4f * t * (1f - t); // 0..1..0
            nextPos.y = Mathf.Lerp(_start.y, _target.y, t) + parabola * _arcHeight;

            // Determine ground point under the horizontal position we are moving towards
            if (TryGetGroundAt(nextPos, out var groundPoint))
            {
                // If the arc crosses the ground between prev and next, explode on ground
                bool wasAbove = prevPos.y > groundPoint.y;
                bool nowBelowOrAt = nextPos.y <= groundPoint.y + 0.001f;
                if (wasAbove && nowBelowOrAt)
                {
                    // Find approximate impact point along the arc segment and snap Y to ground
                    float denom = Mathf.Max(0.0001f, (prevPos.y - nextPos.y));
                    float alpha = Mathf.Clamp01((prevPos.y - groundPoint.y) / denom);
                    Vector3 impact = Vector3.Lerp(prevPos, nextPos, alpha);
                    impact.y = groundPoint.y;
                    transform.position = impact;
                    try { _onImpact?.Invoke(impact); } catch (Exception) { }
                    Destroy(gameObject);
                    return;
                }
            }

            // Advance to next position
            transform.position = nextPos;

            if (t >= 1f)
            {
                // Fallback: ensure we land on ground at the target XZ if available
                Vector3 final = _target;
                if (TryGetGroundAt(_target, out var groundAtTarget))
                {
                    final = groundAtTarget;
                }
                try { _onImpact?.Invoke(final); }
                catch (Exception) { /* swallow to avoid breaking gameplay loop */ }
                Destroy(gameObject);
            }
        }

        private bool TryGetGroundAt(Vector3 worldPos, out Vector3 groundPoint)
        {
            // Cast down from above and pick the nearest non-enemy collider hit, honoring the provided ground mask
            Vector3 origin = new Vector3(worldPos.x, worldPos.y + 100f, worldPos.z);

            bool FindGround(int layerMask, out Vector3 gp)
            {
                var hits = Physics.RaycastAll(origin, Vector3.down, 1000f, layerMask, QueryTriggerInteraction.Ignore);
                float bestDist = float.PositiveInfinity;
                Vector3 bestPoint = default;
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    var col = h.collider;
                    if (col == null) continue;
                    // Skip enemies so we only treat actual ground/static geometry as "ground"
                    if (col.GetComponentInParent<Enemy>() != null)
                        continue;
                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        bestPoint = h.point;
                    }
                }
                if (bestDist < float.PositiveInfinity)
                {
                    gp = bestPoint;
                    return true;
                }
                gp = default;
                return false;
            }

            // Try with configured mask first
            if (FindGround(_groundMask, out groundPoint))
                return true;

            // Fallback: try without mask to avoid misconfiguration issues
            if (FindGround(~0, out groundPoint))
                return true;

            groundPoint = default;
            return false;
        }
    }
}
