using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public float speed = 5f; // Movement speed
    public float rotationSpeed = 720f; // Rotation speed in degrees per second

    private CharacterController characterController;

    void Start()
    {
        // Get the CharacterController component attached to the capsule
        characterController = GetComponent<CharacterController>();

        // Ensure the CharacterController is attached
        if (characterController == null)
        {
            Debug.LogError("CharacterController not found! Please attach a CharacterController to the capsule.");
        }
    }

    void Update()
    {
        // Get input from keyboard (WASD or arrow keys)
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Create a movement vector based on input
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // Normalize the movement vector to ensure consistent speed in all directions
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }

        // Move the character using the CharacterController
        characterController.Move(movement * speed * Time.deltaTime);

        // Rotate the capsule to face the movement direction (if there's input)
        if (movement.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}