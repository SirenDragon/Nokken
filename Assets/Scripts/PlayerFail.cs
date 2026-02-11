using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerFail : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Assign the Game Over UI panel (will be enabled when game over is triggered).")]
    public GameObject gameOverPanel;

    [Header("Behavior")]
    [Tooltip("If true, LockedPlayerMovement components will be disabled to prevent movement.")]
    public bool disablePlayerMovementComponents = true;

    [Tooltip("Pause Unity time (Time.timeScale = 0) when game over is triggered.")]
    public bool pauseGameOnFail = true;

    [Tooltip("Also pause audio via AudioListener.pause")]
    public bool pauseAudioOnFail = true;

    [Tooltip("If true, disables any FlashlightController instances when game over is triggered.")]
    public bool disableFlashlightOnFail = true;

    bool isGameOver;

    // store previous time settings so we can restore them
    float previousTimeScale = 1f;
    float previousFixedDeltaTime = 0.02f;

    // store previous flashlight states so they can be restored later
    private class SavedFlashlightState
    {
        public bool gameObjectActive;
        public bool flashlightActive;
    }
    private readonly Dictionary<FlashlightController, SavedFlashlightState> previousFlashlightStates = new Dictionary<FlashlightController, SavedFlashlightState>();

    void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
    }

    void OnEnable()
    {
        // ensure we reset game-over state when any new scene loads
        SceneManager.sceneLoaded += OnSceneLoaded_ResetIfNeeded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_ResetIfNeeded;
    }

    private void OnSceneLoaded_ResetIfNeeded(Scene scene, LoadSceneMode mode)
    {
        // Reset to a playable state after a scene load in case this object persisted
        Debug.Log("PlayerFail: Scene loaded - ensuring game-over state is reset.");
        ResetGameOver();
    }

    // Make this public so other scripts can call it: PlayerFailInstance.HandleGameOver(...)
    public void HandleGameOver(string reason = null)
    {
        if (isGameOver) return;
        isGameOver = true;

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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.LogWarning("PlayerFail: gameOverPanel not assigned.");

        if (disablePlayerMovementComponents)
        {
            var movementComponents = FindObjectsOfType<LockedPlayerMovement>();
            foreach (var component in movementComponents)
            {
                if (component != null)
                    component.enabled = false;
            }
        }

        if (pauseGameOnFail)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = previousFixedDeltaTime * Time.timeScale;
        }

        if (pauseAudioOnFail)
        {
            AudioListener.pause = true;
        }

        if (disableFlashlightOnFail)
        {
            var flashlights = FindObjectsOfType<FlashlightController>();
            previousFlashlightStates.Clear();
            foreach (var f in flashlights)
            {
                if (f == null) continue;

                // record previous states (gameObject active + controller's logical active flag)
                bool prevGoActive = false;
                if (f.flashlight != null)
                    prevGoActive = f.flashlight.gameObject.activeSelf;

                previousFlashlightStates[f] = new SavedFlashlightState
                {
                    gameObjectActive = prevGoActive,
                    flashlightActive = f.flashlightActive
                };

                // Disable the flashlight GameObject so it is fully off in the scene
                if (f.flashlight != null)
                    f.flashlight.gameObject.SetActive(false);

                // Also set the controller's logical flag to false so its logic matches the visual state
                f.flashlightActive = false;
            }
        }
    }

    // Call this to restore previously stored flashlight states (e.g., on restart)
    public void RestoreFlashlights()
    {
        foreach (var kv in previousFlashlightStates)
        {
            var controller = kv.Key;
            var saved = kv.Value;
            if (controller == null || saved == null) continue;

            // restore the controller's logical state
            controller.flashlightActive = saved.flashlightActive;

            // restore the flashlight GameObject active state
            if (controller.flashlight != null)
            {
                controller.flashlight.gameObject.SetActive(saved.gameObjectActive);

                // if enabled, ensure Light intensity reflects controller state (match existing FlashlightController defaults)
                var light = controller.flashlight.GetComponent<Light>();
                if (light != null)
                    light.intensity = controller.flashlightActive ? 15f : 0f;
            }
        }

        previousFlashlightStates.Clear();
    }

    // Public helper: fully reset the game-over state (useful after scene reload)
    public void ResetGameOver()
    {
        if (!isGameOver)
        {
            // still ensure global systems are unpaused
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            AudioListener.pause = false;
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            return;
        }

        Debug.Log("PlayerFail: Resetting GameOver state.");

        isGameOver = false;

        // restore time and physics timestep to sensible defaults
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // unpause audio
        AudioListener.pause = false;

        // re-enable movement components if we disabled them previously
        if (disablePlayerMovementComponents)
        {
            var movementComponents = FindObjectsOfType<LockedPlayerMovement>();
            foreach (var component in movementComponents)
            {
                if (component != null)
                    component.enabled = true;
            }
        }

        // hide the Game Over UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // restore flashlights if we saved them earlier
        RestoreFlashlights();
    }
}