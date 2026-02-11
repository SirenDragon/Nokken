using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class WeaponCharge : MonoBehaviour
{
    public Image chargeBar;

    float currentCharge = 0f;
    float maxCharge = 100f;
    float chargeRate = 10f;

    bool isCharging = false;
    bool isFullyCharged = false;

    // Only allow charging when a Charger enables it
    [HideInInspector]
    public bool allowCharging = false;

    [Header("Colors")]
    [Tooltip("Color used when the charge bar is full.")]
    public Color fullChargeColor = Color.cyan;

    // stored initial color to restore when not full
    private Color normalChargeColor = Color.white;

    void Awake()
    {
        if (chargeBar != null)
            normalChargeColor = chargeBar.color;
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
                isCharging = false; // stop growth when full
                Debug.Log("Weapon fully charged");
            }
        }

        ChargeBarFiller();
    }

    void ChargeBarFiller()
    {
        if (chargeBar == null) return;

        chargeBar.fillAmount = currentCharge / maxCharge;

        // change color to cyan when fully charged, revert when not
        if (currentCharge >= maxCharge)
            chargeBar.color = fullChargeColor;
        else
            chargeBar.color = normalChargeColor;
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
        ChargeBarFiller();
    }
}