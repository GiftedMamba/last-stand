using UnityEngine;

namespace Game.Core
{
    public class GameRunner : MonoBehaviour
    {
        private void Start() => RunGame();

        private void RunGame()
        {
            Debug.Log("Run game");
        }
    }
}
