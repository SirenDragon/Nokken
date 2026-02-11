using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the Input System

public class Boxes : MonoBehaviour
{
    public bool isBroken = false; // Tracks if the box is broken

    [Tooltip("The room index this box belongs to.")]
    public int roomIndex; // Room index for the box

    private Renderer boxRenderer;
    private Score scoreManager; // Reference to the Score script
    private AudioSource audioSource; // Reference to the AudioSource component

    [Tooltip("Audio clip for the repair sound.")]
    public AudioClip repairSound; // Sound for repairing the box

    [Tooltip("Audio clip for the breaking sound.")]
    public AudioClip breakSound; // Sound for breaking the box

    private bool isPlayerHovering = false; // Tracks if the player is hovering over the box
    private Coroutine repairCoroutine; // Tracks the repair coroutine
    private bool isRepairSoundPlaying = false; // Tracks if the repair sound is already playing

    [Tooltip("Maximum distance the player can be from the box to repair it.")]
    public float repairDistance = 3f; // Maximum distance to repair the box

    private Transform playerTransform; // Reference to the player's transform
    private PlayerControls playerControls; // Reference to the PlayerControls input actions

    void Awake()
    {
        // Initialize the PlayerControls input actions
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Enable the "Boat" action map
        playerControls.Boat.Enable();

        // Bind the Repaire action to the repair logic
        playerControls.Boat.Repaire.started += OnRepaireStarted;
        playerControls.Boat.Repaire.canceled += OnRepaireCanceled;
    }

    private void OnDisable()
    {
        // Unbind the Repaire action
        playerControls.Boat.Repaire.started -= OnRepaireStarted;
        playerControls.Boat.Repaire.canceled -= OnRepaireCanceled;

        // Disable the "Boat" action map
        playerControls.Boat.Disable();
    }

    private void OnRepaireStarted(InputAction.CallbackContext context)
    {
        // Check if the player is hovering over the box and the box is broken
        if (isPlayerHovering && isBroken)
        {
            // Check if the player is within the repair distance
            if (playerTransform != null && Vector3.Distance(playerTransform.position, transform.position) <= repairDistance)
            {
                if (repairCoroutine == null)
                {
                    repairCoroutine = StartCoroutine(RepairBoxCoroutine());
                }
            }
            else
            {
                Debug.Log("Player is too far away to repair the box.");
            }
        }
    }

    private void OnRepaireCanceled(InputAction.CallbackContext context)
    {
        // Stop the repair process if the button is released
        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
            Debug.Log("Repair interrupted.");

            // Stop the repair sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Reset the repair sound flag
            isRepairSoundPlaying = false;
        }
    }

    void Start()
    {
        boxRenderer = GetComponent<Renderer>();
        SetBoxColor(Color.blue); // Initial color

        // Find the Score script in the scene
        scoreManager = Object.FindAnyObjectByType<Score>();
        if (scoreManager == null)
        {
            Debug.LogError("Score script not found in the scene!");
        }

        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component not found on the box!");
        }

        // Find the player's transform in the scene
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Player not found in the scene! Ensure the Player GameObject has the 'Player' tag.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Set the color of the gizmo
        Gizmos.color = Color.green;

        // Draw a wire sphere to represent the repair distance
        Gizmos.DrawWireSphere(transform.position, repairDistance);
    }

    // Checks if the box can be broken
    public bool CanBreak(int monsterRoomIndex)
    {
        // Check if the box is not broken and the monster is in the same room
        return !isBroken && monsterRoomIndex == roomIndex;
    }

    // Breaks the box
    public void BreakBox()
    {
        if (isBroken) return; // Prevent breaking an already broken box

        isBroken = true;
        SetBoxColor(Color.red); // Change color to red
        Debug.Log($"{gameObject.name} is broken!");

        // Play the breaking sound
        if (audioSource != null && breakSound != null)
        {
            audioSource.clip = breakSound;
            audioSource.Play();
        }

        // Decrease the score
        if (scoreManager != null)
        {
            scoreManager.DecreaseBoxScore();
        }
    }

    // Repairs the box
    public void RepairBox()
    {
        if (!isBroken) return; // Prevent repairing an already intact box

        isBroken = false;
        SetBoxColor(Color.blue); // Change color back to blue
        Debug.Log($"{gameObject.name} has been repaired!");

        // Stop the repair sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Increase the score
        if (scoreManager != null)
        {
            scoreManager.IncreaseBoxScore();
        }

        // Reset the repair sound flag
        isRepairSoundPlaying = false;
    }

    private void SetBoxColor(Color color)
    {
        if (boxRenderer != null)
        {
            boxRenderer.material.color = color;
        }
    }

    private void OnMouseEnter()
    {
        // Detect when the player's cursor is hovering over the box
        isPlayerHovering = true;
    }

    private void OnMouseExit()
    {
        // Detect when the player's cursor stops hovering over the box
        isPlayerHovering = false;

        // Stop the repair process if the player moves away
        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
            Debug.Log("Repair interrupted.");

            // Stop the repair sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Reset the repair sound flag
            isRepairSoundPlaying = false;
        }
    }

    private IEnumerator RepairBoxCoroutine()
    {
        Debug.Log("Repairing box...");

        // Play the repair sound if it's not already playing
        if (audioSource != null && repairSound != null && !isRepairSoundPlaying)
        {
            audioSource.clip = repairSound;
            audioSource.Play();
            isRepairSoundPlaying = true;
        }

        float repairTime = 5f; // Time required to repair the box
        float elapsedTime = 0f;

        while (elapsedTime < repairTime)
        {
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Repair the box
        RepairBox();

        repairCoroutine = null; // Reset the coroutine reference
    }
}
