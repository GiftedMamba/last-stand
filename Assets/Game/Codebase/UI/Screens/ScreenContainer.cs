using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// Generic screen component that exposes an animation root container.
    /// Place on a screen prefab and assign the container RectTransform to animate.
    /// </summary>
    public sealed class ScreenContainer : MonoBehaviour, IScreenContainerProvider
    {
        [Header("Animation Root")]
        [SerializeField] private RectTransform _container;

        public Transform Container => _container != null ? _container : transform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_container == null)
            {
                // Try to find a child named "Container" by convention
                var t = transform.Find("Container");
                if (t is RectTransform rt)
                    _container = rt;
            }
        }
#endif
    }
}