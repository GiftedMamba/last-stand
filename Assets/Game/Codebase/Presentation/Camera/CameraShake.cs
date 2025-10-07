using System.Collections;
using Game.Core;
using UnityEngine;

namespace Game.Presentation.Camera
{
    /// <summary>
    /// Simple camera shake component inspired by CFXR_Effect's CameraShake, but limited to camera transform only.
    /// Attach to a Camera (or any Transform you want to shake) and call StartShake/ShakeOnce.
    /// No static callbacks; safe to use multiple instances as it only affects the assigned target.
    /// </summary>
    [AddComponentMenu("Game/Presentation/Camera/Camera Shake")]
    public class CameraShake : MonoBehaviour
    {
        public enum ShakeSpace
        {
            Screen,
            World
        }

        public enum ShakePreset
        {
            Unknown = 0,
            Weak = 1,
            Middle = 2,
            Heavy = 3
        }

        [Header("Target")]
        [Tooltip("If true, will try to use Camera.main transform when starting.")]
        [SerializeField] private bool _useMainCamera = true;
        [Tooltip("Explicit target Transform to shake. If null and UseMainCamera is true, will try Camera.main.")]
        [SerializeField] private Transform _target;

        [Header("Shake Settings (Baseline: Middle)")] 
        [SerializeField, Min(0f)] private float _delay = 0f;
        [SerializeField, Min(0f)] private float _duration = 0.4f;
        [Tooltip("Strength of the shake along each axis.")]
        [SerializeField] private Vector3 _strength = new Vector3(0.1f, 0.1f, 0f);
        [Tooltip("Falloff curve over normalized time (0..1).")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0, 1, 1, 0);
        [SerializeField] private ShakeSpace _space = ShakeSpace.Screen;
        [Tooltip("Random noise frequency. Higher values = more jitter per second.")]
        [SerializeField, Min(0f)] private float _frequency = 35f;

        private Coroutine _shakeRoutine;
        private Vector3 _originalLocalPos;
        private bool _cachedOriginal;
        private float _shakeEndTime;

        [Header("Preset Multipliers")]
        [Tooltip("Weak preset multipliers applied to baseline (Middle) values.")]
        [SerializeField, Min(0f)] private float _weakDurationMul = 0.75f;
        [SerializeField, Min(0f)] private float _weakStrengthMul = 0.5f;
        [SerializeField, Min(0f)] private float _weakFrequencyMul = 0.85f;

        [Tooltip("Heavy preset multipliers applied to baseline (Middle) values.")]
        [SerializeField, Min(0f)] private float _heavyDurationMul = 1.25f;
        [SerializeField, Min(0f)] private float _heavyStrengthMul = 1.8f;
        [SerializeField, Min(0f)] private float _heavyFrequencyMul = 1.15f;

        // Returns parameters for the given preset based on the baseline (serialized) settings treated as Middle.
        private void GetPresetParams(ShakePreset preset, out float duration, out Vector3 strength, out float frequency, out AnimationCurve curve, out ShakeSpace space)
        {
            // Baseline = Middle (values from serialized fields)
            duration = Mathf.Max(0.01f, _duration);
            strength = _strength;
            frequency = Mathf.Max(0f, _frequency);
            curve = _curve;
            space = _space;

            switch (preset)
            {
                case ShakePreset.Unknown:
                case ShakePreset.Middle:
                    // keep baseline
                    break;
                case ShakePreset.Weak:
                    duration *= _weakDurationMul;
                    strength *= _weakStrengthMul;
                    frequency *= _weakFrequencyMul;
                    break;
                case ShakePreset.Heavy:
                    duration *= _heavyDurationMul;
                    strength *= _heavyStrengthMul;
                    frequency *= _heavyFrequencyMul;
                    break;
            }
        }

        /// <summary>
        /// Starts shaking using the Middle preset (default). This preserves current usages.
        /// </summary>
        public void StartShake()
        {
            StartShake(ShakePreset.Middle);
        }

