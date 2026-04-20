using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement pauseMenu;

    private Button resumeButton;
    private Button quitButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenu = uiDocument.rootVisualElement.Q<VisualElement>("Pause");

        resumeButton = uiDocument.rootVisualElement.Q<Button>("ResumeButton");
        quitButton = uiDocument.rootVisualElement.Q<Button>("QuitButton");

        resumeButton.clicked += OnResumeClicked;
        quitButton.clicked += OnQuitClicked;
    }

    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            pauseMenu.visible = true;
            Time.timeScale = 0f;
        }
    }

    private void OnResumeClicked()
    {
        pauseMenu.visible = false;
        Time.timeScale = 1f;
    }

    private void OnQuitClicked()
    {
        SceneManager.LoadSceneAsync("menu");
    }
}
