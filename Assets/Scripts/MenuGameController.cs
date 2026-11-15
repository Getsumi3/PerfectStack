using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuGameController : MonoBehaviour
{
    [Header("Menu")]
    public GameObject startMenuPanel;
    public Button startButton;

    [Header("Menu Stack")]
    public Animator menuStackAnimator;
    public AnimationClip stackAssembleClip;
    public AnimationClip stackExitClip;

    [Header("Base Block")]
    public Animator baseBlockAnimator;
    public AnimationClip baseAssembleClip;
    public AnimationClip baseDropClip;

    [Header("Gameplay")]
    public StackManager stackManager;

    private bool gameStarted = false;

    void Awake()
    {
        if (startButton != null)
            startButton.interactable = false;
    }

    void Start()
    {
        StartCoroutine(WaitForMenuReady());
    }

    IEnumerator WaitForMenuReady()
    {
        float waitTime = Mathf.Max(
            stackAssembleClip.length,
            baseAssembleClip.length
        );

        yield return new WaitForSeconds(waitTime);

        if (startButton != null)
            startButton.interactable = true;
    }

    public void StartGame()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        if (startButton != null)
            startButton.interactable = false;

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        // Play the menu blocks' exit animation.
        menuStackAnimator.SetTrigger("StartGame");

        yield return new WaitForSeconds(stackExitClip.length);

        // Drop the blue BaseBlock onto the Ground.
        baseBlockAnimator.SetTrigger("StartDrop");

        yield return new WaitForSeconds(baseDropClip.length);

        // Hide the menu.
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);

        // Start actual gameplay.
        stackManager.BeginGame();
    }
}