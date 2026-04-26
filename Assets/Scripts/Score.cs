using TMPro; // Required for TextMeshPro components
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Score : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioMixer audioMixer;

    private VisualElement gameWon;
    private VisualElement visualUI;

    private Button restartGameWonButton;
    private Button quitGameWonButton;


    [Header("Box Score Settings")]
    [Tooltip("The starting score for boxes.")]
    public int startingBoxScore = 5; // Initial score for boxes

    private int currentBoxScore; // Tracks the current box score

    [Tooltip("The TextMeshProUGUI component to display the box score.")]
    public TextMeshProUGUI boxScoreText; // Reference to the TextMeshProUGUI component for boxes

    [Header("Generator Score Settings")]
    [Tooltip("The starting score for the generator.")]
    public int startingGeneratorScore = 1; // Initial score for the generator

    private int currentGeneratorScore; // Tracks the current generator score

    [Tooltip("The TextMeshProUGUI component to display the generator score.")]
    public TextMeshProUGUI generatorScoreText; // Reference to the TextMeshProUGUI component for the generator

    [Header("Timer Settings")]
    [Tooltip("The TextMeshProUGUI component to display the timer.")]
    public TextMeshProUGUI timerText; // Reference to the TextMeshProUGUI component for the timer

    [Tooltip("The starting time for the countdown timer in seconds.")]
    public float startingTime = 180f; // Timer starts at 60 seconds

    private float currentTime; // Tracks the current time for the countdown

    private bool isTimerPaused = false; // Tracks whether the timer is paused

    [Header("Win UI")]
    [Tooltip("If true, pause Unity time (Time.timeScale = 0) when the player wins.")]
    public bool pauseGameOnWin = true;
    [Tooltip("If true, pause audio via AudioListener.pause when the player wins.")]
    public bool pauseAudioOnWin = true;

    [Header("Optional GameOver handler")]
    [Tooltip("Optional reference to PlayerFail to handle losing the game when boxes reach 0. If not set, the script will try to find one automatically.")]
    public PlayerFail playerFail;

      void Start()
    {
        gameWon = uiDocument.rootVisualElement.Q<VisualElement>("GameWon");

        restartGameWonButton = uiDocument.rootVisualElement.Q<Button>("RestartGameWonButton");
        quitGameWonButton = uiDocument.rootVisualElement.Q<Button>("QuitGameWonButton");
        visualUI = uiDocument.rootVisualElement.Q<VisualElement>("VisualUI");


        restartGameWonButton.clicked += OnRestartGameWonClicked;
        quitGameWonButton.clicked += OnQuitGameWonClicked;

        // Initialize scores
        currentBoxScore = startingBoxScore;
        currentGeneratorScore = startingGeneratorScore;

        // Initialize timer
        currentTime = startingTime;

        // Update UI
        UpdateBoxScoreText();
        UpdateGeneratorScoreText();
        UpdateTimerText();

        // try to auto-find PlayerFail if not assigned
        if (playerFail == null)
            playerFail = FindObjectOfType<PlayerFail>();
    }

    void Update()
    {
        // Update the timer only if it's not paused
        if (!isTimerPaused && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerText();

            // Clamp the timer to 0 to avoid negative values
            if (currentTime <= 0)
            {
                currentTime = 0;
                HandleTimerEnd();
            }
        }
    }

    public int GetCurrentGeneratorScore()
    {
        return currentGeneratorScore;
    }


    // Decreases the box score by 1
    public void DecreaseBoxScore()
    {
        currentBoxScore = Mathf.Max(0, currentBoxScore - 1);
        UpdateBoxScoreText();

        if (currentBoxScore <= 0)
            HandleBoxesDepleted();
    }

    // Increases the box score by 1
    public void IncreaseBoxScore()
    {
        currentBoxScore++;
        UpdateBoxScoreText();
    }

    // Updates the UI text to display the current box score
    private void UpdateBoxScoreText()
    {
        if (boxScoreText != null)
        {
            boxScoreText.text = $"Boxes: {currentBoxScore}";
        }
        else
        {
            Debug.LogError("BoxScoreText UI element is not assigned!");
        }
    }

    // Called when boxes hit zero
    private void HandleBoxesDepleted()
    {
        Debug.Log("Boxes depleted -> Game Over");

        // Prefer centralized PlayerFail handler
        if (playerFail == null)
            playerFail = FindObjectOfType<PlayerFail>();

        if (playerFail != null)
        {
            playerFail.HandleGameOver("Boxes depleted");
        }
        else
        {
            // Fallback: show win UI? no — log warning so you can wire PlayerFail
            Debug.LogWarning("Score: PlayerFail not found. Assign PlayerFail in Score inspector to handle Game Over.");
        }
    }

    // Decreases the generator score by 1
    public void DecreaseGeneratorScore()
    {
        currentGeneratorScore--;
        UpdateGeneratorScoreText();

        // Check if the generator score has reached 0
        if (currentGeneratorScore <= 0)
        {
            PauseTimer(); // Pause the timer when the generator score is 0
        }
    }

    // Increases the generator score by 1
    public void IncreaseGeneratorScore()
    {
        currentGeneratorScore++;
        UpdateGeneratorScoreText();

        // Resume the timer if the generator score goes back to 1 or higher
        if (currentGeneratorScore == 1)
        {
            ResumeTimer();
        }
    }

    // Pauses the timer
    private void PauseTimer()
    {
        isTimerPaused = true;
        Debug.Log("Timer paused because the generator score is 0.");
    }

    // Resumes the timer
    private void ResumeTimer()
    {
        isTimerPaused = false;
        Debug.Log("Timer resumed because the generator score is 1 or higher.");
    }

    // Updates the UI text to display the current generator score
    private void UpdateGeneratorScoreText()
    {
        if (generatorScoreText != null)
        {
            generatorScoreText.text = $"Generator: {currentGeneratorScore}";
        }
        else
        {
            Debug.LogError("GeneratorScoreText UI element is not assigned!");
        }
    }

    // Updates the UI text to display the current timer
    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60); // Get minutes
            float seconds = currentTime % 60; // Get seconds
            timerText.text = $"Time: {minutes:00}:{seconds:00.000}"; // Format as MM:SS.mmm
        }
        else
        {
            Debug.LogError("TimerText UI element is not assigned!");
        }
    }

    // Handles the end of the timer
    private void HandleTimerEnd()
    {
        Debug.Log("Time's up! You win.");
        // Show win UI and apply global win behaviour
        HandleWin();
    }

    void OnRestartGameWonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("SampleScene");
    }

    void OnQuitGameWonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("menu");
    }

    private void HandleWin()
    {
        // stop the timer
        isTimerPaused = true;
        visualUI.style.display = DisplayStyle.None;
        Time.timeScale = 0f;
        gameWon.visible = true;
    }
}