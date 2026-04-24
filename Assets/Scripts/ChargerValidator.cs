using UnityEngine;

[RequireComponent(typeof(Charger))]
public class ChargerValidator : MonoBehaviour
{
    void Start()
    {
        var charger = GetComponent<Charger>();

        // Collider checks
        var ownCollider = GetComponent<Collider>();
        var childCollider = GetComponentInChildren<Collider>();
        if (ownCollider == null)
        {
            if (childCollider == null)
                Debug.LogError($"Charger on '{gameObject.name}' has no Collider. Add a 3D Collider to receive OnMouseEnter/Exit.");
            else
                Debug.LogWarning($"Charger on '{gameObject.name}' has no Collider on the same GameObject. OnMouse callbacks will be received by the child with the Collider; move the Charger or add a Collider to this GameObject.");
        }

        // Renderer check
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null)
            Debug.LogWarning($"Charger on '{gameObject.name}' has no Renderer found in children. Ensure the object is visible to the Camera.");

        // Layer / camera culling mask
        if (Camera.main != null)
        {
            if ((Camera.main.cullingMask & (1 << gameObject.layer)) == 0)
                Debug.LogWarning($"Charger '{gameObject.name}' layer is not included in Camera.main cullingMask.");
        }

        // WeaponCharge presence
        var wc = Object.FindObjectOfType<WeaponCharge>();
        if (wc == null)
            Debug.LogWarning("No WeaponCharge found in scene. Charger won't enable charging.");

        // Player tag
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            Debug.LogWarning("No GameObject with tag 'Player' found. Charger will skip distance checks (see console).");

        // Generator specified?
        if (charger.generator != null && charger.generator.isBroken)
            Debug.Log($"Assigned Generator isBroken = true; this Charger will be blocked until the generator is repaired.");
    }
}