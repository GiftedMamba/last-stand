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

        public bool IsReady => (_launchPoint != null) && (_projectilePrefab != null);
        public Transform LaunchPoint => _launchPoint != null ? _launchPoint : transform;

        /// <summary>
        /// Fires a projectile from LaunchPoint towards LaunchPoint's forward direction.
        /// Impact position is determined at a fixed forward distance and snapped to ground via raycast.
        /// </summary>
        public CannonProjectile Fire(Action<Vector3> onImpact)
        {
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

            var proj = Instantiate(_projectilePrefab, start, Quaternion.identity, transform);
            proj.Init(start, target, _arcHeight, _flightTime, onImpact);
            return proj;
        }
    }
}
