using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundSpeed : MonoBehaviour
{
    [SerializeField] private Transform playerPosition;
    [SerializeField] private float minPitch = 1f;
    [SerializeField] private float maxPitch = 3f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float smooth = 8f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerPosition == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) playerPosition = go.transform;
        }
    }

    void Update()
    {
        if (playerPosition == null || audioSource == null) return;

        float dist = Vector3.Distance(transform.position, playerPosition.position);
        float t = Mathf.InverseLerp(maxDistance, 0f, dist); // 0 at far, 1 when close
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, t);
        audioSource.pitch = Mathf.Clamp(Mathf.Lerp(audioSource.pitch, targetPitch, smooth * Time.deltaTime), 0.01f, 10f);

        // optional: play only when in range
        if (dist <= maxDistance && !audioSource.isPlaying) audioSource.Play();
        if (dist > maxDistance && audioSource.isPlaying) audioSource.Stop();
    }
}