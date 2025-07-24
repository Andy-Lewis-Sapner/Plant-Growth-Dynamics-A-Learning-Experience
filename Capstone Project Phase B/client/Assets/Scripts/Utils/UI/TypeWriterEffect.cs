using DG.Tweening;
using LoM.Super;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Displays text with a typewriter animation effect using DOTween.
/// Allows skipping the animation by clicking.
/// </summary>
public class TypeWriterEffect : SuperBehaviour, IPointerClickHandler {
    private const float TypingSpeed = 0.05f;
    private TMP_Text _tmpText;
    private string _text;
    private Tween _typingTween;

    /// <summary>
    /// The full text to display with the typewriter effect.
    /// Setting this starts the animation if active.
    /// </summary>
    public string Text {
        get => _text;
        set {
            _text = value;
            if (_tmpText && gameObject.activeInHierarchy) StartTypingEffect();
        }
    }

    /// <summary>
    /// Initializes the TMP component and clears initial text.
    /// </summary>
    private void Awake() {
        _tmpText = GetComponent<TMP_Text>();
        if (!_tmpText) return;
        _text = _tmpText.text;
        _tmpText.text = string.Empty;
    }

    /// <summary>
    /// Starts the typewriter animation using DOTween.
    /// </summary>
    private void StartTypingEffect() {
        _typingTween?.Kill();
        _tmpText.text = string.Empty;
        if (string.IsNullOrEmpty(_text)) return;
        
        float duration = _text.Length * TypingSpeed;
        float counter = 0f;
        char[] textChars = _text.ToCharArray();

        _typingTween = DOTween.To(() => counter, x => {
            counter = x;
            int charsToShow = Mathf.FloorToInt(counter);
            _tmpText.text = new string(textChars, 0, Mathf.Min(charsToShow, _text.Length));
        }, _text.Length, duration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// Skips the typing animation and shows the full text instantly on click.
    /// </summary>
    /// <param name="eventData">Click event data.</param>
    public void OnPointerClick(PointerEventData eventData) {
        _typingTween?.Kill();
        _tmpText.text = _text;
    }

    /// <summary>
    /// Restarts the typewriter effect when enabled.
    /// </summary>
    private void OnEnable() {
        if (_tmpText && !string.IsNullOrEmpty(_text)) StartTypingEffect();
    }

    /// <summary>
    /// Stops the animation when disabled.
    /// </summary>
    private void OnDisable() {
        _typingTween?.Kill();
    }
}
