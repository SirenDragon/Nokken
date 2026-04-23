using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class startscreenswitch : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button startButton;

    [SerializeField] private AudioMixerSnapshot paused;
    public AudioMixerSnapshot startScreenSnapshot;
    public AudioMixerSnapshot deckSnapshot;
    [SerializeField] private int sceneBuildIndex = 1;
    [SerializeField] private float sceneTransitionTime = 0.6f;

    private bool isLoadingScene;

    private VisualElement fadeContainer;
    private VisualElement menuElements;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fadeContainer = uiDocument.rootVisualElement.Q<VisualElement>("FadePanel");
        menuElements = uiDocument.rootVisualElement.Q<VisualElement>("Menu");

        Invoke("SetStartSnapshot", 0.1f);
        BindStartButton();
    }

    void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.clicked -= StartGameScene;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FadeToBlack(float targetOpacity)
    {
        fadeContainer.style.opacity = targetOpacity;
    }
    private void RemoveMenuUI(float targetOpacity)
    {
        menuElements.style.opacity = targetOpacity;
    }

    private void BindStartButton()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("UIDocument is not assigned on startscreenswitch.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        startButton = root.Q<Button>("StartButton");

        if (startButton == null)
        {
            Debug.LogWarning("StartButton not found in UIDocument (search name: 'StartButton').");
            return;
        }

        startButton.clicked += StartGameScene;
    }

    public void SetStartSnapshot()
    {
        startScreenSnapshot.TransitionTo(0f);
    }

    public void SetDeckSnapshot()
    {
        deckSnapshot.TransitionTo(0f);
    }

    public void StartGameScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;

        if (deckSnapshot != null)
        {
            deckSnapshot.TransitionTo(sceneTransitionTime);
        }

        StartCoroutine(LoadSceneAsyncAfterTransition());
    }

    private System.Collections.IEnumerator LoadSceneAsyncAfterTransition()
    {
        if (sceneTransitionTime > 0f)
        {
            yield return new WaitForSecondsRealtime(sceneTransitionTime);
        }

        RemoveMenuUI(0f);
        FadeToBlack(1f);

        yield return new WaitForSecondsRealtime(4f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex);

        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }
    }
}