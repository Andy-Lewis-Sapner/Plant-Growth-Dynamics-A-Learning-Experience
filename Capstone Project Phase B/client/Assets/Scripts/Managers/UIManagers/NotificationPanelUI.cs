using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Handles display of notifications and guide messages with optional close button.
// Uses DOTween for smooth fade animations.
public class NotificationPanelUI : Singleton<NotificationPanelUI> {
    private const float NotificationFadeDuration = 0.5f;// Duration for fade-in/out animations
    private const float NotificationStayDurationPerChar = 0.05f;// Time to keep notification visible per character
    public event EventHandler OnCloseButtonClicked; // Event triggered when the close button is clicked
    
    [SerializeField] private CanvasGroup notificationPanel; // The main canvas group for the notification panel
    [SerializeField] private TextMeshProUGUI notificationText; // The text field to display the message
    [SerializeField] private Button closeButton; // The close button for guide messages

    private Sequence _notificationSequence; // DOTween sequence used to animate notifications
    private bool _isGuideNotificationActive; // Flag to indicate if a guide notification is currently shown

    private void Start() {
        // Ensure the panel starts hidden
        if (notificationPanel) {
            notificationPanel.alpha = 0;
            SetCanvasGroupSettings(false);
        }
        
        LoadingScreenUI.Instance.OnLoadingScreenClosed += OnLoadingScreenClosed;
    }

    private void OnLoadingScreenClosed(object sender, EventArgs e) {
        string message = $"Welcome {DataManager.Instance.UserData.username}! Hold Alt key to access UI.";
        ShowGuideNotification(message);
        Invoke(nameof(EndGuideNotification), message.Length * 0.15f);
    }
    // Displays a persistent guide-style notification with a close button.
    public void ShowGuideNotification(string message) {
        if (!notificationPanel || !notificationText) return;

        closeButton.gameObject.SetActive(true); // Show the close button
        notificationPanel.DOKill(); // Kill any ongoing fade
        _notificationSequence?.Kill(); // Kill any ongoing sequence
        notificationText.text = message;

        if (!_isGuideNotificationActive) {
            notificationPanel.alpha = 0f;
            notificationPanel.DOFade(1f, NotificationFadeDuration).OnComplete(() => SetCanvasGroupSettings(true));
            _isGuideNotificationActive = true;
        }
    }
    // Ends the currently active guide notification.
    public void EndGuideNotification() {
        if (!_isGuideNotificationActive) return;
        notificationPanel.DOKill();
        _notificationSequence?.Kill();
        notificationPanel.DOFade(0f, NotificationFadeDuration).OnComplete(() => SetCanvasGroupSettings(false));
        _isGuideNotificationActive = false;
    }
    
    // Shows a standard temporary notification without close button.
    public void ShowNotification(string message, float durationExtension = 0.5f) {
        if (!notificationPanel || !notificationText) return;
        if (_isGuideNotificationActive) return;// Prevent interrupting guide messages
    
        closeButton.gameObject.SetActive(false);// Hide close button
        notificationPanel.DOKill();
        _notificationSequence?.Kill();
        notificationPanel.alpha = 0;
        SetCanvasGroupSettings(false);
        notificationText.text = message;
    
        float stayDuration = NotificationStayDurationPerChar * message.Length + durationExtension;
        _notificationSequence = DOTween.Sequence();// Creates a new sequence to chain multiple animations for showing and hiding the notification.
        _notificationSequence.Append(notificationPanel.DOFade(1f, NotificationFadeDuration)
                .OnComplete(() => SetCanvasGroupSettings(true)))
            .AppendInterval(stayDuration).Append(notificationPanel.DOFade(0f, NotificationFadeDuration)
                .OnComplete(() => SetCanvasGroupSettings(false)));
    }
    
    // Enables or disables interactivity for the canvas group.
    private void SetCanvasGroupSettings(bool isActive) {
        notificationPanel.interactable = isActive;
        notificationPanel.blocksRaycasts = isActive;
    }
    
    // Method hooked to the close button to notify listeners.
    public void OnCloseButtonClickedEvent() {
        OnCloseButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    // Unsubscribe from the event when the object is destroyed
    private void OnDestroy() {
        LoadingScreenUI.Instance.OnLoadingScreenClosed -= OnLoadingScreenClosed;
    }
}
