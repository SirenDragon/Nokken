using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using UnityEditor.SettingsManagement;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioMixer audioMixer;

    private VisualElement pauseMenu;
    private VisualElement settingsMenu;

    private Slider MusicSlider;

    private Button resumeButton;
    private Button settingsButton;
    private Button quitButton;
    private Button settingsBackButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenu = uiDocument.rootVisualElement.Q<VisualElement>("Pause");
        settingsMenu = uiDocument.rootVisualElement.Q<VisualElement>("Settings");

        MusicSlider = uiDocument.rootVisualElement.Q<Slider>("MusicSlider");

        resumeButton = uiDocument.rootVisualElement.Q<Button>("ResumeButton");
        settingsButton = uiDocument.rootVisualElement.Q<Button>("SettingsButton");
        quitButton = uiDocument.rootVisualElement.Q<Button>("QuitButton");
        settingsBackButton = uiDocument.rootVisualElement.Q<Button>("SettingsBackButton");

        MusicSlider.RegisterCallback<ChangeEvent<float>>(OnMusicSliderChanged);

        resumeButton.clicked += OnResumeClicked;
        settingsButton.clicked += OnSettingsButtonClicked;
        quitButton.clicked += OnQuitClicked;
        settingsBackButton.clicked += OnSettingsBackClicked;
    }

    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            pauseMenu.visible = true;
            Time.timeScale = 0f;
            audioMixer.SetFloat("lowPass", 200f);
        }
    }

    private void OnResumeClicked()
    {
        pauseMenu.visible = false;
        Time.timeScale = 1f;
        audioMixer.SetFloat("lowPass", 5000f);
    }

    private void OnSettingsButtonClicked()
    {
        pauseMenu.visible = false;
        settingsMenu.visible = true;
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("menu");
    }

    private void OnSettingsBackClicked()
    {
        pauseMenu.visible = true;
        settingsMenu.visible = false;
    }

    private void OnMusicSliderChanged(ChangeEvent<float> evt)
    {
        float sliderValue = evt.newValue;

        // Convert 0�100 to 0�1
        float normalized = sliderValue / 100f;

        // Avoid log(0) common bug apparently
        normalized = Mathf.Clamp(normalized, 0.0001f, 1f);

        // Convert to decibels
        float dB = Mathf.Log10(normalized) * 20f;

        audioMixer.SetFloat("musicVol", dB);
    }
}
