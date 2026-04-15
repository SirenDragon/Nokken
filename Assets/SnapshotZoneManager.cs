using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton manager that blends snapshots for active zones.
/// Supports polling the player's position (useful for teleporting players).
/// </summary>
public class SnapshotZoneManager : MonoBehaviour
{
    public static SnapshotZoneManager Instance { get; private set; }

    [Tooltip("AudioMixer used to TransitionToSnapshots.")]
    public AudioMixer audioMixer;

    [Tooltip("Snapshot used when the player is not inside any zone.")]
    public AudioMixerSnapshot defaultSnapshot;

    [Tooltip("Transition duration when switching/blending snapshots.")]
    public float transitionTime = 0.5f;

    [Tooltip("If true, the manager will check the player position every frame using ContainsPoint. Use this when the player teleports.")]
    public bool pollPlayerPosition = true;

    [Tooltip("Player transform used for polling. Auto-find by tag if empty.")]
    public Transform player;

    // All registered zones
    private readonly HashSet<SnapshotZone> allZones = new HashSet<SnapshotZone>();

    // Active zones currently considered "inside"
    private readonly HashSet<SnapshotZone> activeZones = new HashSet<SnapshotZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        // IMPORTANT: register any SnapshotZone components that were enabled before this manager's Awake ran.
        // This prevents zones from never being registered when SnapshotZone.OnEnable ran before manager existed.
        var preexisting = FindObjectsOfType<SnapshotZone>();
        foreach (var z in preexisting)
            RegisterZone(z);

        Debug.Log($"[SnapshotZoneManager] Awake: found {preexisting.Length} zones, player={(player != null ? player.name : "null")}, audioMixer={(audioMixer != null ? "assigned" : "null")}");
    }

    private void Update()
    {
        if (pollPlayerPosition && player != null)
            UpdateZonesForPosition(player.position);
    }

    public void RegisterZone(SnapshotZone zone)
    {
        if (zone == null) return;
        allZones.Add(zone);
    }

    public void UnregisterZone(SnapshotZone zone)
    {
        if (zone == null) return;
        allZones.Remove(zone);
        if (activeZones.Remove(zone)) UpdateMixer();
    }

    // Called by SnapshotZone trigger events
    public void EnterZone(SnapshotZone zone)
    {
        if (zone == null || zone.snapshot == null) return;
        if (activeZones.Add(zone)) UpdateMixer();
    }

    public void ExitZone(SnapshotZone zone)
    {
        if (zone == null) return;
        if (activeZones.Remove(zone)) UpdateMixer();
    }

    /// <summary>
    /// Polls all registered zones for whether <paramref name="worldPoint"/> is inside them.
    /// Call this from your teleport code immediately after moving the player to force an immediate update.
    /// </summary>
    public void UpdateZonesForPosition(Vector3 worldPoint)
    {
        var newActive = new HashSet<SnapshotZone>();
        foreach (var z in allZones)
        {
            if (z == null) continue;
            if (z.ContainsPoint(worldPoint))
                newActive.Add(z);
        }

        // Add newly entered zones
        foreach (var z in newActive)
        {
            if (!activeZones.Contains(z))
                activeZones.Add(z);
        }

        // Remove zones that are no longer active
        var removed = new List<SnapshotZone>();
        foreach (var z in activeZones)
        {
            if (!newActive.Contains(z))
                removed.Add(z);
        }
        foreach (var r in removed) activeZones.Remove(r);

        UpdateMixer();
    }

    private void UpdateMixer()
    {
        if (audioMixer == null) return;

        if (activeZones.Count == 0)
        {
            if (defaultSnapshot != null)
                audioMixer.TransitionToSnapshots(new[] { defaultSnapshot }, new[] { 1f }, transitionTime);
            return;
        }

        var counts = new Dictionary<AudioMixerSnapshot, int>();
        foreach (var z in activeZones)
        {
            if (z == null || z.snapshot == null) continue;
            if (!counts.TryGetValue(z.snapshot, out var c)) c = 0;
            counts[z.snapshot] = c + 1;
        }

        var snapshots = new List<AudioMixerSnapshot>();
        var weights = new List<float>();
        int total = 0;
        foreach (var kv in counts) total += kv.Value;
        if (total == 0)
        {
            if (defaultSnapshot != null)
                audioMixer.TransitionToSnapshots(new[] { defaultSnapshot }, new[] { 1f }, transitionTime);
            return;
        }

        foreach (var kv in counts)
        {
            snapshots.Add(kv.Key);
            weights.Add(kv.Value / (float)total);
        }

        audioMixer.TransitionToSnapshots(snapshots.ToArray(), weights.ToArray(), transitionTime);
        //Debug.Log($"[SnapshotZoneManager] TransitionToSnapshots: {snapshots.Count} snapshots, transitionTime={transitionTime:F2}");
    }
}