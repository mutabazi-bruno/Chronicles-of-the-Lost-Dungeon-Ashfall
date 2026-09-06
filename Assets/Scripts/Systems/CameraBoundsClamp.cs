using UnityEngine;

namespace Ashfall.Systems
{
    // Keeps the camera inside the level art.
    //
    // Cinemachine follows the player wherever they go, including past the edge of the
    // background sprites. Everything beyond that edge is the camera's clear colour, which
    // is the grey band that shows up on the sides of a level. Rather than draw a confiner
    // polygon by hand in five scenes, this measures the background renderers and clamps
    // the camera to what they actually cover.
    //
    // Runs late on purpose: CinemachineBrain moves the camera in LateUpdate, so this has
    // to happen afterwards or it gets overwritten in the same frame.
    // As late as Unity allows. CinemachineBrain writes the camera transform in its own
    // LateUpdate, so anything that clamps it has to run after the brain, not before.
    [DefaultExecutionOrder(32000)]
    [RequireComponent(typeof(Camera))]
    public class CameraBoundsClamp : MonoBehaviour
    {
        [Tooltip("Object whose renderers define the playable area. Left empty, the scene is " +
                 "searched for an object with this name.")]
        public Transform boundsRoot;

        public string boundsObjectName = "Background";

        [Tooltip("Turn an axis off if a level is meant to scroll past the art on that axis.")]
        public bool clampHorizontally = true;
        public bool clampVertically = true;

        [Tooltip("Hold the camera at the vertical middle of the art instead of letting it " +
                 "drift. These levels are one ground line seen from the side, and the art is " +
                 "barely taller than the view, so any vertical movement risks showing the edge " +
                 "for the sake of travel nobody asked for.")]
        public bool lockVertical = true;

        [Tooltip("Pulls the limit in slightly so a sprite's transparent border does not count " +
                 "as playable area.")]
        public float padding = 0f;

        [Tooltip("Print what was measured on start. Turn this on when the camera is still " +
                 "showing background outside the art.")]
        public bool logBounds = true;

        Camera cam;
        Bounds bounds;
        bool hasBounds;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            ResolveBounds();
        }

        [ContextMenu("Recalculate Bounds")]
        public void ResolveBounds()
        {
            hasBounds = false;

            Transform root = boundsRoot;
            if (root == null && !string.IsNullOrEmpty(boundsObjectName))
            {
                var found = GameObject.Find(boundsObjectName);
                if (found != null) root = found.transform;
            }

            Renderer[] renderers;

            if (root != null)
            {
                renderers = root.GetComponentsInChildren<Renderer>();
            }
            else
            {
                // No object by that name. Rather than do nothing, fall back to every sprite
                // in the scene, which still beats letting the camera wander off the art.
                Debug.LogWarning($"[CameraBoundsClamp] no '{boundsObjectName}' found, " +
                                 "falling back to every SpriteRenderer in the scene");
                renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            }

            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning("[CameraBoundsClamp] nothing to measure, camera will not be clamped");
                return;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            hasBounds = true;

            if (logBounds)
            {
                float halfHeight = cam != null ? cam.orthographicSize : 0f;
                float halfWidth = cam != null ? halfHeight * cam.aspect : 0f;
                Debug.Log($"[CameraBoundsClamp] measured {renderers.Length} renderers, " +
                          $"bounds min {bounds.min} max {bounds.max}, " +
                          $"camera half extents {halfWidth} x {halfHeight}. " +
                          $"Horizontal room: {(bounds.size.x > halfWidth * 2 ? "yes" : "NO, art is narrower than the view")}. " +
                          $"Vertical room: {(bounds.size.y > halfHeight * 2 ? "yes" : "NO, art is shorter than the view")}.");
            }
        }

        // Draws what it actually measured, so a wrong result is visible in the Scene view
        // rather than something to reason about.
        void OnDrawGizmosSelected()
        {
            if (!hasBounds) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        void LateUpdate()
        {
            if (!hasBounds || cam == null || !cam.orthographic) return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 position = transform.position;

            if (clampHorizontally)
                position.x = ClampAxis(position.x, bounds.min.x, bounds.max.x, halfWidth, bounds.center.x);

            if (lockVertical)
                position.y = bounds.center.y;
            else if (clampVertically)
                position.y = ClampAxis(position.y, bounds.min.y, bounds.max.y, halfHeight, bounds.center.y);

            transform.position = position;
        }

        // When the art is narrower than the view there is no valid range to clamp into, so
        // centre on it instead and let the gap sit evenly on both sides.
        float ClampAxis(float value, float min, float max, float halfExtent, float centre)
        {
            float low = min + halfExtent + padding;
            float high = max - halfExtent - padding;

            if (low > high) return centre;
            return Mathf.Clamp(value, low, high);
        }
    }
}
