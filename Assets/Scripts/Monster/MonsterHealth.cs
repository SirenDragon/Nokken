using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MonsterHealth : MonoBehaviour
{
    [Tooltip("Maximum health of the monster.")]
    public int maxHealth = 3;

    [Tooltip("Optional reference to the MonsterMovement that controls the monster. If not set the script will find one in the scene.")]
    public MonsterMovement monsterMovement;

    [Header("UI (optional)")]
    [Tooltip("Assign an Image with Image.Type = Filled to show monster health.")]
    public Image healthBar;
    [Tooltip("Optional TextMeshProUGUI to show numeric HP (e.g. 2 / 3).")]
    public TextMeshProUGUI healthText;
    [Tooltip("Color for full health")]
    public Color fullColor = Color.green;
    [Tooltip("Color for low health")]
    public Color lowColor = Color.red;
    [Tooltip("When health <= this fraction, use lowColor")]
    [Range(0f, 1f)]
    public float lowThreshold = 0.33f;

    [Header("Win UI")]
    [Tooltip("Optional 'You Win' UI panel to enable when monster dies.")]
    public GameObject youWinPanel;
    [Tooltip("If true, time will be paused when player wins.")]
    public bool pauseGameOnWin = true;
    [Tooltip("If true, audio will be paused when player wins.")]
    public bool pauseAudioOnWin = true;
    [Tooltip("If true, LockedPlayerMovement components will be disabled on win.")]
    public bool disablePlayerMovementOnWin = true;

    [Header("Replay")]
    [Tooltip("Optional UI Button on the You Win panel that restarts the scene.")]
    public Button replayButton;
    [Tooltip("If >= 0, loads this build index. Otherwise reloads the active scene.")]
    public int replaySceneBuildIndex = -1;

    private int currentHealth;

    // store previous time settings so we can restore them if needed
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;

    void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);

        // auto-find if not assigned in inspector
        if (monsterMovement == null)
            monsterMovement = FindObjectOfType<MonsterMovement>();

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;

        UpdateHealthUI();

        // ensure win UI hidden at start
        if (youWinPanel != null)
            youWinPanel.SetActive(false);

        // wire replay button if assigned
        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayButtonClicked);
    }

    void OnDestroy()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);
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
        if (healthBar != null)
        {
            float fill = (maxHealth > 0) ? (float)currentHealth / (float)maxHealth : 0f;
            healthBar.fillAmount = Mathf.Clamp01(fill);

            // color lerp or thresholded color
            if (fill <= lowThreshold)
                healthBar.color = lowColor;
            else
                healthBar.color = fullColor;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Called when health reaches zero
    private void Die()
    {
        Debug.Log("MonsterHealth: Monster defeated (health reached zero).");

        // show You Win UI
        if (youWinPanel != null)
            youWinPanel.SetActive(true);

        // optional global effects (pause, audio, disable movement)
        if (pauseGameOnWin)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = previousFixedDeltaTime * Time.timeScale;
        }

        if (pauseAudioOnWin)
        {
            AudioListener.pause = true;
        }

        if (disablePlayerMovementOnWin)
        {
            var movers = FindObjectsOfType<LockedPlayerMovement>();
            foreach (var m in movers)
            {
                if (m != null)
                    m.enabled = false;
            }
        }

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

    // Called by the You Win panel button to replay the scene
    private void OnReplayButtonClicked()
    {
        // ensure the global time and audio are in a good state before reloading
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;

        int buildIndex = replaySceneBuildIndex >= 0 ? replaySceneBuildIndex : SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"MonsterHealth: Replay button pressed - loading scene build index {buildIndex}.");

        // immediate load (simple). Use LoadSceneAsync if you want a progress UI.
        SceneManager.LoadScene(buildIndex);
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