using System;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    /// <summary>
    /// Simple parabolic projectile that travels from start to target over time and invokes an impact callback.
    /// Visual is optional; by default a small sphere is created as a child for visibility.
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

        /// <summary>
        /// Initialize the projectile.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="target">Target (impact) position.</param>
        /// <param name="arcHeight">Additional height at the peak of the arc.</param>
        /// <param name="duration">Flight time in seconds.</param>
        /// <param name="onImpact">Callback invoked at impact position.</param>
        public void Init(Vector3 start, Vector3 target, float arcHeight, float duration, Action<Vector3> onImpact)
        {
            _start = start;
            _target = target;
            _arcHeight = Mathf.Max(0f, arcHeight);
            _duration = Mathf.Max(0.001f, duration);
            _onImpact = onImpact;
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
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Horizontal lerp
            Vector3 pos = Vector3.Lerp(_start, _target, t);

            // Add arc using parabola: peak at t=0.5 with height _arcHeight
            float parabola = 4f * t * (1f - t); // 0..1..0
            pos.y = Mathf.Lerp(_start.y, _target.y, t) + parabola * _arcHeight;

            transform.position = pos;

            if (t >= 1f)
            {
                try { _onImpact?.Invoke(_target); }
                catch (Exception) { /* swallow to avoid breaking gameplay loop */ }
                Destroy(gameObject);
            }
        }
    }
}
