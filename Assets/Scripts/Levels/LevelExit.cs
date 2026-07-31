using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [Tooltip("When on, the level id is taken from the scene name at runtime. This is the " +
                 "safe default - hand-typed ids drift the moment a scene is duplicated, which " +
                 "is exactly how every level ended up reporting itself as Level1.")]
        public bool useSceneNameAsLevelId = true;

        [Tooltip("Only used when useSceneNameAsLevelId is off")]
        public string levelIdOverride;

        [Tooltip("Optional. Normally leave this empty - the level complete screen decides " +
                 "where to go next, so loading a scene here would skip straight past it.")]
        public string sceneToLoadAfter;

        bool triggered;

        public string LevelId => useSceneNameAsLevelId
            ? SceneManager.GetActiveScene().name
            : levelIdOverride;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player")) return;

            string levelId = LevelId;

            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[LevelExit] no level id resolved, completion ignored");
                return;
            }

            if (LevelManager.Instance == null)
            {
                Debug.LogError("[LevelExit] no LevelManager in the scene - is the Systems prefab missing?");
                return;
            }

            triggered = true;
            LevelManager.Instance.CompleteLevel(levelId);

            Debug.Log($"[LevelExit] {levelId} complete");

            if (!string.IsNullOrEmpty(sceneToLoadAfter))
                SceneManager.LoadScene(sceneToLoadAfter);
        }
    }
}