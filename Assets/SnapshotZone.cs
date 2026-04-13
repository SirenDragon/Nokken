using UnityEngine;

/// <summary>
/// Attach this to a GameObject with a Collider representing a zone.
/// Works with both trigger events and explicit position checks (teleports).
/// </summary>
public class SnapshotZone : MonoBehaviour
{
    [Tooltip("Snapshot to activate while the player is inside this trigger zone.")]
    public UnityEngine.Audio.AudioMixerSnapshot snapshot;

    [Tooltip("If true, only objects with this tag will trigger the zone.")]
    public bool requirePlayerTag = true;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Tooltip("If true, the zone will use OnTriggerEnter/Exit. If you teleport the player, prefer polling mode in the manager.")]
    public bool useTriggerEvents = true;

    // explicit collider reference (auto-find on Awake)
    [Tooltip("Collider used for point-in-zone checks. Auto-assigned from this GameObject if empty.")]
    public Collider zoneCollider;

    private void Awake()
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        SnapshotZoneManager.Instance?.RegisterZone(this);
    }

    private void OnDisable()
    {
        SnapshotZoneManager.Instance?.UnregisterZone(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerEvents) return;
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;
        SnapshotZoneManager.Instance?.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!useTriggerEvents) return;
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;
        SnapshotZoneManager.Instance?.ExitZone(this);
    }

    /// <summary>
    /// Returns true when the supplied world point is inside this zone's collider.
    /// Uses Collider.ClosestPoint — if the closest point equals the test point, the point is inside.
    /// </summary>
    public bool ContainsPoint(Vector3 worldPoint)
    {
        if (zoneCollider == null) return false;

        Vector3 closest = zoneCollider.ClosestPoint(worldPoint);
        return Vector3.Distance(closest, worldPoint) < 0.001f;
    }
}