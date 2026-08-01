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
        Interact,
        UsePotion
    }

    // Use pointer events for continuous input holding.
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
                case TouchAction.UsePotion: GameInput.QueuePotion(); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Release movement holds.
            if (action == TouchAction.MoveLeft || action == TouchAction.MoveRight)
                GameInput.SetHorizontal(0f);
        }

        void OnDisable()
        {
            // Ensure movement clears when dragging off the button.
            if (action == TouchAction.MoveLeft || action == TouchAction.MoveRight)
                GameInput.SetHorizontal(0f);
        }
    }
}