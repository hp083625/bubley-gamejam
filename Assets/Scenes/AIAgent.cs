using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using TMPro;

public class AIAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 120f;

    // Patrol variables
    public List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private bool isPatrolling = true;
    
    [Header("Patrol Settings")]
    public float waypointReachedDistance = 1.5f;
    public float stuckCheckTime = 5f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 4f;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private float stuckDistance = 0.05f;

    [Header("Follow Settings")]
    public float followDistance = 3f;
    public float followUpdateInterval = 0.5f;
    private bool isFollowingPlayer = false;
    private float followTimer = 0f;

    [Header("Player Detection")]
    public float detectionRadius = 5f;
    public float lookAtSpeed = 5f;
    private bool isInteractingWithPlayer = false;

    [Header("Object Navigation")]
    public float objectSearchRadius = 20f;
    public LayerMask searchLayers = -1;  // All layers by default

    [Header("Services")]
    public GroqService groqService;
    private GameObject player;
    private ChatUI chatUI;

    [Header("Speech Bubble")]
    private Transform speechBubble;
    private TextMeshProUGUI bubbleText;
    
    [Header("Personality")]
    public AgentPersonality personality;
    [SerializeField] private string defaultPersonalityPath = "AI/DefaultPersonality";

    // Public property to check follow state
    public bool IsFollowing => isFollowingPlayer;

    private float lastTabTime = 0f;
    private const float TAB_COOLDOWN = 4f;

    private bool isMovingToObject = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player");
        groqService = FindObjectOfType<GroqService>();
        chatUI = FindObjectOfType<ChatUI>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing!");
            enabled = false;
            return;
        }

        // Setup personality
        if (personality == null)
        {
            personality = Resources.Load<AgentPersonality>(defaultPersonalityPath);
            if (personality == null)
            {
                Debug.LogWarning($"No personality assigned and couldn't load default from {defaultPersonalityPath}");
                personality = ScriptableObject.CreateInstance<AgentPersonality>();
            }
        }

        // Setup NavMeshAgent
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = waypointReachedDistance * 0.8f;
        agent.autoBraking = true;

        lastPosition = transform.position;

        // Find speech bubble components
        speechBubble = transform.Find("SpeechBubble");
        if (speechBubble != null)
        {
            bubbleText = speechBubble.GetComponentInChildren<TextMeshProUGUI>();
            speechBubble.gameObject.SetActive(false);
            
            // Set bubble color based on personality
            var background = speechBubble.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
            {
                background.color = personality.bubbleColor;
            }

            // Show initial greeting
            if (!string.IsNullOrEmpty(personality.greeting))
            {
                SetBubbleText(personality.greeting);
                speechBubble.gameObject.SetActive(true);
                StartCoroutine(HideBubbleAfterDelay(3f));
            }
        }
        else
        {
            Debug.LogWarning("Speech bubble not found! Make sure there's a child object named 'SpeechBubble'");
        }

        if (waypoints.Count > 0)
        {
            SetNextWaypoint();
        }
    }

    void Update()
    {
        if (agent == null) return;

        // Check for player proximity first
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            
            if (distanceToPlayer <= detectionRadius && !isMovingToObject)  // Don't look at player if moving to object
            {
                // Stop and look at player
                agent.isStopped = true;
                isPatrolling = false;
                isInteractingWithPlayer = true;

                // Look at player smoothly
                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                directionToPlayer.y = 0;
                
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
                }

                // Show speech bubble if not already showing
                if (speechBubble != null && !speechBubble.gameObject.activeSelf)
                {
                    speechBubble.gameObject.SetActive(true);
                    if (bubbleText != null)
                    {
                        bubbleText.text = "Hello there!";
                    }
                }

                // Handle Tab key with cooldown
                if (Input.GetKeyDown(KeyCode.Tab) && chatUI != null)
                {
                    float currentTime = Time.time;
                    if (currentTime - lastTabTime >= TAB_COOLDOWN)
                    {
                        lastTabTime = currentTime;
                        if (chatUI.IsEnabled)
                        {
                            chatUI.HideChat();
                        }
                        else
                        {
                            chatUI.ShowChat(this);
                        }
                    }
                }

                return; // Skip other behaviors when interacting
            }
            else if (isInteractingWithPlayer && !isFollowingPlayer && !isMovingToObject)
            {
                // Resume patrol when player leaves detection radius
                isInteractingWithPlayer = false;
                isPatrolling = true;
                agent.isStopped = false;
                
                // Hide speech bubble
                if (speechBubble != null && !isMovingToObject)  // Keep bubble visible if moving to object
                {
                    speechBubble.gameObject.SetActive(false);
                }
            }
        }

        // Handle follow mode
        if (isFollowingPlayer && player != null)
        {
            followTimer += Time.deltaTime;
            if (followTimer >= followUpdateInterval)
            {
                followTimer = 0f;
                UpdateFollowPosition();
            }
            return;
        }

        // Normal patrol behavior
        if (isPatrolling)
        {
            HandlePatrolling();
        }

        // Check if stuck
        CheckIfStuck();
    }

    void HandlePatrolling()
    {
        if (isWaiting)
        {
            HandleWaiting();
            return;
        }

        if (HasReachedWaypoint())
        {
            StartWaiting();
        }
    }

    bool HasReachedWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Count) return false;

        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        return distanceToWaypoint <= waypointReachedDistance;
    }

    void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        if (movedDistance < stuckDistance)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckTime)
            {
                Debug.Log("Stuck detected, moving to next waypoint");
                SetNextWaypoint();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;
    }

    void HandleWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= minWaitTime)
        {
            isWaiting = false;
            waitTimer = 0f;
            SetNextWaypoint();
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
        agent.isStopped = true;
    }

    void SetNextWaypoint()
    {
        if (waypoints.Count == 0) return;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        if (agent != null && waypoints[currentWaypointIndex] != null)
        {
            agent.isStopped = false;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
            Debug.Log($"Moving to waypoint {currentWaypointIndex + 1}");
        }
    }

    void UpdateFollowPosition()
    {
        if (player == null) return;

        Vector3 targetPosition = player.transform.position;
        Vector3 directionToPlayer = transform.position - targetPosition;
        
        // Stay at follow distance
        Vector3 desiredPosition = targetPosition + directionToPlayer.normalized * followDistance;
        
        agent.isStopped = false;
        agent.SetDestination(desiredPosition);
    }

    public void ToggleFollowMode()
    {
        isFollowingPlayer = !isFollowingPlayer;
        string message = isFollowingPlayer ? "Following you!" : "Stopped following.";
        SetBubbleText(message);
        
        if (isFollowingPlayer)
        {
            isPatrolling = false;
            agent.isStopped = false;
        }
        else
        {
            isPatrolling = true;
            FindNearestWaypoint();
        }
    }

    void FindNearestWaypoint()
    {
        if (waypoints.Count == 0) return;

        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        currentWaypointIndex = nearestIndex;
        if (agent != null && waypoints[currentWaypointIndex] != null)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    public void SetBubbleText(string text)
    {
        if (bubbleText != null)
        {
            bubbleText.text = text;
        }
    }

    public bool MoveToObject(string objectName)
    {
        // Search for objects within radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, objectSearchRadius, searchLayers);
        GameObject targetObject = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.name.ToLower().Contains(objectName.ToLower()))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetObject = collider.gameObject;
                }
            }
        }

        if (targetObject != null)
        {
            // Stop current behaviors
            isPatrolling = false;
            isFollowingPlayer = false;
            isMovingToObject = true;
            isInteractingWithPlayer = false;
            
            // Set destination
            agent.isStopped = false;
            agent.SetDestination(targetObject.transform.position);
            
            if (bubbleText != null)
            {
                bubbleText.text = $"Going to {targetObject.name}!";
                speechBubble.gameObject.SetActive(true);
            }

            // Start a coroutine to check when we've reached the destination
            StartCoroutine(CheckDestinationReached(targetObject.transform.position));
            return true;
        }

        if (bubbleText != null)
        {
            bubbleText.text = $"I can't find {objectName}...";
            speechBubble.gameObject.SetActive(true);
        }
        return false;
    }

    private System.Collections.IEnumerator CheckDestinationReached(Vector3 targetPosition)
    {
        while (true)
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        // We've reached the destination
                        isMovingToObject = false;
                        isPatrolling = true;
                        
                        if (bubbleText != null)
                        {
                            bubbleText.text = "I'm here!";
                            // Start a coroutine to hide the bubble after a delay
                            StartCoroutine(HideBubbleAfterDelay(2f));
                        }
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private System.Collections.IEnumerator HideBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (speechBubble != null && !isInteractingWithPlayer)
        {
            speechBubble.gameObject.SetActive(false);
        }
    }

    public List<string> GetNearbyObjects()
    {
        List<string> nearbyObjects = new List<string>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, objectSearchRadius, searchLayers);
        
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.gameObject != player)
            {
                nearbyObjects.Add(collider.gameObject.name);
            }
        }
        
        return nearbyObjects;
    }
}
