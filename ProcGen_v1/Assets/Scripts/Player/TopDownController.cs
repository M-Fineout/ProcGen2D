using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class TopDownController : MonoBehaviour
    {
        public float moveSpeed = 1f;
        public float collisionOffset = 0.02f;
        public ContactFilter2D movementFilter;
        List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();

        float moveX;
        float moveY;
        Vector2 moveDirection;

        bool isMoving;

        Animator animator;
        SpriteRenderer spriteRenderer;
        Rigidbody2D rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void FixedUpdate()
        {
            //Handle movement
            moveX = Input.GetAxis("Horizontal");
            moveY = Input.GetAxis("Vertical");

            if (moveX != 0)
            {
                moveY = 0;
            }

            moveDirection = new Vector2(moveX, moveY);

            if (moveDirection != Vector2.zero)
            {
                Debug.Log("Attempting move");
                //Move
                isMoving = AttemptMove(moveDirection);
            }
            else
            {
                isMoving = false;
            }

            animator.SetFloat("moveX", Mathf.Abs(moveX));
            animator.SetFloat("moveY", moveY);

            // Set direction of sprite to movement direction
            if (moveX != 0)
            {
                spriteRenderer.flipX = moveX < 0;              
            }
        }

        private bool AttemptMove(Vector2 direction)
        {
            var count = rb.Cast(direction, movementFilter, castCollisions, moveSpeed * Time.fixedDeltaTime + collisionOffset);
            Debug.Log(count);
            if (count == 0)
            {
                Debug.Log($"Moving in x: {direction.x}, y: {direction.y}");
                rb.MovePosition(rb.position + Time.deltaTime * moveSpeed * direction);
                return true;
            }
            return false;
        }
    }
}
