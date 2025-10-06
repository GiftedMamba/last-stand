using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// Runtime binder for WinScreen. Holds references to star GameObjects and
    /// activates as many as the provided count. Extra stars are deactivated.
    /// Attach this to the WinScreen prefab and assign the stars list in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WinScreenBehaviour : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("Assign star GameObjects in the order you want them to appear (left to right).")]
        [SerializeField] private List<GameObject> _stars = new();

        /// <summary>
        /// Sets how many stars should be shown (activated). Values less than 0 are clamped to 0.
        /// Values greater than the number of available stars are clamped to the max.
        /// </summary>
        public void SetStarsCount(int count)
        {
            if (_stars == null || _stars.Count == 0)
                return;

            int toActivate = Mathf.Clamp(count, 0, _stars.Count);
            for (int i = 0; i < _stars.Count; i++)
            {
                var go = _stars[i];
                if (go == null) continue;
                go.SetActive(i < toActivate);
            }
        }
    }
}
