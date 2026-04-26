using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class WeaponCharge : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private ProgressBar chargerProgress;

    private Renderer[] chargerRenderer;
    private readonly Dictionary<Renderer, Material[]> originalSharedMaterials = new Dictionary<Renderer, Material[]>();

    [SerializeField] private Material chargerOffMaterial;
    [SerializeField] private Material chargerOnMaterial;

    //My attempt
    public AudioSource audioSource;
    public List<AudioClip> chargedSounds;

    public Image chargeBar;

    float currentCharge = 0f;
    float maxCharge = 100f;
    float chargeRate = 10f;

    bool isCharging = false;
    bool isFullyCharged = false;

    // Only allow charging when a Charger enables it
    [HideInInspector]
    public bool allowCharging = false;

    private void Start()
    {
        chargerRenderer = GetComponentsInChildren<Renderer>(true);

        if (uiDocument != null)
        {
            chargerProgress = uiDocument.rootVisualElement.Q<ProgressBar>("ChargerProgress");
            if (chargerProgress != null)
            {
                chargerProgress.lowValue = 0f;
                chargerProgress.highValue = maxCharge;
                chargerProgress.value = currentCharge;
            }
        }
    }

    void Update()
    {
        // If charging was in progress but charger permission removed, stop charging
        if (!allowCharging && isCharging)
        {
            isCharging = false;
            Debug.Log("Charging interrupted (left charger area).");
        }

        // start charging when key is initially pressed and charging is allowed
        if (allowCharging && Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            isFullyCharged = false;
        }

        // continue charging while the key is held and charging permitted
        if (isCharging && Input.GetKey(KeyCode.Space))
        {
            currentCharge += chargeRate * Time.deltaTime;
            if (currentCharge >= maxCharge)
            {
                currentCharge = maxCharge;
                isFullyCharged = true;
                foreach (Renderer rend in chargerRenderer)
                {
                    rend.material = chargerOnMaterial; // Change material to indicate full charge
                }
                isCharging = false; // stop growth when full
                Debug.Log("Weapon fully charged");
                PlayChargedSound();
            }
        }

        ChargeBarFiller();
    }

    void ChargeBarFiller()
    {
        // Prefer UI Toolkit ProgressBar when available
        if (chargerProgress != null)
        {
            chargerProgress.value = currentCharge;
        }
    }

    public void Add(float additionalPoints)
    {
        if (currentCharge < maxCharge)
        {
            currentCharge += additionalPoints;
            if (currentCharge >= maxCharge)
            {
                currentCharge = maxCharge;
                isFullyCharged = true;
            }
        }
    }

    // Called by Charger to enable/disable charging permission
    public void SetAllowCharging(bool allow)
    {
        allowCharging = allow;
        if (!allow)
        {
            // stop any active charging immediately
            isCharging = false;
        }
    }

    // Public getter so other systems can check if the weapon is fully charged
    public bool IsFullyCharged => isFullyCharged;

    // Optional helper to drain/reset charge (not required but handy)
    public void ResetCharge()
    {
        currentCharge = 0f;
        isFullyCharged = false;
        isCharging = false;
        foreach (Renderer rend in chargerRenderer)
        {
            rend.material = chargerOffMaterial;
        }
        ChargeBarFiller();
    }

    public void PlayChargedSound()
    {
        Debug.Log("Playing charged sound");
        int randomIndex = Random.Range(0, chargedSounds.Count);
        audioSource.PlayOneShot(chargedSounds[randomIndex]);
    }
}