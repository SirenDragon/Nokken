using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerFail : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private LockedPlayerMovement lockedPlayerMovement;

    private VisualElement gameOver;
    private VisualElement visualUI;

    private Button restartGameOverButton;
    private Button quitGameOverButton;

    public bool pauseGameOnFail = true;

    bool isGameOver;

    private void Start()
    {
        gameOver = uiDocument.rootVisualElement.Q<VisualElement>("GameOver");

        restartGameOverButton = uiDocument.rootVisualElement.Q<Button>("RestartGameOverButton");
        quitGameOverButton = uiDocument.rootVisualElement.Q<Button>("QuitGameOverButton");
        visualUI = uiDocument.rootVisualElement.Q<VisualElement>("VisualUI");


        restartGameOverButton.clicked += OnRestartGameOverClicked;
        quitGameOverButton.clicked += OnQuitGameOverClicked;
    }

    // Make this public so other scripts can call it: PlayerFailInstance.HandleGameOver(...)
    public void HandleGameOver(string reason = null)
    {
        if (isGameOver) return;
        isGameOver = true;
        visualUI.style.display = DisplayStyle.None;
        Time.timeScale = 0f;
        gameOver.visible = true;


        // --- TRACKING: increment death count and monster-attack count when appropriate ---
        var profile = FindObjectOfType<UserProfileData>();
        if (profile != null)
        {
            profile.deaths++;

            // detect monster-caused deaths (conservative check on reason text)
            if (!string.IsNullOrEmpty(reason) && reason.ToLower().Contains("monster"))
                profile.timesAttacked++;
        }

        Debug.Log($"GameOver triggered. Reason: {reason}");
    }

    void OnRestartGameOverClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("SampleScene");
    }

    void OnQuitGameOverClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("menu");
    }
}