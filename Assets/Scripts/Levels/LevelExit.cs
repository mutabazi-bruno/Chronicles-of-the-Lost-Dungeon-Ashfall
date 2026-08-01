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

        [Header("Locked appearance")]
        [Tooltip("Optional visual shown while objectives are still outstanding")]
        public GameObject lockedVisual;
        public GameObject unlockedVisual;

        bool triggered;
        bool playerInside;

        public string LevelId => useSceneNameAsLevelId
            ? SceneManager.GetActiveScene().name
            : levelIdOverride;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.OnObjectivesChanged += HandleObjectivesChanged;

            RefreshVisuals();
        }

        void OnDestroy()
        {
            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.OnObjectivesChanged -= HandleObjectivesChanged;
        }

        void HandleObjectivesChanged()
        {
            RefreshVisuals();

            // finishing the last objective while already standing in the doorway should
            // complete the level, not require stepping out and back in
            if (playerInside) TryComplete();
        }

        void RefreshVisuals()
        {
            bool open = ObjectivesSatisfied();

            if (lockedVisual != null) lockedVisual.SetActive(!open);
            if (unlockedVisual != null) unlockedVisual.SetActive(open);
        }

        bool ObjectivesSatisfied()
        {
            // no manager in the scene means the level has no objectives - stay permissive
            return ObjectiveManager.Instance == null || ObjectiveManager.Instance.AllComplete;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            playerInside = true;
            TryComplete();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                playerInside = false;
        }

        void TryComplete()
        {
            if (triggered) return;

            if (!ObjectivesSatisfied())
            {
                return;
            }

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

            if (!string.IsNullOrEmpty(sceneToLoadAfter))
                SceneManager.LoadScene(sceneToLoadAfter);
        }
    }
}