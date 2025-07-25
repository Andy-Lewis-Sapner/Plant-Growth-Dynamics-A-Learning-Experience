using DG.Tweening;
using LoM.Super;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the main menu UI elements with fading and scaling effects.
/// </summary>
public class MainMenuAnimation : SuperBehaviour {
    [SerializeField] private Image backgroundImage; // Background image of the menu
    [SerializeField] private TextMeshProUGUI titleText; // Title text component
    [SerializeField] private TextMeshProUGUI subtitleText; // Subtitle text component
    [SerializeField] private Button[] buttons; // Array of buttons to animate

    /// <summary>
    /// Initializes and starts the animation on start.
    /// </summary>
    private void Start() {
        InitializeElements();
        PlayAnimation();
    }

    /// <summary>
    /// Sets initial states for UI elements (hidden text, scaled-down buttons).
    /// </summary>
    private void InitializeElements() {
        titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 0f);
        subtitleText.color = new Color(subtitleText.color.r, subtitleText.color.g, subtitleText.color.b, 0f);
        foreach (Button button in buttons) button.transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Plays the animation sequence for title, subtitle, and buttons.
    /// </summary>
    private void PlayAnimation() {
        Sequence sequence = DOTween.Sequence();

        sequence.Append(titleText.rectTransform.DOAnchorPos(new Vector2(193, 326), 0.7f).From(new Vector2(193, 100))
            .SetEase(Ease.OutBack));
        sequence.Join(titleText.DOFade(1f, 0.7f));

        sequence.Append(subtitleText.rectTransform.DOAnchorPos(new Vector2(14.05f, 276), 0.7f)
            .From(new Vector2(-200, 276)).SetEase(Ease.OutBack));
        sequence.Join(subtitleText.DOFade(1f, 0.7f));

        const float delayBetweenButtons = 0.05f;
        for (int i = 0; i < buttons.Length; i++)
            sequence.Append(buttons[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack)
                .SetDelay(i * delayBetweenButtons));
    }
}