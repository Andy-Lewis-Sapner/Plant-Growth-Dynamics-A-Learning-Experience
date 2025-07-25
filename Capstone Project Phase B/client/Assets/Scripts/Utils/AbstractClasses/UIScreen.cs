using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract singleton UI screen with open/close animations.
/// </summary>
/// <typeparam name="T">The component type for the singleton.</typeparam>
public abstract class UIScreen<T> : Singleton<T> where T : Component {
    private const float OpeningDuration = 0.75f; // Duration for opening animation
    private const float ClosingDuration = 0.75f; // Duration for closing animation
    private readonly Vector2 _closedOffset = new(0, -1500); // Offset when screen is closed
    private readonly Vector2 _openOffset = Vector2.zero; // Offset when screen is open

    public bool IsScreenOpen { get; private set; } // Tracks if the screen is open

    private RectTransform _screenRectTransform; // Reference to the screen's RectTransform
    private bool _isClosing; // Tracks if the screen is closing

    /// <summary>
    /// Virtual method for initializing the screen.
    /// </summary>
    protected virtual void InitializeScreen() { }

    /// <summary>
    /// Initializes the screen's RectTransform and sets it closed after Awake.
    /// </summary>
    protected override void AfterAwake() {
        InitializeScreen();

        _screenRectTransform = GetComponent<RectTransform>();
        _screenRectTransform.offsetMin = _closedOffset;
        _screenRectTransform.offsetMax = _closedOffset;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Opens the screen with an animation.
    /// </summary>
    public virtual void OpenScreen() {
        if (_isClosing) return;

        IsScreenOpen = true;
        gameObject.SetActive(true);
        _screenRectTransform.DOKill();

        DOTween.To(() => _screenRectTransform.offsetMin, x => {
            _screenRectTransform.offsetMin = x;
            _screenRectTransform.offsetMax = x;
        }, _openOffset, OpeningDuration).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// Closes the screen with an animation.
    /// </summary>
    public void CloseScreen() {
        if (!IsScreenOpen || _isClosing) return;

        _isClosing = true;
        _screenRectTransform.DOKill();

        DOTween.To(() => _screenRectTransform.offsetMin, x => {
            _screenRectTransform.offsetMin = x;
            _screenRectTransform.offsetMax = x;
        }, _closedOffset, ClosingDuration).SetEase(Ease.InCubic).OnComplete(DeactivateScreen);
    }

    /// <summary>
    /// Deactivates the screen and resets state after closing.
    /// </summary>
    protected virtual void DeactivateScreen() {
        gameObject.SetActive(false);
        IsScreenOpen = false;
        _isClosing = false;
    }
}