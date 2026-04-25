using UnityEngine;
using System.Collections.Generic;

public class MonsterStages : MonoBehaviour
{
    [Tooltip("Reference to the Animator controlling the monster's animations.")]
    public Animator animator;

    [System.Serializable]
    public class StageAnimation
    {
        public string stageName;      // e.g., "Stage1", "Stage2"
        public string animationState; // e.g., "Idle", "Roar", "Attack"
    }

    [Tooltip("List of stage-to-animation mappings (fallback/global by stage index).")]
    public List<StageAnimation> stageAnimations = new List<StageAnimation>();

    [System.Serializable]
    public class RoomStageMapping
    {
        [Tooltip("Room index this mapping applies to.")]
        public int roomIndex;

        [Tooltip("Animation state names for each stage in this room. Index = stage index.")]
        public List<string> animationStatePerStage = new List<string>();
    }

    [Tooltip("Optional: per-room stage => animation mappings. If a mapping exists for a room it will be used first.")]
    public List<RoomStageMapping> roomStageMappings = new List<RoomStageMapping>();

    // Set the monster's animation based on the global stage index (existing behavior)
    public void SetStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageAnimations.Count)
            return;

        string animationState = stageAnimations[stageIndex].animationState;
        if (animator != null && !string.IsNullOrEmpty(animationState))
        {
            animator.Play(animationState);
        }
    }

    // New: set animation by room index + stage index (preferred for per-position poses)
    public void SetStage(int roomIndex, int stageIndex)
    {
        // Try per-room mapping first
        var mapping = roomStageMappings.Find(m => m.roomIndex == roomIndex);
        if (mapping != null)
        {
            if (stageIndex >= 0 && stageIndex < mapping.animationStatePerStage.Count)
            {
                string state = mapping.animationStatePerStage[stageIndex];
                if (animator != null && !string.IsNullOrEmpty(state))
                {
                    animator.Play(state);
                    return;
                }
            }
            else
            {
                Debug.LogWarning($"MonsterStages: room {roomIndex} mapping does not contain stage index {stageIndex}.");
            }
        }

        // Fallback to global stageAnimations (by stageIndex)
        SetStage(stageIndex);
    }

    // Optional: Set by stage name (unchanged)
    public void SetStage(string stageName)
    {
        var stage = stageAnimations.Find(s => s.stageName == stageName);
        if (stage != null && animator != null && !string.IsNullOrEmpty(stage.animationState))
        {
            animator.Play(stage.animationState);
        }
    }
}