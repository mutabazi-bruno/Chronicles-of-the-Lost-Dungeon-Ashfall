using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [Tooltip("must match this level's id, e.g. Level1")]
        public string levelId;

        [Tooltip("scene to load after completing, leave empty to just stay put for now")]
        public string sceneToLoadAfter;

        bool triggered;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player")) return;

            triggered = true;
            LevelManager.Instance.CompleteLevel(levelId);

            Debug.Log($"{levelId} complete!");

            if (!string.IsNullOrEmpty(sceneToLoadAfter))
                SceneManager.LoadScene(sceneToLoadAfter);
        }
    }
}