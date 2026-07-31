using UnityEngine;

namespace Ashfall.Systems
{
    // Single point of truth for "what does the player want to do this frame".
    //
    // Gameplay scripts ask this instead of touching UnityEngine.Input directly, so the
    // keyboard/touch difference lives in exactly one file rather than being duplicated as
    // #if blocks inside PlayerController, PlayerAttack, PlayerAbilities and PlayerInteractor.
    // Adding a gamepad later means editing this class only.
    public static class GameInput
    {
        // --- values driven by on-screen buttons ---
        static float touchHorizontal;
        static bool touchJumpQueued;
        static bool touchAttackQueued;
        static bool touchDashQueued;
        static bool touchHeavyQueued;
        static bool touchInteractQueued;
        static bool touchPotionQueued;

        // -1, 0 or 1. Keyboard and touch are merged so the editor stays playable with a
        // keyboard even while the on-screen controls are visible.
        public static float Horizontal
        {
            get
            {
                float keyboard = 0f;

#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
                keyboard = Input.GetAxisRaw("Horizontal");
#endif

                float combined = keyboard + touchHorizontal;
                return Mathf.Clamp(combined, -1f, 1f);
            }
        }

        public static bool JumpPressed => touchJumpQueued || KeyboardJump();
        public static bool AttackPressed => touchAttackQueued || KeyboardAttack();
        public static bool DashPressed => touchDashQueued || KeyboardDash();
        public static bool HeavyStrikePressed => touchHeavyQueued || KeyboardHeavy();
        public static bool InteractPressed => touchInteractQueued || KeyboardInteract();
        public static bool UsePotionPressed => touchPotionQueued || KeyboardPotion();

        static bool KeyboardJump()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetButtonDown("Jump");
#else
            return false;
#endif
        }

        static bool KeyboardAttack()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetButtonDown("Fire1");
#else
            return false;
#endif
        }

        static bool KeyboardDash()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetKeyDown(KeyCode.LeftShift);
#else
            return false;
#endif
        }

        static bool KeyboardHeavy()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetButtonDown("Fire2");
#else
            return false;
#endif
        }

        static bool KeyboardInteract()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        static bool KeyboardPotion()
        {
#if !UNITY_ANDROID && !UNITY_IOS || UNITY_EDITOR
            return Input.GetKeyDown(KeyCode.Q);
#else
            return false;
#endif
        }

        // --- called by the on-screen TouchButton components ---

        public static void SetHorizontal(float value) => touchHorizontal = value;

        public static void QueueJump() => touchJumpQueued = true;
        public static void QueueAttack() => touchAttackQueued = true;
        public static void QueueDash() => touchDashQueued = true;
        public static void QueueHeavyStrike() => touchHeavyQueued = true;
        public static void QueueInteract() => touchInteractQueued = true;
        public static void QueuePotion() => touchPotionQueued = true;

        // One-shot actions must survive exactly one frame so every Update() that checks them
        // sees the same value. InputRouter clears them in LateUpdate, after all Updates ran.
        public static void ClearOneShots()
        {
            touchJumpQueued = false;
            touchAttackQueued = false;
            touchDashQueued = false;
            touchHeavyQueued = false;
            touchInteractQueued = false;
            touchPotionQueued = false;
        }

        // scene changes shouldn't leave the player walking into a wall forever because a
        // button's OnPointerUp never fired
        public static void ResetAll()
        {
            touchHorizontal = 0f;
            ClearOneShots();
        }
    }
}