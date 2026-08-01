using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashfall.Systems
{
    // Handle input routing.
    public class InputRouter : MonoBehaviour
    {
        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Handle edge cases where input is interrupted by scene loads.
            GameInput.ResetAll();
        }

        void LateUpdate()
        {
            GameInput.ClearOneShots();
        }
    }
}