using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IMovable
    {
        // static so AudioManager can listen without needing a direct reference to the player
        public static event Action OnMovementSound; // fires for both jump and dash, same sound
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        Rigidbody2D rb;
        Animator animator;
        SpriteRenderer spriteRenderer;

        bool isGrounded;
        bool isDashing;
        bool isDead;
        float horizontalInput;
        int facingDirection = 1;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (isDead) return; // dont let input do anything once dead

            // just reading input here, actual movement happens in fixedupdate
            horizontalInput = Input.GetAxisRaw("Horizontal");

            CheckGrounded();

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }

            FlipSprite();
            UpdateAnimator();
        }

        void FixedUpdate()
        {
            if (isDashing || isDead) return; // dash coroutine or death controls stuff right now
            Move(new Vector2(horizontalInput, 0));
        }

        public bool FacingRight => facingDirection == 1;

        public void PerformDash(float force, float duration)
        {
            if (isDashing) return;
            animator?.SetTrigger("Roll");
            OnMovementSound?.Invoke();
            StartCoroutine(DashRoutine(force, duration));
        }

        System.Collections.IEnumerator DashRoutine(float force, float duration)
        {
            isDashing = true;
            rb.linearVelocity = new Vector2(facingDirection * force, rb.linearVelocity.y);

            yield return new WaitForSeconds(duration);

            isDashing = false;
        }

        // IMovable implementation
        public void Move(Vector2 direction)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }

        void Jump()
        {
            animator?.SetTrigger("Jump");
            OnMovementSound?.Invoke();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        void CheckGrounded()
        {
            if (groundCheck == null) return;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        void FlipSprite()
        {
            if (horizontalInput > 0)
            {
                spriteRenderer.flipX = false;
                facingDirection = 1;
            }
            else if (horizontalInput < 0)
            {
                spriteRenderer.flipX = true;
                facingDirection = -1;
            }
        }

        void UpdateAnimator()
        {
            if (animator == null) return;

            animator.SetBool("Grounded", isGrounded);
            animator.SetFloat("AirSpeedY", rb.linearVelocity.y);

            // 1 = running, 0 = idle, matches the animator's AnimState int
            animator.SetInteger("AnimState", Mathf.Abs(horizontalInput) > 0.01f ? 1 : 0);
        }

        // called by PlayerHealth when hp hits 0
        public void SetDead()
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
        }
    }
}