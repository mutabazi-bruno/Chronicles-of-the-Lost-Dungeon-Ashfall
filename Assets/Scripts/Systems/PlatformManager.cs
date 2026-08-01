using UnityEngine;

namespace Ashfall.Systems
{
    // Self-initializing platform-specific settings (WebGL, Mobile, Desktop).
    public static class PlatformManager
    {
        public static bool IsMobile { get; private set; }
        public static bool IsWebGL { get; private set; }
        public static bool IsDesktop { get; private set; }

        // other scripts (e.g. a future on-screen joystick, or PlayerController) can check
        // this instead of each having their own #if blocks scattered around.
        public static bool UseTouchInput => IsMobile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
#if UNITY_ANDROID || UNITY_IOS
            IsMobile = true;
            IsWebGL = false;
            IsDesktop = false;

            // mobile: keep the screen awake during play, cap framerate to save battery,
            // no cursor to worry about
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;

#elif UNITY_WEBGL
            IsMobile = false;
            IsWebGL = true;
            IsDesktop = false;

            // WebGL specific settings.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            IsMobile = false;
            IsWebGL = false;
            IsDesktop = true;

            // desktop: lock the cursor for a focused gameplay view, uncap framerate
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
            Cursor.lockState = CursorLockMode.Confined;

#else
            IsMobile = false;
            IsWebGL = false;
            IsDesktop = true;
#endif

        }
    }
}