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
    [DefaultExecutionOrder(1000)]
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

        [Tooltip("Pulls the limit in slightly so a sprite's transparent border does not count " +
                 "as playable area.")]
        public float padding = 0f;

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

            if (root == null)
            {
                Debug.LogWarning($"[CameraBoundsClamp] no '{boundsObjectName}' in this scene, " +
                                 "camera will not be clamped");
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[CameraBoundsClamp] bounds object has no renderers");
                return;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            hasBounds = true;
        }

        void LateUpdate()
        {
            if (!hasBounds || cam == null || !cam.orthographic) return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 position = transform.position;

            if (clampHorizontally)
                position.x = ClampAxis(position.x, bounds.min.x, bounds.max.x, halfWidth, bounds.center.x);

            if (clampVertically)
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
