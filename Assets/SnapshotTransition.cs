using UnityEngine;
using UnityEngine.Audio;

public class SnapshotTransition : MonoBehaviour
{
    [Tooltip("Transform of the player (will try to auto-find by tag 'Player' if empty).")]
    public Transform player;

    [Tooltip("The object to measure distance to.")]
    public Transform targetObject;

    [Header("Audio Mixer Snapshots")]
    public AudioMixer audioMixer;
    public AudioMixerSnapshot nearSnapshot; // used when player is close
    public AudioMixerSnapshot farSnapshot;  // used when player is far

    [Header("Distance Settings")]
    [Tooltip("Distance at which the transition is fully to the far snapshot.")]
    public float maxDistance = 10f;

    [Tooltip("How long the mixer should take to apply each transition call.")]
    public float transitionTime = 0.5f;

    // Small threshold so we don't spam TransitionToSnapshots every frame for tiny changes.
    [SerializeField, HideInInspector]
    private float lastNearWeight = -1f;

    private void Awake()
    {
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (maxDistance <= 0f) maxDistance = 0.01f;
    }

    private void Update()
    {
        // Basic validation
        if (player == null || targetObject == null || audioMixer == null || nearSnapshot == null || farSnapshot == null)
            return;

        // Get normalized distance [0..1] where 0 == player on top of target (nearSnapshot)
        // and 1 == at or beyond maxDistance (farSnapshot).
        float distance = Vector3.Distance(player.position, targetObject.position);
        float t = Mathf.Clamp01(distance / maxDistance);

        // Weight for the near snapshot (1 when close, 0 when far)
        float nearWeight = 1f - t;
        float farWeight = t;

        // Only update mixer when the weight changed noticeably to avoid unnecessary calls.
        if (Mathf.Abs(nearWeight - lastNearWeight) > 0.01f)
        {
            audioMixer.TransitionToSnapshots(
                new AudioMixerSnapshot[] { nearSnapshot, farSnapshot },
                new float[] { nearWeight, farWeight },
                transitionTime
            );

            lastNearWeight = nearWeight;
        }
    }
}