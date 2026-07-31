using UnityEngine;

namespace Ashfall.UI
{
    // Put this on the parent object holding all the on-screen controls.
    // Mobile gets them, desktop and WebGL don't - unless you tick the editor override so
    // you can test the touch layout without making an Android build every time.
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
            // Application.isMobilePlatform is false for a desktop browser, so this keeps a
            // phone browser playable without cluttering the desktop web build
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