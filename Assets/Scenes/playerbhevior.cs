using UnityEngine;
using UnityEngine.AI;

public class playerbhevior : MonoBehaviour
{
    private NavMeshAgent agent;
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;

    // Movement settings
    public float acceleration = 8f;

    private Camera mainCamera;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing!");
            enabled = false;
            return;
        }

        // Setup agent
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = acceleration;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    void Update()
    {
        // Skip movement input if chatting
        if (ChatUI.IsTyping)
        {
            return;
        }

        // Handle keyboard input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            // Convert input to movement vector
            Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Get camera forward and right vectors
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            
            // Project camera directions onto the horizontal plane
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Calculate movement direction relative to camera
            Vector3 moveDirection = (cameraRight * movement.x + cameraForward * movement.z);
            
            // Move the agent
            agent.isStopped = false;
            agent.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
        else
        {
            agent.isStopped = true;
        }
    }
}
