using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
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

    [Tooltip("Layer mask to filter objects in the field of view.")]
    public LayerMask targetMask;

    [Header("Detection State")]
    [Tooltip("True if an enemy is within the field of view.")]
    [SerializeField]
    private bool isSpotted; // Keep the field private

    // Add a public getter
    public bool IsSpotted => isSpotted;





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
                    lightComponent.intensity = flashlightActive ? 15f : 0f;
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
            lightComponent.intensity = Random.Range(0.5f, 14f);

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

            // Adjust the intensity of the Light component
            if (flashlight != null)
            {
                Light lightComponent = flashlight.GetComponent<Light>();
                if (lightComponent != null)
                {
                    lightComponent.intensity = flashlightActive ? 15f : 0f; // Set intensity to 1 when active, 0 when inactive
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

        // Find all objects within the view distance
        Collider[] targetsInViewRadius = Physics.OverlapSphere(flashlight.position, viewDistance, targetMask);

        foreach (Collider target in targetsInViewRadius)
        {
            // Check if the target is tagged as "enemy"
            if (target.CompareTag("enemy"))
            {
                Vector3 directionToTarget = (target.transform.position - flashlight.position).normalized;

                // Check if the target is within the view angle
                if (Vector3.Angle(flashlight.forward, directionToTarget) < viewAngle / 2)
                {
                    // Perform a raycast to ensure there are no obstacles blocking the view
                    if (Physics.Raycast(flashlight.position, directionToTarget, out RaycastHit hit, viewDistance))
                    {
                        if (hit.collider.CompareTag("enemy"))
                        {
                            isSpotted = true;
                            break; // Exit the loop as soon as an enemy is spotted
                        }
                    }
                }
            }
        }
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
}
