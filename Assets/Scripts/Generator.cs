using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private ProgressBar repairProgress;
    private Label eTip;

    public bool isBroken = false; // Tracks if the generator is broken

    [SerializeField] private Material generatorMaterial; // Material for the generator
    [SerializeField] private Material brokenGeneratorMaterial; // Material for the broken generator

    [Tooltip("The room index this generator belongs to.")]
    public int roomIndex; // Room index for the generator

    private Renderer[] generatorRenderer;
    private readonly Dictionary<Renderer, Material[]> originalSharedMaterials = new Dictionary<Renderer, Material[]>();

    private Score scoreManager; // Reference to the Score script
    private AudioSource audioSource; // Reference to the AudioSource component

    [Tooltip("Audio clip for the repair sound.")]
    public AudioClip repairSound; // Sound for repairing the generator

    [Tooltip("Audio clip for the breaking sound.")]
    public AudioClip breakSound; // Sound for breaking the generator

    [Tooltip("Audio clip for the delayed sound after breaking.")]
    public AudioClip delayedBreakSound; // Sound to play after a delay when the generator breaks

    [Tooltip("Delay time in seconds for the delayed break sound.")]
    public float delayedSoundDelay = 2f; // Delay time for the delayed sound

    private bool isPlayerHovering = false; // Tracks if the player is hovering over the generator
    private Coroutine repairCoroutine; // Tracks the repair coroutine
    private bool isRepairSoundPlaying = false; // Tracks if the repair sound is already playing

    [Tooltip("Maximum distance the player can be from the generator to repair it.")]
    public float repairDistance = 5f; // Maximum distance to repair the generator

    private Transform playerTransform; // Reference to the player's transform
    private PlayerControls playerControls; // Reference to the PlayerControls input actions

    // Event: notifies subscribers when generator becomes online (true) or offline (false)
    public static event Action<bool> OnGeneratorStateChanged;

    [Header("Optional: toggle objects when generator breaks")]
    [Tooltip("GameObjects to disable when the generator is broken (e.g. lamp parent).")]
    public GameObject[] objectsToToggle;

    // runtime caches so we can restore original states when repaired
    private Dictionary<GameObject, bool> originalActive = new Dictionary<GameObject, bool>();

    void Awake()
    {
        // Initialize the PlayerControls input actions
        playerControls = new PlayerControls();
    }

    void Start()
    {
        eTip = uiDocument.rootVisualElement.Q<Label>("ETip");

        repairProgress = uiDocument.rootVisualElement.Q<ProgressBar>("RepairProgress");
        repairProgress.lowValue = 0f;
        repairProgress.highValue = 1f;
        repairProgress.value = 0f;

        generatorRenderer = GetComponentsInChildren<Renderer>(true);

        // Find the Score script in the scene
        scoreManager = FindObjectOfType<Score>();
        if (scoreManager == null)
        {
            //Debug.LogError("Score script not found in the scene!");
        }

        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component not found on the generator!");
        }

        // Find the player's transform in the scene
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            //Debug.LogError("Player not found in the scene! Ensure the Player GameObject has the 'Player' tag.");
        }

        // cache original states for restore
        CacheOriginalStates();

        // Notify any lamps / subscribers of the current state at start
        OnGeneratorStateChanged?.Invoke(!isBroken);
    }

    void CacheOriginalStates()
    {
        if (objectsToToggle != null)
        {
            foreach (var go in objectsToToggle)
            {
                if (go == null) continue;
                if (!originalActive.ContainsKey(go))
                    originalActive[go] = go.activeSelf;
            }
        }
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
        // Check if the player is hovering over the generator and the generator is broken
        if (isPlayerHovering && isBroken)
        {
            // Check if the player is within the repair distance
            if (playerTransform != null && Vector3.Distance(playerTransform.position, transform.position) <= repairDistance)
            {
                if (repairCoroutine == null)
                {
                    repairCoroutine = StartCoroutine(RepairGeneratorCoroutine());
                }
            }
            else
            {
                Debug.Log("Player is too far away to repair the generator.");
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
            repairProgress.visible = false; // Hide the progress bar
            repairProgress.value = 0; // Reset the progress bar

            // Stop the repair sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Reset the repair sound flag
            isRepairSoundPlaying = false;
        }
    }

    private IEnumerator RepairGeneratorCoroutine()
    {
        Debug.Log("Repairing generator...");

        // Play the repair sound if it's not already playing
        if (audioSource != null && repairSound != null && !isRepairSoundPlaying)
        {
            audioSource.clip = repairSound;
            audioSource.Play();
            isRepairSoundPlaying = true;
        }

        float repairTime = 10f; // Time required to repair the generator
        float elapsedTime = 0f;

        if (repairProgress != null)
            repairProgress.visible = true;

        while (elapsedTime < repairTime)
        {
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;

            if (repairProgress != null)
            {
                repairProgress.value = Mathf.Clamp01(elapsedTime / repairTime); // Update the progress bar (0..1)
            }

            // Wait for the next frame
            yield return null;
        }

        // Repair the generator
        RepairGenerator();

        // hide and reset progress UI
        if (repairProgress != null)
        {
            repairProgress.visible = false;
            repairProgress.value = 0f;
        }

        repairCoroutine = null; // Reset the coroutine reference
    }

    // Checks if the generator can be interacted with based on the room index
    public bool CanInteract(int playerRoomIndex)
    {
        // Check if the generator is in the same room as the player
        return playerRoomIndex == roomIndex;
    }

    // Breaks the generator
    public void BreakGenerator()
    {
        if (isBroken) return; // Prevent breaking an already broken generator

        isBroken = true;
        foreach (Renderer rend in generatorRenderer)
        {
            rend.material = brokenGeneratorMaterial; // Change material to indicate breakage
        }
        Debug.Log($"{gameObject.name} is broken!");

        // apply visual / object disabling for break
        ApplyBrokenState(true);

        // notify lamps / subscribers: generator is offline
        OnGeneratorStateChanged?.Invoke(false);

        // Play the breaking sound
        if (audioSource != null && breakSound != null)
        {
            audioSource.clip = breakSound;
            audioSource.Play();
        }

        // Start the delayed sound coroutine
        if (audioSource != null && delayedBreakSound != null)
        {
            StartCoroutine(PlayDelayedBreakSound());
        }

        // Decrease the generator score
        if (scoreManager != null)
        {
            scoreManager.DecreaseGeneratorScore();
        }
    }

    // Coroutine to play the delayed break sound
    private IEnumerator PlayDelayedBreakSound()
    {
        yield return new WaitForSeconds(delayedSoundDelay); // Wait for the specified delay

        // Play the delayed break sound
        audioSource.clip = delayedBreakSound;
        audioSource.Play();
    }

    // Repairs the generator
    public void RepairGenerator()
    {
        if (!isBroken) return; // Prevent repairing an already intact generator

        isBroken = false;
        foreach (Renderer rend in generatorRenderer)
        {
            rend.material = generatorMaterial; // Change material back to indicate repair
        }
        Debug.Log($"{gameObject.name} has been repaired!");
        eTip.visible = false; // Hide the "Press E to Repair" tip


        var profile = FindObjectOfType<UserProfileData>();
        if (profile != null)
            profile.generatorsFixed++;

        // restore visuals / objects
        ApplyBrokenState(false);

        // notify lamps / subscribers: generator is online
        OnGeneratorStateChanged?.Invoke(true);

        // Stop the repair sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Increase the generator score
        if (scoreManager != null)
        {
            scoreManager.IncreaseGeneratorScore();
        }

        // Reset the repair sound flag
        isRepairSoundPlaying = false;
    }

    private void ApplyBrokenState(bool broken)
    {
        // toggle GameObjects
        if (objectsToToggle != null)
        {
            foreach (var go in objectsToToggle)
            {
                if (go == null) continue;
                if (broken)
                    go.SetActive(true);
                else
                {
                    bool original;
                    if (originalActive.TryGetValue(go, out original))
                        go.SetActive(original);
                    else
                        go.SetActive(false);
                }
            }
        }
    }

    private void OnMouseEnter()
    {
        // Detect when the player's cursor is hovering over the generator
        isPlayerHovering = true;

        if (isBroken)
        {
            eTip.visible = true; // Show the "Press E to Repair" tip
        }
    }

    private void OnMouseExit()
    {
        // Detect when the player's cursor stops hovering over the generator
        isPlayerHovering = false;
        eTip.visible = false; // Hide the "Press E to Repair" tip
        repairProgress.visible = false;

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

    private void OnDrawGizmosSelected()
    {
        // Set the color of the gizmo
        Gizmos.color = Color.yellow;

        // Draw a wire sphere to represent the repair distance
        Gizmos.DrawWireSphere(transform.position, repairDistance);
    }
}