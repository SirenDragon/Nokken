using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public enum MonsterState
    {
        ChoosingRoom,
        MovingThroughStages,
        FinalStageCountdown,
        BreakingBox,
        Caught,
        Respawning
    }

    [Header("Monster Settings")]
    [Tooltip("The monster GameObject to control.")]
    public GameObject monster;

    [Tooltip("List of rooms, each containing stage positions.")]
    public List<Room> rooms;

    [Header("Caught Area")]
    [Tooltip("The position where the monster is sent when it gets caught.")]
    public Transform caughtArea;

    [Header("Player Movement Reference")]
    [Tooltip("Reference to the LockedPlayerMovement script.")]
    public LockedPlayerMovement playerMovement;

    [Header("Button Mash Reference")]
    [Tooltip("Reference to the ButtonMash script.")]
    public ButtonMash buttonMash;

    [Header("Movement Settings")]
    [Tooltip("Time interval (in seconds) between stage movements or room picks.")]
    public float stageInterval = 5f;

    [Tooltip("Time to hold on the final stage before causing game over.")]
    public float finalStageHoldTime = 7f;

    [Tooltip("How long the monster must be spotted to be sent to caught area.")]
    public float spottedTimeout = 3f;

    [Tooltip("Time to wait before respawning after being caught.")]
    public float respawnDelay = 10f;

    [Header("References")]
    public FlashlightController flashlightController;
    public MonsterAudio monsterAudio;
    public MonsterStages monsterStages;
    public Score scoreManager;

    [Header("Game Over / Fail Handler")]
    [Tooltip("Optional reference to the PlayerFail handler. If not assigned the script will try to find one at Start.")]
    public PlayerFail playerFail;

    // runtime
    [SerializeField, Tooltip("The room index the player is currently in (for debugging purposes).")]
    private int playerRoomIndex = -1;

    public int currentRoomIndex = -1;
    private int currentStageIndex = 0;

    private MonsterState currentState = MonsterState.ChoosingRoom;
    private float stateTimer = 0f;
    private float spottedTime = 0f;

    // convenience flag preserved from original behavior
    public bool isMovingThroughStages = false;

    // --- QTE pause support ---
    private bool pausedForQTE = false;

    void Start()
    {
        // try to auto-find referenced systems if not assigned in inspector
        if (flashlightController == null)
            flashlightController = Object.FindAnyObjectByType<FlashlightController>();

        if (playerFail == null)
            playerFail = Object.FindAnyObjectByType<PlayerFail>();

        if (scoreManager == null)
            scoreManager = Object.FindAnyObjectByType<Score>();

        ChangeState(MonsterState.ChoosingRoom);
    }

    void Update()
    {
        // If paused by a QTE, do not progress behavior (monster stops moving)
        if (pausedForQTE) return;

        // update player room index if available
        if (playerMovement != null)
            playerRoomIndex = playerMovement.CurrentRoomIndex;

        // common spotted handling
        HandleSpottedLogic();

        // per-state tick
        stateTimer += Time.deltaTime;
        switch (currentState)
        {
            case MonsterState.ChoosingRoom:
                UpdateChoosingRoom();
                break;
            case MonsterState.MovingThroughStages:
                UpdateMovingThroughStages();
                break;
            case MonsterState.FinalStageCountdown:
                UpdateFinalStageCountdown();
                break;
            case MonsterState.BreakingBox:
                UpdateBreakingBox();
                break;
            case MonsterState.Caught:
                // caught handled on enter
                break;
            case MonsterState.Respawning:
                UpdateRespawning();
                break;
        }
    }

    void ChangeState(MonsterState newState)
    {
        // Exit actions for certain states
        switch (currentState)
        {
            case MonsterState.MovingThroughStages:
                isMovingThroughStages = false;
                break;
        }

        // Enter new state
        currentState = newState;
        stateTimer = 0f;

        switch (currentState)
        {
            case MonsterState.ChoosingRoom:
                EnterChoosingRoom();
                break;
            case MonsterState.MovingThroughStages:
                EnterMovingThroughStages();
                break;
            case MonsterState.FinalStageCountdown:
                EnterFinalStageCountdown();
                break;
            case MonsterState.BreakingBox:
                EnterBreakingBox();
                break;
            case MonsterState.Caught:
                EnterCaught();
                break;
            case MonsterState.Respawning:
                EnterRespawning();
                break;
        }
    }

    // ---------------- State: ChoosingRoom ----------------
    void EnterChoosingRoom()
    {
        // wait at least stageInterval before acting; logic in UpdateChoosingRoom
    }

    void UpdateChoosingRoom()
    {
        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogError("No rooms available for the monster to pick!");
            return;
        }

        if (stateTimer < stageInterval) return;

        // pick a random room
        int randomRoomIndex = Random.Range(0, rooms.Count);
        Room selectedRoom = rooms[randomRoomIndex];
        currentRoomIndex = selectedRoom.roomIndex;
        Debug.Log($"Monster picked Room {selectedRoom.roomName} (Index: {currentRoomIndex}).");

        // choose behavior depending on generator state
        if (scoreManager != null && scoreManager.GetCurrentGeneratorScore() == 0)
        {
            // generator broken -> focus on player
            if (currentRoomIndex == playerRoomIndex)
            {
                ChangeState(MonsterState.MovingThroughStages);
            }
            else
            {
                // pick again after interval
                stateTimer = 0f;
            }
        }
        else
        {
            // generator ok -> either attack player or break boxes
            if (currentRoomIndex == playerRoomIndex)
            {
                ChangeState(MonsterState.MovingThroughStages);
            }
            else
            {
                bool boxBroken = BreakRandomBoxInRoom(currentRoomIndex);
                if (boxBroken)
                {
                    // after breaking a box wait then choose again
                    ChangeState(MonsterState.BreakingBox);
                }
                else
                {
                    // no box to break, pick another room after interval
                    stateTimer = 0f;
                }

                // play ambient occasionally
                if (monsterAudio != null && monsterAudio.ambientSounds != null && monsterAudio.ambientSounds.Count > 0)
                {
                    if (Random.value < 0.5f)
                    {
                        monsterAudio.PlayAmbientSound();
                        Debug.Log("Playing ambient sound.");
                    }
                }
            }
        }
    }

    // ---------------- State: MovingThroughStages ----------------
    void EnterMovingThroughStages()
    {
        isMovingThroughStages = true;
        currentStageIndex = 0;
        MoveToStageIndex(currentStageIndex);
    }

    void UpdateMovingThroughStages()
    {
        if (rooms == null || rooms.Count == 0) return;

        Room room = rooms.Find(r => r.roomIndex == currentRoomIndex);
        if (room == null || room.stagePositions.Count == 0)
        {
            Debug.LogError($"No valid stages found for Room index {currentRoomIndex}!");
            ChangeState(MonsterState.ChoosingRoom);
            return;
        }

        // if we are at final stage, transition to countdown
        if (currentStageIndex >= room.stagePositions.Count - 1)
        {
            ChangeState(MonsterState.FinalStageCountdown);
            return;
        }

        // move to next stage after interval
        if (stateTimer >= stageInterval)
        {
            currentStageIndex++;
            MoveToStageIndex(currentStageIndex);
            stateTimer = 0f;
        }
    }

    void MoveToStageIndex(int index)
    {
        var room = rooms.Find(r => r.roomIndex == currentRoomIndex);
        if (room == null) return;
        if (index < 0 || index >= room.stagePositions.Count) return;

        Transform stage = room.stagePositions[index];
        if (stage == null) return;

        monster.transform.position = stage.position;
        Debug.Log($"Monster moved to Stage {index} at Position: {stage.position}");

        // play stage move sound
        if (monsterAudio != null)
            monsterAudio.PlayStageMoveSound();

        // set animation stage if provided
        if (monsterStages != null)
            monsterStages.SetStage(index);
    }

    // ---------------- State: FinalStageCountdown ----------------
    void EnterFinalStageCountdown()
    {
        stateTimer = 0f;
        Debug.Log("Monster reached the final stage. Starting final stage countdown...");
    }

    void UpdateFinalStageCountdown()
    {
        // If player is spotted by flashlight for long enough handled elsewhere (HandleSpottedLogic)
        // Ensure monster is still at the expected final stage position
        var room = rooms.Find(r => r.roomIndex == currentRoomIndex);
        if (room == null) { ChangeState(MonsterState.ChoosingRoom); return; }

        Transform finalStage = room.stagePositions.Count > 0 ? room.stagePositions[^1] : null;
        if (finalStage == null) { ChangeState(MonsterState.ChoosingRoom); return; }

        if (monster.transform.position != finalStage.position)
        {
            Debug.Log("Monster left the final stage or was moved/frozen. Countdown stopped.");
            ChangeState(MonsterState.ChoosingRoom);
            return;
        }

        if (stateTimer >= finalStageHoldTime)
        {
            Debug.Log("Final stage hold time reached -> Game Over");
            // delegate to PlayerFail
            if (playerFail != null)
                playerFail.HandleGameOver("Killed by monster");
            else
                Debug.LogWarning("PlayerFail not assigned; implement fallback handling (e.g., reload scene or show UI).");

            ChangeState(MonsterState.ChoosingRoom);
        }
    }

    // ---------------- State: BreakingBox ----------------
    void EnterBreakingBox()
    {
        // simple behaviour: wait one stageInterval then go back to choosing room
        stateTimer = 0f;
    }

    void UpdateBreakingBox()
    {
        if (stateTimer >= stageInterval)
        {
            ChangeState(MonsterState.ChoosingRoom);
        }
    }

    // ---------------- State: Caught ----------------
    void EnterCaught()
    {
        // Move monster to caught area, play sound and then respawn after delay
        if (caughtArea != null && monster != null)
        {
            monster.transform.position = caughtArea.position;
            Debug.Log("Monster sent to the caught area.");

            if (monsterAudio != null)
                monsterAudio.PlayCaughtSound();
        }
        else
        {
            Debug.LogError("Caught area is not assigned!");
        }

        ChangeState(MonsterState.Respawning);
    }

    // ---------------- State: Respawning ----------------
    void EnterRespawning()
    {
        stateTimer = 0f;
    }

    void UpdateRespawning()
    {
        if (stateTimer >= respawnDelay)
        {
            Debug.Log("Monster has respawned and is resuming behavior.");
            ChangeState(MonsterState.ChoosingRoom);
        }
    }

    // ---------------- Helpers & Public API ----------------
    private void HandleSpottedLogic()
    {
        if (flashlightController != null && flashlightController.IsSpotted)
        {
            spottedTime += Time.deltaTime;
            if (spottedTime >= spottedTimeout)
            {
                // send to caught area immediately
                Debug.Log("Monster spotted for long enough -> sending to caught area.");
                SendToCaughtArea();
                spottedTime = 0f;
                isMovingThroughStages = false;
            }
        }
        else
        {
            spottedTime = 0f;
        }
    }

    // Public: triggered when monster should be sent to the caught area (e.g., by player actions)
    public void SendToCaughtArea()
    {
        // Ensure monster is not left paused by a QTE so it can process caught/respawn logic
        pausedForQTE = false;

        // stop any in-progress movement by switching state
        ChangeState(MonsterState.Caught);
    }

    public void SetMonsterRoom(int roomIndex)
    {
        currentRoomIndex = roomIndex;
        currentStageIndex = 0;
        Debug.Log($"Monster moved to Room {roomIndex}.");
    }

    // Call this to pause monster behavior while a QTE runs
    public void PauseForQTE()
    {
        if (pausedForQTE) return;
        pausedForQTE = true;
        // also mark moving flag false so other systems won't consider monster actively moving
        isMovingThroughStages = false;
        Debug.Log("Monster paused for QTE.");
    }

    // Call this when QTE fails and you want monster behavior to resume where it left off
    public void ResumeAfterQTEFailure()
    {
        if (!pausedForQTE) return;
        pausedForQTE = false;
        Debug.Log("Monster resumed after QTE failure (continuing previous behavior).");
    }

    // Attempt to break a random box or generator in a room (keeps original behavior)
    private bool BreakRandomBoxInRoom(int roomIndex)
    {
        GameObject[] allBoxesAndGenerators = GameObject.FindGameObjectsWithTag("Box");

        List<Boxes> validBoxes = new List<Boxes>();
        List<Generator> validGenerators = new List<Generator>();

        foreach (GameObject obj in allBoxesAndGenerators)
        {
            Boxes box = obj.GetComponent<Boxes>();
            if (box != null && box.roomIndex == roomIndex && box.CanBreak(roomIndex))
                validBoxes.Add(box);

            Generator generator = obj.GetComponent<Generator>();
            if (generator != null && generator.roomIndex == roomIndex && !generator.isBroken)
                validGenerators.Add(generator);
        }

        if (validBoxes.Count == 0 && validGenerators.Count == 0)
        {
            Debug.Log($"No valid boxes or generators available to break in Room {roomIndex}!");
            return false;
        }

        if (validBoxes.Count > 0 && (validGenerators.Count == 0 || Random.value < 0.5f))
        {
            int randomIndex = Random.Range(0, validBoxes.Count);
            Boxes selectedBox = validBoxes[randomIndex];
            selectedBox.BreakBox();
            return true;
        }
        else if (validGenerators.Count > 0)
        {
            int randomIndex = Random.Range(0, validGenerators.Count);
            Generator selectedGenerator = validGenerators[randomIndex];
            selectedGenerator.BreakGenerator();
            return true;
        }

        return false;
    }
}