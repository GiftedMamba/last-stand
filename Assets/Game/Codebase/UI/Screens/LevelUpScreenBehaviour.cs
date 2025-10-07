using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    /// <summary>
    /// Runtime binder for LevelUpScreen. Wires the OK button to close the screen instance.
    /// Added automatically by ScreenService when LevelUpScreen is shown, so no prefab edits required.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelUpScreenBehaviour : MonoBehaviour
    {
        [SerializeField] private Button _okButton; // optional direct assignment via prefab; otherwise auto-find

        private void Awake()
        {
            if (_okButton == null)
            {
                // Try to find a button named commonly as "Ok", "OK", or "OkButton"; fallback to first Button in children
                foreach (var btn in GetComponentsInChildren<Button>(true))
                {
                    if (btn == null) continue;
                    string n = btn.name;
                    if (string.Equals(n, "OkButton", System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(n, "OK", System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(n, "Ok", System.StringComparison.OrdinalIgnoreCase))
                    {
                        _okButton = btn;
                        break;
                    }
                    if (_okButton == null)
                    {
                        _okButton = btn; // fallback to first encountered
                    }
                }
            }

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(Close);
            }
            else
            {
                GameLogger.LogWarning("LevelUpScreenBehaviour: OK Button not found on LevelUpScreen. Screen will not close on click.");
            }
        }

        private void OnDestroy()
        {
            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(Close);
            }
        }

        private void Close()
        {
            // Close with scale-down if available; otherwise destroy immediately
            var bounce = GetComponent<ScreenOpenBounce>();
            if (bounce != null)
            {
                bounce.PlayClose();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
