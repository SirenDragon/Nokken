using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TMPro;

public class MonsterHealth : MonoBehaviour
{
    [SerializeField] Score score;
    [SerializeField] private UIDocument uiDocument;
    private ProgressBar monsterHealth;

    [Tooltip("Maximum health of the monster.")]
    public int maxHealth = 3;

    [Tooltip("Optional reference to the MonsterMovement that controls the monster. If not set the script will find one in the scene.")]
    public MonsterMovement monsterMovement;

    private int currentHealth;

    // store previous time settings so we can restore them if needed
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;

    private void Start()
    {
        if (uiDocument != null)
        {
            monsterHealth = uiDocument.rootVisualElement.Q<ProgressBar>("MonsterHealth");
            if (monsterHealth != null)
            {
                // Set the progress bar range to use integer health directly
                monsterHealth.lowValue = 0f;
                monsterHealth.highValue = maxHealth;
                monsterHealth.value = currentHealth;
                monsterHealth.visible = true; // keep visible if you want; hide elsewhere if needed
            }
        }
        else
        {
            Debug.LogWarning($"MonsterHealth on '{name}' has no UIDocument assigned; MonsterHealth ProgressBar will not be updated.");
        }
    }

    void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);

        // auto-find if not assigned in inspector
        if (monsterMovement == null)
            monsterMovement = FindObjectOfType<MonsterMovement>();

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;

        UpdateHealthUI();
    }

    // Reduce health by amount (default 1). If health reaches zero, trigger Die().
    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"MonsterHealth: Took {amount} damage. Remaining HP = {currentHealth}/{maxHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    // Optional: heal the monster (useful for testing)
    public void Heal(int amount = 1)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        // Update UI Toolkit ProgressBar (uses integer range set in Start)
        if (monsterHealth != null)
        {
            // If you set highValue = maxHealth, assign integer currentHealth directly
            monsterHealth.value = Mathf.Clamp(currentHealth, (int)monsterHealth.lowValue, (int)monsterHealth.highValue);
        }
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Called when health reaches zero
    private void Die()
    {
        Debug.Log("MonsterHealth: Monster defeated (health reached zero).");
        //Win Game
        score.HandleWin();

        // notify MonsterMovement (caught/respawn behaviour) if present
        if (monsterMovement != null)
        {
            monsterMovement.SendToCaughtArea();
        }
        else
        {
            // fallback: try to find an instance at runtime, otherwise disable the GameObject
            var mm = FindObjectOfType<MonsterMovement>();
            if (mm != null)
                mm.SendToCaughtArea();
            else
            {
                Debug.LogWarning("MonsterHealth: MonsterMovement not found; disabling monster GameObject as fallback.");
                gameObject.SetActive(false);
            }
        }
    }

    void OnValidate()
    {
        if (maxHealth < 1) maxHealth = 1;
        // update editor preview
        if (Application.isPlaying == false)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();
        }
    }
}