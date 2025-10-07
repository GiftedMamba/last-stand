using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// Implement on a screen to expose its animation root container.
    /// </summary>
    public interface IScreenContainerProvider
    {
        Transform Container { get; }
    }
}