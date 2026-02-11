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

    [Tooltip("List of stage-to-animation mappings.")]
    public List<StageAnimation> stageAnimations = new List<StageAnimation>();

    // Set the monster's animation based on the stage index or name
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

    // Optional: Set by stage name
    public void SetStage(string stageName)
    {
        var stage = stageAnimations.Find(s => s.stageName == stageName);
        if (stage != null && animator != null && !string.IsNullOrEmpty(stage.animationState))
        {
            animator.Play(stage.animationState);
        }
    }
}