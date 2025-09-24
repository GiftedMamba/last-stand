using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Marker/anchor for runtime UI screens. Put this on your root Canvas object you want screens to be parented under.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        public Transform Root => transform;
    }
}
