using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashfall.Systems
{
    // Tiny lifecycle helper for GameInput. Put this on the Systems prefab.
    //
    // LateUpdate runs after every Update in the frame, so a button press queued during
    // OnPointerDown is visible to PlayerController, PlayerAttack and PlayerAbilities alike,
    // then cleared exactly once.
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
            // a held movement button is destroyed on scene change without ever firing
            // OnPointerUp, which would leave the player permanently walking
            GameInput.ResetAll();
        }

        void LateUpdate()
        {
            GameInput.ClearOneShots();
        }
    }
}