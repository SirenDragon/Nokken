using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class startscreenswitch : MonoBehaviour
{
    public AudioMixerSnapshot startScreenSnapshot;
    public AudioMixerSnapshot deckSnapshot;
    [SerializeField] private int sceneBuildIndex = 1;
    [SerializeField] private float sceneTransitionTime = 0.6f;

    private bool isLoadingScene;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("SetStartSnapshot", 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        
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

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex);

        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }
    }
}
