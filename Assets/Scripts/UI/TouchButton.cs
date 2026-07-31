using UnityEngine;
using UnityEngine.EventSystems;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public enum TouchAction
    {
        MoveLeft,
        MoveRight,
        Jump,
        Attack,
        Dash,
        HeavyStrike,
        Interact
    }

    // Drop this on any UI Image/Button in the mobile control panel and pick an action.
    //
    // Uses IPointerDown/IPointerUp rather than Button.onClick because movement has to be
    // *held*, and onClick only fires on release - which would make the character twitch a
    // single frame per tap instead of walking.
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public TouchAction action;

        public void OnPointerDown(PointerEventData eventData)
        {
            switch (action)
            {
                case TouchAction.MoveLeft: GameInput.SetHorizontal(-1f); break;
                case TouchAction.MoveRight: GameInput.SetHorizontal(1f); break;
                case TouchAction.Jump: GameInput.QueueJump(); break;
                case TouchAction.Attack: GameInput.QueueAttack(); break;
                case TouchAction.Dash: GameInput.QueueDash(); break;
                case TouchAction.HeavyStrike: GameInput.QueueHeavyStrike(); break;
                case TouchAction.Interact: GameInput.QueueInteract(); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // only the movement buttons are held, the rest are one-shots that clear themselves
            if (action == TouchAction.MoveLeft || action == TouchAction.MoveRight)
                GameInput.SetHorizontal(0f);
        }

        void OnDisable()
        {
            // dragging a finger off the button, or the panel hiding mid-press, would
            // otherwise never release the movement
            if (action == TouchAction.MoveLeft || action == TouchAction.MoveRight)
                GameInput.SetHorizontal(0f);
        }
    }
}