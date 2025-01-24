using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 6.0f;       // Movement speed
    public float jumpForce = 8.0f;   // Jumping upward velocity
    public float gravity = 20.0f;    // Gravity, feel free to adjust
    
    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

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
            
            // Press Space to jump
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        
        // Apply gravity over time
        moveDirection.y -= gravity * Time.deltaTime;
        
        // Move the character based on our calculated direction
        controller.Move(moveDirection * Time.deltaTime);
    }
}