        /// <summary>
        /// Starts shaking with one of the predefined presets. If already shaking, extends the shake to cover the new duration.
        /// </summary>
        public void StartShake(ShakePreset preset)
        {
            var t = ResolveTarget();
            if (t == null)
            {
                GameLogger.LogWarning("CameraShake: No target to shake. Assign a target or enable UseMainCamera.");
                return;
            }

            GetPresetParams(preset, out float duration, out Vector3 strength, out float frequency, out AnimationCurve curve, out ShakeSpace space);

            float end = Time.time + Mathf.Max(0f, _delay) + Mathf.Max(0.01f, duration);
            if (_shakeRoutine == null)
            {
                _shakeEndTime = end;
                _shakeRoutine = StartCoroutine(ShakeRoutine(t, _delay, duration, strength, curve, space, frequency));
            }
            else
            {
                // extend current shake
                if (end > _shakeEndTime)
                    _shakeEndTime = end;
            }
        }

        /// <summary>
        /// Starts a one-off shake with custom parameters. If already shaking, extends duration if longer.
        /// </summary>
        public void ShakeOnce(float duration, Vector3 strength, float delay = 0f, float frequency = 35f, ShakeSpace space = ShakeSpace.Screen, AnimationCurve curve = null)
        {
            _duration = Mathf.Max(0.01f, duration);
            _strength = strength;
            _delay = Mathf.Max(0f, delay);
            _frequency = Mathf.Max(0f, frequency);
            _space = space;
            if (curve != null) _curve = curve;
            StartShake();
        }

        /// <summary>
        /// Stops the current shake immediately and restores the original local position.
        /// </summary>
        public void StopShake()
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeRoutine = null;
            }
            RestoreOriginal();
        }

        private Transform ResolveTarget()
        {
            if (_target == null && _useMainCamera)
            {
                if (UnityEngine.Camera.main != null)
                    _target = UnityEngine.Camera.main.transform;
            }
            return _target != null ? _target : transform;
        }

        private IEnumerator ShakeRoutine(Transform target, float delay, float duration, Vector3 strength, AnimationCurve curve, ShakeSpace space, float frequency)
        {
            if (!_cachedOriginal)
            {
                _originalLocalPos = target.localPosition;
                _cachedOriginal = true;
            }

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            var seed = Random.Range(-1000f, 1000f);

            while (true)
            {
                // Extend until the externally tracked end time is reached (supports repeated calls)
                float remaining = _shakeEndTime - Time.time;
                if (remaining <= 0f)
                    break;

                float dt = Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
                float falloff = curve != null ? Mathf.Max(0f, curve.Evaluate(t)) : 1f;

                // Generate pseudo-random offset using Perlin noise
                float time = Time.time * Mathf.Max(0.0001f, frequency);
                float nx = (Mathf.PerlinNoise(seed + time, 0.0f) - 0.5f) * 2f;
                float ny = (Mathf.PerlinNoise(0.0f, seed + time) - 0.5f) * 2f;
                float nz = (Mathf.PerlinNoise(seed - time, seed + time) - 0.5f) * 2f;

                Vector3 offset = new Vector3(nx * strength.x, ny * strength.y, nz * strength.z) * falloff;

                ApplyOffset(target, offset, space);
                yield return null;
            }

            RestoreOriginal();
            _shakeRoutine = null;
        }

        private void ApplyOffset(Transform target, Vector3 offset, ShakeSpace space)
        {
            if (space == ShakeSpace.Screen)
            {
                // In screen space, offset is applied in local coordinates
                target.localPosition = _originalLocalPos + offset;
            }
            else
            {
                // In world space, convert offset by target's orientation
                target.localPosition = _originalLocalPos; // reset local first to avoid drift
                target.position += target.TransformVector(offset);
            }
        }

        private void RestoreOriginal()
        {
            var t = ResolveTarget();
            if (t == null) return;
            t.localPosition = _originalLocalPos;
        }
    }
}
