using UnityEngine;

namespace Ashfall.UI
{
    // Show touch controls on mobile or when editor override is active.
    public class TouchControlsVisibility : MonoBehaviour
    {
        [Tooltip("Show the on-screen controls in the editor too, for testing")]
        public bool showInEditor = true;

        [Tooltip("WebGL can run on a phone browser as well - tick to always show there")]
        public bool showOnWebGL = false;

        void Awake()
        {
            bool show;

#if UNITY_ANDROID || UNITY_IOS
            show = true;
#elif UNITY_WEBGL
            // Support mobile browsers in WebGL builds.
            show = showOnWebGL || Application.isMobilePlatform;
#else
            show = false;
#endif

#if UNITY_EDITOR
            show = showInEditor;
#endif

            gameObject.SetActive(show);
        }
    }
}