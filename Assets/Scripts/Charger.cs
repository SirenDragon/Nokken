using UnityEngine;
using UnityEngine.UIElements;

public class Charger : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Label spaceTip;

    private Renderer[] chargerRenderer;
    [SerializeField] private Material chargerOffMaterial;
    [SerializeField] private Material chargerOnMaterial;

    [Tooltip("Maximum distance the player can be from the charger to charge.")]
    public float useDistance = 3f;

    [Tooltip("If set, this Generator's state (isBroken) disables charging for all chargers.")]
    public Generator generator; // assign your single generator here (optional, auto-found if null)

    private bool isPlayerHovering = false;
    private Transform playerTransform;
    private WeaponCharge weaponCharge;

    void Start()
    {
        spaceTip = uiDocument.rootVisualElement.Q<Label>("SpaceTip");

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        weaponCharge = Object.FindObjectOfType<WeaponCharge>();
        if (weaponCharge == null)
            Debug.LogWarning("WeaponCharge not found in the scene. Charger won't enable charging.");
        if (playerTransform == null)
            Debug.LogWarning("Player not found (expecting tag 'Player'). Charger will only check hover without distance.");

        if (generator == null)
        {
            // try to auto-find the single generator in the scene
            generator = Object.FindObjectOfType<Generator>();
            if (generator == null)
                Debug.LogWarning("Generator not found in the scene. Chargers will not be blocked by generator state.");
        }

        // cache renderers (including children) to swap materials
        chargerRenderer = GetComponentsInChildren<Renderer>(true);

        // subscribe to generator state changes (if generator exists)
        Generator.OnGeneratorStateChanged += OnGeneratorStateChanged;

        // initialize material according to current generator state (if available)
        bool generatorOnline = generator == null || !generator.isBroken;
        ApplyMaterial(generatorOnline ? chargerOnMaterial : chargerOffMaterial);
    }

    void Update()
    {
        // While the mouse is over the charger, keep checking player distance and generator state and update permission
        if (isPlayerHovering && weaponCharge != null)
        {
            bool closeEnough = playerTransform == null || Vector3.Distance(playerTransform.position, transform.position) <= useDistance;
            bool generatorOk = generator == null || !generator.isBroken; // if generator exists and is broken -> block

            weaponCharge.SetAllowCharging(closeEnough && generatorOk);
        }
    }

    void OnMouseEnter()
    {
        isPlayerHovering = true;
        if (weaponCharge != null)
        {
            bool closeEnough = playerTransform == null || Vector3.Distance(playerTransform.position, transform.position) <= useDistance;
            bool generatorOk = generator == null || !generator.isBroken;

            weaponCharge.SetAllowCharging(closeEnough && generatorOk);
        }

        if (weaponCharge.allowCharging == true)
        {
            spaceTip.visible = true;
        }
    }

    void OnMouseExit()
    {
        isPlayerHovering = false;
        if (spaceTip != null)
        {
            spaceTip.visible = false;
        }
        if (weaponCharge != null)
            weaponCharge.SetAllowCharging(false);
    }

    void OnDisable()
    {
        if (weaponCharge != null)
            weaponCharge.SetAllowCharging(false);

        // unsubscribe
        Generator.OnGeneratorStateChanged -= OnGeneratorStateChanged;
    }

    void OnDestroy()
    {
        if (weaponCharge != null)
            weaponCharge.SetAllowCharging(false);

        // unsubscribe
        Generator.OnGeneratorStateChanged -= OnGeneratorStateChanged;
    }

    // Callback from Generator.OnGeneratorStateChanged (true == online)
    private void OnGeneratorStateChanged(bool online)
    {
        ApplyMaterial(online ? chargerOnMaterial : chargerOffMaterial);
    }

    // Helper: set material on all cached renderers (guard nulls)
    private void ApplyMaterial(Material mat)
    {
        if (mat == null || chargerRenderer == null) return;
        foreach (var rend in chargerRenderer)
        {
            if (rend == null) continue;
            // If renderer uses multiple materials you can set sharedMaterial(s) or a specific material index here.
            rend.material = mat;
        }
    }

    // Optional: draw the usage radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, useDistance);
    }
}