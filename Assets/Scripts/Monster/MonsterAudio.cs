using System.Collections.Generic;
using UnityEngine;

public class MonsterAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip caughtSound;
    public AudioClip hurtSound;
    public AudioClip stageStartSound;
    public List<AudioClip> stageMoveSounds;
    public List<AudioClip> ambientSounds;
    public float minAmbientInterval = 5f;
    public float maxAmbientInterval = 15f;

    public void PlayCaughtSound()
    {
        if (audioSource != null && caughtSound != null)
            audioSource.PlayOneShot(caughtSound);
    }

    public void PlayHurtSound()
    {
        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);
    }

    public void PlayStageMoveSound()
    {
        if (audioSource != null && stageMoveSounds != null && stageMoveSounds.Count > 0)
        {
            int randomIndex = Random.Range(0, stageMoveSounds.Count);
            audioSource.PlayOneShot(stageMoveSounds[randomIndex]);
        }
    }

    public void PlayAmbientSound()
    {
        if (audioSource != null && ambientSounds != null && ambientSounds.Count > 0)
        {
            int randomIndex = Random.Range(0, ambientSounds.Count);
            audioSource.PlayOneShot(ambientSounds[randomIndex]);
        }
    }
}