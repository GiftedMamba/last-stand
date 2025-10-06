using System;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    [AddComponentMenu("Game/Gameplay/Abilities/Cannon")]
    public class Cannon : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform _launchPoint;
        [Tooltip("Projectile prefab with CannonProjectile component at root.")]
        [SerializeField] private CannonProjectile _projectilePrefab;

        [Header("Trajectory")]
        [SerializeField, Min(0.1f)] private float _flightTime = 1.25f;
        [SerializeField, Min(0f)] private float _arcHeight = 6f;
        [SerializeField, Min(1f)] private float _forwardDistance = 15f;
        [SerializeField] private LayerMask _groundMask = ~0; // default to everything

        [Header("Fire Control")]
        [Tooltip("Initial delay before the very first shot (seconds).")]
        [SerializeField, Min(0f)] private float _startFireDelay = 0f;
        [Tooltip("Cooldown between consecutive shots (seconds).")]
        [SerializeField, Min(0f)] private float _fireCooldown = 0.75f;

        private float _nextReadyTime;

        public bool IsReady => (_launchPoint != null) && (_projectilePrefab != null) && Time.time >= _nextReadyTime;
        public Transform LaunchPoint => _launchPoint != null ? _launchPoint : transform;

        private void Awake()
        {
            // Ensure the first shot is delayed by _startFireDelay
            _nextReadyTime = Time.time + Mathf.Max(0f, _startFireDelay);
        }

        /// <summary>
        /// Resets the start fire delay back timer so the cannon cannot fire until StartFireDelay elapses again.
        /// Use when the Cannon ability is (re)triggered to enforce initial delay.
        /// </summary>
        public void ResetStartDelay()
        {
            _nextReadyTime = Time.time + Mathf.Max(0f, _startFireDelay);
        }

        /// <summary>
        /// Fires a projectile from LaunchPoint towards LaunchPoint's forward direction.
        /// Impact position is determined at a fixed forward distance and snapped to ground via raycast.
        /// </summary>
        public CannonProjectile Fire(Action<Vector3> onImpact)
        {
            // Enforce readiness so StartFireDelay and cooldown are always respected
            if (!IsReady)
                return null;

            var lp = LaunchPoint;
            Vector3 start = lp.position;
            Vector3 rawTarget = start + lp.forward * _forwardDistance;

            // Raycast to find ground height around the raw target
            Vector3 target = rawTarget;
            if (Physics.Raycast(rawTarget + Vector3.up * 50f, Vector3.down, out var hit, 200f, _groundMask, QueryTriggerInteraction.Ignore))
            {
                target = hit.point;
            }
            else
            {
                // Keep Y of launch point if ground not found
                target.y = start.y;
            }

            // Instantiate with explicit world position and rotation so Awake() of the projectile sees correct transform
            var proj = Instantiate(_projectilePrefab, start, lp.rotation);

            // Ensure projectile starts exactly at LaunchPoint: parent it and zero local transform
            var projTransform = proj.transform;
            projTransform.SetParent(lp, true);
            projTransform.localPosition = Vector3.zero;
            projTransform.localRotation = Quaternion.identity;

            proj.Init(start, target, _arcHeight, _flightTime, onImpact, _groundMask);

            // Set next ready time based on cooldown
            _nextReadyTime = Time.time + Mathf.Max(0f, _fireCooldown);
            return proj;
        }
    }
}
