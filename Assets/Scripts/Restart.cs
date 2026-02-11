using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class Restart : MonoBehaviour
{
    [Tooltip("UI Button to wire. Assign a Button or wire the RestartScene() method in the Button's On Click() inspector.")]
    public Button restartButton;

    [Tooltip("Build index of the scene to load. Set this using the numbers shown in __File > Build Settings...__")]
    public int sceneBuildIndex = 0;

    void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);
    }

    void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartScene);
    }

    // Call this from a UI Button OnClick() handler
    public void RestartScene()
    {
        // validate index before starting
        int buildCount = SceneManager.sceneCountInBuildSettings;
        if (sceneBuildIndex < 0 || sceneBuildIndex >= buildCount)
        {
            Debug.LogError($"Invalid sceneBuildIndex {sceneBuildIndex}. Scenes in build: {buildCount}. Set the index in __File > Build Settings...__.");
            return;
        }

        StartCoroutine(ReloadAsync(sceneBuildIndex));
    }

    private IEnumerator ReloadAsync(int buildIndex)
    {
        if (restartButton != null)
            restartButton.interactable = false;

        var async = SceneManager.LoadSceneAsync(buildIndex);
        while (!async.isDone)
            yield return null;

        // Ensure the game is unpaused and cursor is restored for gameplay
        Time.timeScale = 1f;
        // Restore a reasonable fixed timestep for physics (default is 0.02)
        Time.fixedDeltaTime = 0.02f;

        // Unpause audio if it was paused
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Clear any selected UI object so it doesn't steal input
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // Re-enable player movement components in case PlayerFail disabled them
        var movers = FindObjectsOfType<LockedPlayerMovement>();
        foreach (var m in movers)
        {
            if (m != null)
                m.enabled = true;
        }

        // If there is a persistent PlayerFail instance, explicitly reset its state
        var pf = FindObjectOfType<PlayerFail>();
        if (pf != null)
        {
            pf.ResetGameOver();
        }
    }

    // Handy context menu to restart from the component in the editor
    [ContextMenu("Restart Scene")]
    private void RestartSceneContext() => RestartScene();
}