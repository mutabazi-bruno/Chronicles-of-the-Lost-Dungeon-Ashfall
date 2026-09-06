using System;
using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IMovable
    {
        public static event Action OnMovementSound;
        public static event Action OnFootstepSound;
        public static event Action OnWallSlideDust;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        [Header("Wall Slide")]
        public float wallSlideSpeed = 2f;
        public float wallCheckDistance = 0.15f;

        [Header("Footsteps")]
        public float footstepInterval = 0.35f;

        Rigidbody2D rb;
        Animator animator;
        SpriteRenderer spriteRenderer;
        Collider2D col;

        bool isGrounded;
        bool isDashing;
        bool isDead;
        bool isWallSliding;
        float horizontalInput;
        float footstepTimer;
        int facingDirection = 1;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
        }

        void Update()
        {
            if (isDead) return;

            // was Input.GetAxisRaw - now goes through GameInput so touch and keyboard
            // both feed the same path
            horizontalInput = GameInput.Horizontal;

            CheckGrounded();
            HandleFootsteps();

            if (GameInput.JumpPressed && isGrounded)
            {
                Jump();
            }

            FlipSprite();
            UpdateAnimator();
        }

        void FixedUpdate()
        {
            if (isDashing || isDead) return;

            CheckWallSlide();
            Move(new Vector2(horizontalInput, 0));

            if (isWallSliding && rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
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

        void HandleFootsteps()
        {
            bool isMoving = Mathf.Abs(horizontalInput) > 0.01f;

            if (isGrounded && isMoving && !isDashing)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    OnFootstepSound?.Invoke();
                    footstepTimer = footstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }
        }

        void CheckGrounded()
        {
            if (groundCheck == null) return;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        void CheckWallSlide()
        {
            if (isGrounded || Mathf.Abs(horizontalInput) < 0.01f || Mathf.Sign(horizontalInput) != facingDirection)
            {
                isWallSliding = false;
                return;
            }

            Vector2 origin = col.bounds.center;
            Vector2 castSize = new Vector2(0.05f, col.bounds.size.y * 0.6f);
            float castDistance = col.bounds.extents.x + wallCheckDistance;

            RaycastHit2D wallHit = Physics2D.BoxCast(
                origin, castSize, 0f,
                new Vector2(facingDirection, 0f),
                castDistance, groundLayer);

            isWallSliding = wallHit.collider != null;
        }

        // Called via Animation Event from HeroKnight_WallSlide.anim
        void AE_SlideDust()
        {
            OnWallSlideDust?.Invoke();
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
            animator.SetInteger("AnimState", Mathf.Abs(horizontalInput) > 0.01f ? 1 : 0);
            animator.SetBool("WallSlide", isWallSliding);
        }

        public void SetDead()
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
        }
    }
}