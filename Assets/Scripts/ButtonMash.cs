using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonMash : MonoBehaviour
{
    [Header("UI")]
    public GameObject qteCanvas; // Assign the QTE Canvas / Panel here
    public TextMeshProUGUI keyPrompt;
    public Image mashBar;

    // Events (cannot use [Header] on events)
    public event Action OnQTESuccess;
    public event Action OnQTEFail;

    [Header("Settings")]
    public PlayerFail playerFail;

    [Header("Optional integrations")]
    [Tooltip("Optional WeaponCharge reference to check for a fully charged weapon.")]
    public WeaponCharge weaponCharge;

    [Tooltip("Optional specific generator to check. If null the script checks if any generator in the scene is broken.")]
    public Generator targetGenerator;

    float currentValue = 0f;
    float maxValue = 100f;
    float decayRate = 25f;
    float failTimer = 7f;
    int CompletedMashes = 0;
    int RequiredMashes = 3;

    bool qteActive = false;

    KeyCode[] possibleKeys = { KeyCode.R, KeyCode.F, KeyCode.G };
    KeyCode currentKey;

    // Ultimate (spacebar spam) QTE
    bool ultimateQteActive = false;
    int ultimateCurrentCount = 0;
    [Tooltip("How many space presses are required for the ultimate QTE.")]
    public int ultimateRequiredCount = 20;
    [Tooltip("Time allowed to complete the ultimate QTE (seconds).")]
    public float ultimateFailTimer = 5f;

    // runtime remainder for the ultimate QTE timer (don't reuse the config field)
    private float ultimateTimerRemaining = 0f;

    void Start()
    {
        // Ensure UI is hidden at start
        if (qteCanvas != null)
            qteCanvas.SetActive(false);

        currentValue = 0;
        PickNewKey();
    }

    void Update()
    {
        if (!qteActive && !ultimateQteActive) return;

        if (ultimateQteActive)
        {
            ultimateTimerRemaining -= Time.deltaTime;
            if (ultimateTimerRemaining <= 0f)
            {
                PlayerDiedUltimate();
                return;
            }

            // spam spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ultimateCurrentCount++;
                // update mashBar to show progress
                if (mashBar != null)
                    mashBar.fillAmount = (float)ultimateCurrentCount / Mathf.Max(1, ultimateRequiredCount);
            }

            if (ultimateCurrentCount >= ultimateRequiredCount)
            {
                // success
                ultimateQteActive = false;
                if (qteCanvas != null)
                    qteCanvas.SetActive(false);

                Debug.Log("Ultimate QTE succeeded!");
                // Optionally drain the weapon so player can't reuse it immediately
                if (weaponCharge != null)
                    weaponCharge.ResetCharge();

                var profile = FindObjectOfType<UserProfileData>();
                if (profile != null)
                    profile.successfulAttacks++;

                // --- NEW: damage the monster by 1 on ultimate QTE success ---
                var monsterHealth = FindObjectOfType<MonsterHealth>();
                if (monsterHealth != null)
                {
                    monsterHealth.TakeDamage(1);
                }
                else
                {
                    Debug.LogWarning("ButtonMash: MonsterHealth not found in scene; cannot apply ultimate-QTE damage.");
                }

                OnQTESuccess?.Invoke();
            }

            return;
        }

        // Normal mash QTE logic
        failTimer -= Time.deltaTime;
        if (failTimer <= 0)
        {
            PlayerDied();
            return;
        }

        currentValue -= decayRate * Time.deltaTime;
        currentValue = Mathf.Clamp(currentValue, 0, maxValue);

        if (Input.GetKeyDown(currentKey))
        {
            Add(10f);
        }

        if (currentValue >= maxValue)
        {
            CompletedMashes++;
            Debug.Log($"Stage {CompletedMashes} completed!");

            if (CompletedMashes >= RequiredMashes)
            {
                // Debug the condition so you can see why ultimate QTE may not start
                bool genBroken = IsGeneratorBroken();
                bool weaponReady = weaponCharge != null && weaponCharge.IsFullyCharged;
                Debug.Log($"Checking ultimate QTE conditions: generatorBroken={genBroken}, weaponChargeAssigned={(weaponCharge != null)}, weaponFullyCharged={weaponReady}");

                // Check for ultimate condition: generator off AND weapon charged
                if (genBroken && weaponReady)
                {
                    // start the ultimate spacebar-spam QTE instead of immediate success
                    StartUltimateQTE();
                }
                else
                {
                    decayRate = 0f;
                    qteActive = false;
                    Debug.Log("Button mash challenge completed successfully!");

                    // hide UI
                    if (qteCanvas != null)
                        qteCanvas.SetActive(false);

                    OnQTESuccess?.Invoke();
                }
            }
            else
            {
                currentValue = 0;
                PickNewKey();
                failTimer = 7f;
            }
        }

        MashBarFiller();
    }

    void MashBarFiller()
    {
        if (mashBar != null)
            mashBar.fillAmount = currentValue / maxValue;
    }

    public void Add(float additionalPoints)
    {
        if (currentValue < maxValue)
        {
            currentValue += additionalPoints;
            if (currentValue > maxValue) currentValue = maxValue;
        }
    }

    void PickNewKey()
    {
        currentKey = possibleKeys[UnityEngine.Random.Range(0, possibleKeys.Length)];

        if (keyPrompt != null)
        {
            keyPrompt.text = currentKey.ToString();
        }
    }

    void PlayerDied()
    {
        if (!qteActive) return;
        qteActive = false;
        Debug.Log("Player has failed the button mash challenge.");

        // hide UI
        if (qteCanvas != null)
            qteCanvas.SetActive(false);

        OnQTEFail?.Invoke();

        if (playerFail != null)
        {
            playerFail.HandleGameOver("Failed QTE");
        }
        else
        {
            Debug.LogWarning("PlayerFail handler not assigned; game over was not routed to PlayerFail.");
        }
    }

    void PlayerDiedUltimate()
    {
        if (!ultimateQteActive) return;
        ultimateQteActive = false;
        Debug.Log("Player failed the ultimate spam QTE.");

        // hide UI
        if (qteCanvas != null)
            qteCanvas.SetActive(false);

        OnQTEFail?.Invoke();

        if (playerFail != null)
        {
            playerFail.HandleGameOver("Failed ultimate QTE");
        }
        else
        {
            Debug.LogWarning("PlayerFail handler not assigned; game over was not routed to PlayerFail.");
        }
    }

    public void StartQTE()
    {
        // show UI
        if (qteCanvas != null)
            qteCanvas.SetActive(true);
        else
            Debug.LogWarning("QTE Canvas not assigned on ButtonMash; UI will not be shown.");

        qteActive = true;
        ultimateQteActive = false;

        currentValue = 0;
        CompletedMashes = 0;
        decayRate = 25f;
        failTimer = 7f;
        PickNewKey();
        Debug.Log("Button mash challenge started!");
    }

    void StartUltimateQTE()
    {
        Debug.Log("Starting ultimate spacebar QTE (weapon charged + generator off).");

        // configure UI for ultimate QTE
        if (qteCanvas != null)
            qteCanvas.SetActive(true);

        if (keyPrompt != null)
            keyPrompt.text = "SPACE";

        if (mashBar != null)
            mashBar.fillAmount = 0f;

        // set runtime timer and counters
        ultimateTimerRemaining = ultimateFailTimer;
        ultimateCurrentCount = 0;
        ultimateQteActive = true;
        qteActive = false;
    }

    bool IsGeneratorBroken()
    {
        if (targetGenerator != null)
            return targetGenerator.isBroken;

        // fallback: any generator broken in the scene
        var gens = FindObjectsOfType<Generator>();
        foreach (var g in gens)
        {
            if (g != null && g.isBroken) return true;
        }
        return false;
    }

    void OnDisable()
    {
        if (qteActive)
        {
            qteActive = false;
            Debug.Log("Button mash challenge stopped externally.");

            // hide UI
            if (qteCanvas != null)
                qteCanvas.SetActive(false);

            OnQTEFail?.Invoke();
        }

        if (ultimateQteActive)
        {
            ultimateQteActive = false;
            Debug.Log("Ultimate QTE stopped externally.");

            if (qteCanvas != null)
                qteCanvas.SetActive(false);

            OnQTEFail?.Invoke();
        }
    }
}