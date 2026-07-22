using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IMovable
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        Rigidbody2D rb;
        bool isGrounded;
        float horizontalInput;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            // just reading input here, actual movement happens in fixedupdate
            horizontalInput = Input.GetAxisRaw("Horizontal");

            CheckGrounded();

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }

            FlipSprite();
        }

        void FixedUpdate()
        {
            Move(new Vector2(horizontalInput, 0));
        }

        // IMovable implementation
        public void Move(Vector2 direction)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }

        void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        void CheckGrounded()
        {
            if (groundCheck == null) return;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        void FlipSprite()
        {
            // just flip the whole transform based on move direction, simple and works
            if (horizontalInput > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (horizontalInput < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}