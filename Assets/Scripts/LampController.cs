using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LampEntry
{
    [Tooltip("Optional GameObject to toggle on/off when generator changes. Assign the root GameObject (e.g. lamp parent).")]
    public GameObject targetGameObject;

    [Tooltip("Optional renderers to toggle together with the object (makes lamp mesh appear off).")]
    public Renderer[] emissiveRenderers;
}

public class LampController : MonoBehaviour
{
    [Tooltip("List of lamps (GameObjects) you want this controller to manage. Drag GameObjects here.")]
    public List<LampEntry> lamps = new List<LampEntry>();

    void OnEnable()
    {
        Generator.OnGeneratorStateChanged += OnGeneratorStateChanged;
    }

    void OnDisable()
    {
        Generator.OnGeneratorStateChanged -= OnGeneratorStateChanged;
    }

    void Start()
    {
        // Apply initial generator state (fallback to online if no Generator found)
        var gen = FindObjectOfType<Generator>();
        bool generatorOnline = gen == null ? true : !gen.isBroken;
        ApplyState(generatorOnline);
    }

    private void OnGeneratorStateChanged(bool generatorOnline)
    {
        ApplyState(generatorOnline);
    }

    private void ApplyState(bool generatorOnline)
    {
        foreach (var entry in lamps)
        {
            if (entry == null) continue;

            if (entry.targetGameObject != null)
            {
                entry.targetGameObject.SetActive(generatorOnline);
            }

            if (entry.emissiveRenderers != null)
            {
                foreach (var r in entry.emissiveRenderers)
                {
                    if (r == null) continue;
                    r.enabled = generatorOnline;
                }
            }
        }
    }
}