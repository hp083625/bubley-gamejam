using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 6.0f;       // Movement speed
    public float jumpForce = 8.0f;   // Jump upward velocity
    public float gravity = 20.0f;    // Gravity
    
    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    // Reference to Animator
    public Animator animator;

    // We'll track whether we were moving previously if we need triggers for Idle/Run
    private bool wasMoving = false;

    void Start()
    {
        // Grab the CharacterController component
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Check if the player is on the ground
        if (controller.isGrounded)
        {
            // Get keyboard input for horizontal (A/D or Left/Right) and vertical (W/S or Up/Down) movement
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            
            // Move direction is based on player inputs
            moveDirection = new Vector3(moveX, 0, moveZ);
            
            // Transform direction from local to world space
            moveDirection = transform.TransformDirection(moveDirection);
            
            // Multiply by speed to determine how fast you move
            moveDirection *= speed;

            // OPTIONAL: If using triggers for Idle/Run
            bool isMoving = (moveDirection.sqrMagnitude > 0.01f);
            if (isMoving && !wasMoving)
            {
                // animator.SetTrigger("TriggerRun"); // If you use a run trigger
            }
            else if (!isMoving && wasMoving)
            {
                // animator.SetTrigger("TriggerIdle"); // If you use an idle trigger
            }
            wasMoving = isMoving;

            // Press Space to jump
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
                // If you have a jump trigger, you might do: animator.SetTrigger("TriggerJump");
            }
        }
        
        // Apply gravity over time
        moveDirection.y -= gravity * Time.deltaTime;
        
        // Move the character based on our calculated direction
        controller.Move(moveDirection * Time.deltaTime);

        // A small trick to keep the player on the ground
        if (controller.isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -1f;
        }
    }

    // --------------------------------------------------
    //  Detect collision with an "enemy" using OnTriggerEnter
    // --------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("enemy"))
        {
            // 1. Play the TriggerDie animation
            animator.SetTrigger("TriggerDie");

            // 2. (Optional) Disable movement so player can’t move after death
            //    This could be as simple as disabling this script:
            //    enabled = false;

            // 3. Destroy the player GameObject after 5 seconds
            Destroy(gameObject, 5f);
        }
    }
}
