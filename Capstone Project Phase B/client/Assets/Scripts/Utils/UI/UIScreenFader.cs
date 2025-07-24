using DG.Tweening;
using LoM.Super;
using UnityEngine;

/// <summary>
/// Fades a UI screen in or out by controlling the CanvasGroup's alpha.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIScreenFader : SuperBehaviour {
    private const float FadeDuration = 0.5f;

    [SerializeField] private bool isActiveAtStart;
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// Initializes the CanvasGroup and sets initial active state.
    /// </summary>
    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(isActiveAtStart);
    }

    /// <summary>
    /// Fades the screen in or out over a fixed duration using DOTween.
    /// </summary>
    /// <param name="fadeIn">True to fade in, false to fade out.</param>
    public void FadeScreen(bool fadeIn) {
        if (!_canvasGroup) {
            gameObject.SetActive(fadeIn);
            return;
        }
        if (Mathf.Approximately(_canvasGroup.alpha, fadeIn ? 1f : 0f)) return;

        if (fadeIn)
            _canvasGroup.DOFade(1f, FadeDuration).SetEase(Ease.Linear).OnStart(() => gameObject.SetActive(true));
        else
            _canvasGroup.DOFade(0f, FadeDuration).SetEase(Ease.Linear).OnComplete(() => gameObject.SetActive(false));
    }
}