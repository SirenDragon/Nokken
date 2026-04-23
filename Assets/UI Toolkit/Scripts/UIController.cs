using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Animator animator;

    private VisualElement fadeContainer;
    private VisualElement menuElements;
    private VisualElement galleryOverlay;
    private VisualElement controlsOverlay;

    private Button startButton;
    private Button controlsButton;
    private Button galleryButton;
    private Button exitGalleryButton;
    private Button exitControlsButton;
    private Button pokeButton;


    void Start()
    {
        fadeContainer = uiDocument.rootVisualElement.Q<VisualElement>("FadePanel");
        menuElements = uiDocument.rootVisualElement.Q<VisualElement>("Menu");
        galleryOverlay = uiDocument.rootVisualElement.Q<VisualElement>("Gallery");
        controlsOverlay = uiDocument.rootVisualElement.Q<VisualElement>("Controls");


        //Buttons
        startButton = uiDocument.rootVisualElement.Q<Button>("StartButton");
        controlsButton = uiDocument.rootVisualElement.Q<Button>("ControlsButton");
        galleryButton = uiDocument.rootVisualElement.Q<Button>("GalleryButton");
        controlsButton = uiDocument.rootVisualElement.Q<Button>("ControlsButton");
        pokeButton = uiDocument.rootVisualElement.Q<Button>("PokeButton");

        exitGalleryButton = uiDocument.rootVisualElement.Q<Button>("ExitGalleryButton");
        exitControlsButton = uiDocument.rootVisualElement.Q<Button>("ExitControlsButton");

        controlsButton.clicked += OnControlsClicked;
        galleryButton.clicked += OnGalleryClicked;
        exitGalleryButton.clicked += OnExitGalleryClicked;
        exitControlsButton.clicked += OnExitControlsClicked;
        pokeButton.clicked += OnPokeClicked;
    }

    void Update()
    {
        // var keyboard = Keyboard.current;
        // if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
        // {
        //     RemoveMenuUI(0f);
        //     FadeToBlack(1f);
        // }
    }

    private void FadeToBlack(float targetOpacity)
    {
        fadeContainer.style.opacity = targetOpacity;
    }

    private void RemoveMenuUI(float targetOpacity)
    {
        menuElements.style.opacity = targetOpacity;
    }

    //POKE
    void OnPokeClicked()
    {
        pokeButton.style.display = DisplayStyle.None;
        animator.SetTrigger("FlinchTrigger");
        StartCoroutine(PokeRoutine());
    }
    IEnumerator PokeRoutine()
    {
        yield return new WaitForSeconds(5.3f); // Wait for the animation to finish
        pokeButton.style.display = DisplayStyle.Flex;
    }


    //Gallery
    void OnGalleryClicked ()
    {
        menuElements.visible = false;
        galleryOverlay.visible = true;
    }

    void OnExitGalleryClicked()
    {
        galleryOverlay.visible = false;
        menuElements.visible = true;

    }

    //Controls
    void OnControlsClicked()
    {
        menuElements.visible = false;
        controlsOverlay.visible = true;
    }

    void OnExitControlsClicked()
    {
        controlsOverlay.visible = false;
        menuElements.visible = true;

    }
}
