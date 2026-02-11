using UnityEngine;

public class Info : MonoBehaviour
{
    [Tooltip("Canvas or UI panel GameObject to toggle.")]
    public GameObject canvasToToggle;

    [Tooltip("Key used to toggle the canvas.")]
    public KeyCode toggleKey = KeyCode.I;

    //[Tooltip("Should the canvas start visible?")]
    //public bool startVisible = true;

    void Awake()
    {
        //if (canvasToToggle != null)
            //canvasToToggle.SetActive(startVisible);
    }

    void Update()
    {
        if (canvasToToggle == null) return;

        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    // Public so you can also wire this to a UI Button OnClick()
    public void Toggle()
    {
        if (canvasToToggle == null)
        {
            Debug.LogWarning("Info: canvasToToggle not assigned.");
            return;
        }

        canvasToToggle.SetActive(!canvasToToggle.activeSelf);
    }
}