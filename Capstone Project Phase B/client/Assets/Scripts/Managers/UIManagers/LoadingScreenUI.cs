using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenUI : Singleton<LoadingScreenUI> {
    public event EventHandler OnLoadingScreenClosed;
    private const float FadeDuration = 0.5f; // Duration for fading the loading screen out
    private const float NotificationFadeDuration = 0.5f;

    [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup to control loading screen visibility and fading
    [SerializeField] private TextMeshProUGUI loadingText; // Text that displays "Loading..." with fade animation
    [SerializeField] private CanvasGroup notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;

    protected override void AfterAwake() {
        if (!canvasGroup) return;

        canvasGroup.alpha = 1f;
        notificationPanel.alpha = 0f;
        gameObject.SetActive(true);
    }

    private void Start() {
        // Called on first frame — sets loading text to pulse continuously
        // Fade to 30% opacity over 0.5s
        // Repeat the fade in/out forever
        if (loadingText) loadingText.DOFade(0.3f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    // Hides the loading screen by fading it out
    public void HideLoadingScreen() {
        if (canvasGroup)
            canvasGroup.DOFade(0f, FadeDuration).OnComplete(ClosingLoadingScreenSequence);
        else
            ClosingLoadingScreenSequence();

        OnLoadingScreenClosed?.Invoke(this, EventArgs.Empty);
    }

    // Called when the loading screen is hidden
    private void ClosingLoadingScreenSequence() {
        // Called after the screen is hidden to finalize the transition
        gameObject.SetActive(false); // Disable the loading screen GameObject
        AudioManager.Instance.PlayMusicBasedOnScene(SceneManager.GetActiveScene()); // Play music based on current scene
    }

    // Displays a fading notification on the screen (an error message)
    public void ShowNotification(string message) {
        notificationPanel.alpha = 0f;
        notificationPanel.DOFade(1f, NotificationFadeDuration).OnComplete(() => {
            notificationText.text = message;
            notificationPanel.DOFade(0f, NotificationFadeDuration).OnComplete(() => { notificationPanel.alpha = 1f; });
        });
    }
}