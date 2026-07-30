using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IMovable
    {
        public static event Action OnMovementSound;
        public static event Action OnFootstepSound;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        [Header("Footsteps")]
        public float footstepInterval = 0.35f;

        Rigidbody2D rb;
        Animator animator;
        SpriteRenderer spriteRenderer;

        bool isGrounded;
        bool isDashing;
        bool isDead;
        float horizontalInput;
        float footstepTimer;
        int facingDirection = 1;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (isDead) return;

            horizontalInput = Input.GetAxisRaw("Horizontal");

            CheckGrounded();
            HandleFootsteps();

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }

            FlipSprite();
            UpdateAnimator();
        }

        void FixedUpdate()
        {
            if (isDashing || isDead) return;
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
        }

        public void SetDead()
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
        }
    }
}