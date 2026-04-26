using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    //My attempt
    public AudioSource audioSource;
    public AudioClip lightOnSound;
    public AudioClip lightOffSound;

    [Header("Flashlight Settings")]
    [Tooltip("The flashlight GameObject (e.g., a Spot Light).")]
    public Transform flashlight;

    [Tooltip("The speed at which the flashlight follows the mouse.")]
    public float followSpeed = 5f;

    [Tooltip("The distance from the player to project the flashlight.")]
    public float flashlightDistance = 5f;

    [Header("Field of View Settings")]
    [Tooltip("The maximum distance the flashlight can see.")]
    public float viewDistance = 10f;

    [Tooltip("The angle of the field of view in degrees.")]
    public float viewAngle = 45f;

    [Tooltip("Layer mask to filter objects in the field of view (targets).")]
    public LayerMask targetMask;

    [Tooltip("Layer mask containing possible obstructing layers (walls, geometry). Include enemy layers as well if needed for raycast checks.")]
    public LayerMask obstructionMask = ~0;

    [Header("Detection State")]
    [Tooltip("True if an enemy is within the field of view.")]
    [SerializeField]
    private bool isSpotted; // Keep the field private

    // Add a public getter
    public bool IsSpotted => isSpotted;

    [Header("Debug / Tuning")]
    [Tooltip("Draw raycasts and hit info in Scene view.")]
    public bool debugDrawRays = true;

    [Tooltip("Small offset to move ray origin forward to avoid starting inside geometry.")]
    public float originOffset = 0.1f;

    [Header("Flashlight State")]
    [Tooltip("True if the flashlight is currently active.")]
    public bool flashlightActive = true;

    private Coroutine flickerCoroutine;

    void Update()
    {
        HandleFlashlightToggle();
        MoveFlashlight();
        CheckFieldOfView();

        // Start or stop the flickering effect based on the isSpotted state
        if (isSpotted && flickerCoroutine == null)
        {
            flickerCoroutine = StartCoroutine(FlickerFlashlight());
        }
        else if (!isSpotted && flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;

            // Reset flashlight intensity to normal when not flickering
            if (flashlight != null)
            {
                Light lightComponent = flashlight.GetComponent<Light>();
                if (lightComponent != null)
                {
                    lightComponent.intensity = flashlightActive ? 100f : 0f;
                }
            }
        }
    }

    private IEnumerator FlickerFlashlight()
    {
        if (flashlight == null) yield break;

        Light lightComponent = flashlight.GetComponent<Light>();
        if (lightComponent == null) yield break;

        while (true)
        {
            // Randomly adjust the intensity to create a flickering effect
            lightComponent.intensity = Random.Range(10f, 80f);

            // Wait for a short random duration before changing intensity again
            yield return new WaitForSeconds(Random.Range(0.02f, 0.1f));
        }
    }

    private void HandleFlashlightToggle()
    {
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            flashlightActive = !flashlightActive; // Toggle the flashlight state

            //play sounds
            if (flashlightActive)
                PlayLightOnSound();
            else
                PlayLightOffSound();

            // Adjust the intensity of the Light component
            if (flashlight != null)
            {
                Light lightComponent = flashlight.GetComponent<Light>();
                if (lightComponent != null)
                {
                    lightComponent.intensity = flashlightActive ? 100f : 0f; // Set intensity to 1 when active, 0 when inactive
                }
            }
        }
    }


    private void MoveFlashlight()
    {
        if (flashlight == null) return;

        // Get the mouse position in screen space
        Vector3 mouseScreenPosition = Input.mousePosition;

        // Convert the mouse position to world space
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            flashlightDistance // Use the flashlight distance as the Z offset
        ));

        // Calculate the direction from the flashlight to the mouse world position
        Vector3 direction = (mouseWorldPosition - flashlight.position).normalized;

        // Calculate the target rotation to look at the mouse position
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate the flashlight to face the target direction
        flashlight.rotation = Quaternion.Slerp(flashlight.rotation, targetRotation, followSpeed * Time.deltaTime);
    }

    private void CheckFieldOfView()
    {
        // Ensure the flashlight is active before checking the field of view
        if (!flashlightActive)
        {
            isSpotted = false; // Reset the spotted state
            return;
        }

        isSpotted = false; // Reset the spotted state

        if (flashlight == null) return;

        // find potential targets by overlap using the targetMask
        Collider[] targetsInViewRadius = Physics.OverlapSphere(flashlight.position, viewDistance, targetMask);

        // combine layers for raycast checks so we consider both targets and obstructions
        int raycastMask = targetMask.value | obstructionMask.value;

        foreach (Collider target in targetsInViewRadius)
        {
            if (target == null) continue;

            // Check if the target is tagged as "enemy"
            if (!target.CompareTag("enemy")) continue;

            // direction from flashlight origin -> target closest point to account for big colliders
            Vector3 targetPoint = target.ClosestPoint(flashlight.position);
            Vector3 directionToTarget = (targetPoint - (flashlight.position + flashlight.forward * originOffset)).normalized;

            // Check angle using flashlight.forward (ensure your spotlight's forward is aligned)
            if (Vector3.Angle(flashlight.forward, directionToTarget) < viewAngle / 2f)
            {
                // Raycast against combined mask to see what we hit first
                Vector3 rayOrigin = flashlight.position + flashlight.forward * originOffset;
                if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit hit, viewDistance, raycastMask, QueryTriggerInteraction.Ignore))
                {
                    bool hitIsEnemy = hit.collider.CompareTag("enemy");

                    if (debugDrawRays)
                    {
                        Debug.DrawLine(rayOrigin, hit.point, hitIsEnemy ? Color.green : Color.red, 0.5f);
                        Debug.DrawRay(hit.point, Vector3.up * 0.2f, hitIsEnemy ? Color.green : Color.red, 0.5f);
                        Debug.Log($"Flashlight ray hit: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}), isEnemy={hitIsEnemy}");
                    }

                    if (hitIsEnemy)
                    {
                        isSpotted = true;
                        break;
                    }
                    // else blocked by something else; continue checking other targets
                }
                else
                {
                    if (debugDrawRays)
                    {
                        Debug.DrawRay(rayOrigin, directionToTarget * viewDistance, Color.yellow, 0.5f);
                        Debug.Log("Flashlight raycast did not hit any collider (maybe layers excluded).");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a list of colliders that are currently visible by the flashlight (useful for debug UI).
    /// </summary>
    public List<Collider> GetCurrentlyVisibleTargets()
    {
        List<Collider> visible = new List<Collider>();
        if (flashlight == null || !flashlightActive) return visible;

        Collider[] targets = Physics.OverlapSphere(flashlight.position, viewDistance, targetMask);
        int raycastMask = targetMask.value | obstructionMask.value;
        Vector3 rayOrigin = flashlight.position + flashlight.forward * originOffset;

        foreach (Collider c in targets)
        {
            if (c == null) continue;
            if (!c.CompareTag("enemy")) continue;

            Vector3 targetPoint = c.ClosestPoint(flashlight.position);
            Vector3 dir = (targetPoint - rayOrigin).normalized;

            if (Vector3.Angle(flashlight.forward, dir) >= viewAngle / 2f) continue;

            if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, viewDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == c || hit.collider.transform.IsChildOf(c.transform) || c.transform.IsChildOf(hit.collider.transform))
                {
                    visible.Add(c);
                }
            }
        }

        return visible;
    }

    private void OnDrawGizmosSelected()
    {
        if (flashlight == null) return;

        // Draw the detection range as a wireframe sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(flashlight.position, viewDistance);

        // Draw the FOV cone
        Gizmos.color = new Color(0, 0, 1, 0.2f); // Semi-transparent blue
        int segments = 20; // Number of segments to draw the cone
        float angleStep = viewAngle / segments;

        Vector3 previousPoint = flashlight.position + (Quaternion.Euler(0, -viewAngle / 2, 0) * flashlight.forward * viewDistance);

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -viewAngle / 2 + angleStep * i;
            Vector3 currentPoint = flashlight.position + (Quaternion.Euler(0, currentAngle, 0) * flashlight.forward * viewDistance);

            // Draw a line between the previous point and the current point
            Gizmos.DrawLine(previousPoint, currentPoint);

            // Draw a line from the flashlight to the current point
            Gizmos.DrawLine(flashlight.position, currentPoint);

            previousPoint = currentPoint;
        }
    }

    public void PlayLightOnSound()
    {
        if (audioSource != null && lightOnSound != null)
            audioSource.PlayOneShot(lightOnSound);
    }

    public void PlayLightOffSound()
    {
        if (audioSource != null && lightOffSound != null)
            audioSource.PlayOneShot(lightOffSound);
    }
}