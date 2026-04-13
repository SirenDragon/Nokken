using UnityEngine;
using UnityEngine.Audio;

public class SnapshotTransition : MonoBehaviour
{
    [Tooltip("Transform of the player (will try to auto-find by tag 'Player' if empty).")]
    public Transform player;

    [Tooltip("The object to measure distance to.")]
    public Transform targetObject;

    [Header("Audio Mixer Snapshots (near -> mid -> far)")]
    public AudioMixer audioMixer;
    public AudioMixerSnapshot nearSnapshot; // used when player is very close
    public AudioMixerSnapshot midSnapshot;  // used in the middle range
    public AudioMixerSnapshot farSnapshot;  // used when player is far

    [Header("Distance Settings")]
    [Tooltip("Distance at which the transition is fully to the far snapshot.")]
    public float maxDistance = 10f;

    [Tooltip("Distance at which mid snapshot is centered. Must be between 0 and maxDistance.")]
    public float midDistance = 5f;

    [Tooltip("How long the mixer should take to apply each transition call.")]
    public float transitionTime = 0.5f;

    // small threshold so we don't spam TransitionToSnapshots every frame for tiny changes.
    [SerializeField, HideInInspector]
    private float lastNearWeight = -1f;
    [SerializeField, HideInInspector]
    private float lastMidWeight = -1f;
    [SerializeField, HideInInspector]
    private float lastFarWeight = -1f;

    private void Awake()
    {
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (maxDistance <= 0f) maxDistance = 0.01f;
        midDistance = Mathf.Clamp(midDistance, 0f, maxDistance);
    }

    private void Update()
    {
        // Basic validation
        if (player == null || targetObject == null || audioMixer == null || nearSnapshot == null || midSnapshot == null || farSnapshot == null)
            return;

        float distance = Vector3.Distance(player.position, targetObject.position);
        distance = Mathf.Clamp(distance, 0f, maxDistance);

        // Piecewise linear blending:
        //  - [0 .. midDistance] blend near -> mid
        //  - (midDistance .. maxDistance] blend mid -> far
        float nearWeight = 0f;
        float midWeight = 0f;
        float farWeight = 0f;

        if (midDistance <= 0f)
        {
            // No mid region: blend near -> far across full range
            float t = maxDistance <= 0f ? 0f : distance / maxDistance;
            nearWeight = 1f - t;
            farWeight = t;
            midWeight = 0f;
        }
        else if (distance <= midDistance)
        {
            float t = midDistance <= 0f ? 0f : distance / midDistance;
            nearWeight = 1f - t;
            midWeight = t;
            farWeight = 0f;
        }
        else
        {
            float denom = (maxDistance - midDistance);
            float t = denom <= 0f ? 1f : (distance - midDistance) / denom;
            midWeight = 1f - t;
            farWeight = t;
            nearWeight = 0f;
        }

        // Avoid tiny repeated calls
        if (Mathf.Abs(nearWeight - lastNearWeight) > 0.01f ||
            Mathf.Abs(midWeight - lastMidWeight) > 0.01f ||
            Mathf.Abs(farWeight - lastFarWeight) > 0.01f)
        {
            audioMixer.TransitionToSnapshots(
                new AudioMixerSnapshot[] { nearSnapshot, midSnapshot, farSnapshot },
                new float[] { nearWeight, midWeight, farWeight },
                transitionTime
            );

            lastNearWeight = nearWeight;
            lastMidWeight = midWeight;
            lastFarWeight = farWeight;
        }
    }
}