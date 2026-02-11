using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LockedPlayerMovement : MonoBehaviour
{
    [Header("Room Settings")]
    [Tooltip("List of room nodes. Each room node contains child look nodes and a transition node.")]
    public List<RoomNode> roomNodes; // List of room nodes

    [Header("Camera Settings")]
    [Tooltip("Speed of camera rotation.")]
    public float rotationSpeed = 5f;

    [SerializeField, Tooltip("The index of the current room (for debugging purposes).")]
    private int currentRoomIndex = 0; // Index of the current room node

    public int CurrentRoomIndex => currentRoomIndex; // Public getter for the current room index

    private int currentLookNodeIndex = 0; // Index of the current look node in the room

    [Header("References")]
    [Tooltip("Reference to the MonsterMovement script.")]
    public MonsterMovement monsterMovement; // Reference to the MonsterMovement script

    [Tooltip("Reference to the PlayerFail handler (optional). If not set the script will try to find one at runtime).")]
    public PlayerFail playerFail;

    // optional QTE handler
    private ButtonMash buttonMash;

    private PlayerControls playerControls; // Reference to the PlayerControls input actions

    public bool hasWeapon = false;

    private void Start()
    {
        TransformToRoomNode();

        // Auto-find ButtonMash if not assigned
        if (buttonMash == null)
        {
            buttonMash = FindObjectOfType<ButtonMash>();
            if (buttonMash == null)
                Debug.LogWarning("ButtonMash not found in the scene. QTE will not be available.");
        }

        // Auto-find PlayerFail fallback if not assigned
        if (playerFail == null)
        {
            playerFail = FindObjectOfType<PlayerFail>();
            if (playerFail == null)
                Debug.LogWarning("PlayerFail not found in the scene. GameOver handling may not be routed correctly.");
        }

        // Auto-find MonsterMovement if not assigned
        if (monsterMovement == null)
        {
            monsterMovement = FindObjectOfType<MonsterMovement>();
            if (monsterMovement == null)
                Debug.LogWarning("MonsterMovement not found in the scene. LockedPlayerMovement QTE integration will not work.");
        }
    }

    private void Awake()
    {
        // Initialize the PlayerControls input actions
        playerControls = new PlayerControls();
    }

    void Update()
    {
        RotateToLookNode();
    }

    private void OnEnable()
    {
        // Enable the "Boat" action map
        playerControls.Boat.Enable();

        // Bind input actions to methods
        playerControls.Boat.Move.performed += OnMovePerformed;
        playerControls.Boat.LookLeft.performed += OnLookLeftPerformed;
        playerControls.Boat.LookRight.performed += OnLookRightPerformed;
    }

    private void OnDisable()
    {
        // Unbind input actions
        playerControls.Boat.Move.performed -= OnMovePerformed;
        playerControls.Boat.LookLeft.performed -= OnLookLeftPerformed;
        playerControls.Boat.LookRight.performed -= OnLookRightPerformed;

        // Disable the "Boat" action map
        playerControls.Boat.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        RoomNode currentRoomNode = roomNodes[currentRoomIndex];

        foreach (var transitionNode in currentRoomNode.transitionNodes)
        {
            if (IsFacingNode(transitionNode.nodeTransform))
            {
                // Check if the player is in the same room as the monster and the monster is actively moving through stages
                if (monsterMovement != null && currentRoomIndex == monsterMovement.currentRoomIndex && monsterMovement.isMovingThroughStages)
                {
                    // If a ButtonMash (QTE) is available, start it instead of instant death
                    if (buttonMash != null)
                    {
                        // compute the target next room but don't perform it yet
                        int targetRoomIndex = GetNextRoomIndex(transitionNode.doorName);

                        // Pause the monster so it stops moving while the QTE runs
                        monsterMovement.PauseForQTE();

                        // create handlers so we can unsubscribe the same delegate instances
                        Action successHandler = null;
                        Action failHandler = null;

                        successHandler = () =>
                        {
                            // move player to the target room on QTE success
                            currentRoomIndex = targetRoomIndex;
                            currentLookNodeIndex = 0;
                            TransformToRoomNode();

                            // send monster to caught area / respawn
                            if (monsterMovement != null)
                                monsterMovement.SendToCaughtArea();

                            // cleanup subscriptions
                            buttonMash.OnQTESuccess -= successHandler;
                            buttonMash.OnQTEFail -= failHandler;
                        };

                        failHandler = () =>
                        {
                            // QTE failed: ButtonMash already invokes PlayerFail.HandleGameOver.
                            // Resume monster behavior (if appropriate); PlayerFail may pause time immediately.
                            if (monsterMovement != null)
                                monsterMovement.ResumeAfterQTEFailure();

                            // cleanup subscriptions
                            buttonMash.OnQTESuccess -= successHandler;
                            buttonMash.OnQTEFail -= failHandler;
                        };

                        // subscribe and start QTE
                        buttonMash.OnQTESuccess += successHandler;
                        buttonMash.OnQTEFail += failHandler;
                        buttonMash.StartQTE();

                        return; // don't allow movement now; wait for QTE result
                    }

                    // No QTE available -> fallback to centralized game over
                    if (playerFail != null)
                    {
                        playerFail.HandleGameOver("Tried to move while the monster is attacking");
                    }
                    else
                    {
                        var pf = FindObjectOfType<PlayerFail>();
                        if (pf != null)
                            pf.HandleGameOver("Tried to move while the monster is attacking");
                        else
                            Debug.LogWarning("Game over condition met but no PlayerFail found in scene.");
                    }

                    return; // Prevent the player from moving when game over is triggered
                }

                // Determine the next room index based on the door name
                currentRoomIndex = GetNextRoomIndex(transitionNode.doorName);
                currentLookNodeIndex = 0; // Reset look node index when switching rooms
                TransformToRoomNode();
                break;
            }
        }
    }

    private void OnLookLeftPerformed(InputAction.CallbackContext context)
    {
        if (roomNodes.Count == 0) return;

        RoomNode currentRoomNode = roomNodes[currentRoomIndex];
        int lookNodeCount = currentRoomNode.lookNodes.Count;

        if (lookNodeCount == 0) return;

        // Move to the previous look node
        currentLookNodeIndex = (currentLookNodeIndex - 1 + lookNodeCount) % lookNodeCount;
    }

    private void OnLookRightPerformed(InputAction.CallbackContext context)
    {
        if (roomNodes.Count == 0) return;

        RoomNode currentRoomNode = roomNodes[currentRoomIndex];
        int lookNodeCount = currentRoomNode.lookNodes.Count;

        if (lookNodeCount == 0) return;

        // Move to the next look node
        currentLookNodeIndex = (currentLookNodeIndex + 1) % lookNodeCount;
    }

    private int GetNextRoomIndex(string doorName)
    {
        Debug.Log($"Attempting to transition through door: {doorName}");

        switch (doorName)
        {
            case "Deck":
                return 0; // Transition to room index 0
            case "Helm":
                return 1; // Transition to room index 1
            case "Generator":
                return 2; // Transition to room index 2
            case "Hide":
                return 3; // Transition to room index 3
            default:
                Debug.LogWarning($"Unknown door name: {doorName}");
                return currentRoomIndex; // Stay in the current room if no match
        }
    }

    // Transforms the player to the position of the current room node
    private void TransformToRoomNode()
    {
        if (roomNodes.Count == 0) return;

        RoomNode currentRoomNode = roomNodes[currentRoomIndex];
        Debug.Log($"Transitioning to room: {currentRoomNode.transform.name}");
        transform.position = currentRoomNode.transform.position; // Move the player to the room node's position
    }

    // Smoothly rotates the camera to face the selected look node
    private void RotateToLookNode()
    {
        if (roomNodes.Count == 0) return;

        RoomNode currentRoomNode = roomNodes[currentRoomIndex];
        if (currentRoomNode.lookNodes.Count == 0) return;

        Transform targetLookNode = currentRoomNode.lookNodes[currentLookNodeIndex];
        Vector3 direction = (targetLookNode.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate towards the target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool IsFacingNode(Transform node)
    {
        if (node == null) return false;

        Vector3 directionToNode = (node.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToNode);

        // Check if the player is facing the node within a small angle threshold
        return angle < 30f; // Adjust the angle threshold as needed
    }

    private void OnDrawGizmos()
    {
        if (roomNodes == null) return;

        Gizmos.color = Color.green;
        foreach (var room in roomNodes)
        {
            foreach (var transitionNode in room.transitionNodes)
            {
                if (transitionNode.nodeTransform != null)
                {
                    Gizmos.DrawSphere(transitionNode.nodeTransform.position, 0.5f);
                }
            }
        }
    }
}

[System.Serializable]
public class TransitionNode
{
    public string doorName; // Name of the door (e.g., "LeftDoor", "RightDoor")
    public Transform nodeTransform; // Transform of the transition node
}

[System.Serializable]
public class RoomNode
{
    public Transform transform; // The room's transform
    public List<TransitionNode> transitionNodes = new List<TransitionNode>(); // List of transition nodes
    public List<Transform> lookNodes; // List of look nodes in the room
}