using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extends ToggleSwitch to change colors of background and handle images during toggle animation.
/// </summary>
public class ToggleSwitchColorChange : ToggleSwitch {
    [Header("Elements to Recolor")] [SerializeField]
    private Image backgroundImage; // Background image to recolor

    [SerializeField] private Image handleImage; // Handle image to recolor

    [Space] [SerializeField] private bool recolorBackground; // Enable background recoloring
    [SerializeField] private bool recolorHandle; // Enable handle recoloring

    [Header("Colors")] [SerializeField] private Color backgroundColorOff = Color.white; // Background color when off
    [SerializeField] private Color backgroundColorOn = Color.white; // Background color when on
    [Space] [SerializeField] private Color handleColorOff = Color.red; // Handle color when off
    [SerializeField] private Color handleColorOn = Color.green; // Handle color when on

    private bool _isBackgroundImageNotNull; // Tracks if background image is assigned
    private bool _isHandleImageNotNull; // Tracks if handle image is assigned

    /// <summary>
    /// Validates components and updates colors in the editor.
    /// </summary>
    private new void OnValidate() {
        base.OnValidate();

        CheckForNull();
        ChangeColors();
    }

    /// <summary>
    /// Subscribes to transition effect on enable.
    /// </summary>
    private void OnEnable() {
        TransitionEffect += ChangeColors;
    }

    /// <summary>
    /// Unsubscribes from transition effect on disable.
    /// </summary>
    private void OnDisable() {
        TransitionEffect -= ChangeColors;
    }

    /// <summary>
    /// Initializes component and updates colors after base Awake.
    /// </summary>
    protected override void Awake() {
        base.Awake();

        CheckForNull();
        ChangeColors();
    }

    /// <summary>
    /// Checks if background and handle images are assigned.
    /// </summary>
    private void CheckForNull() {
        _isBackgroundImageNotNull = backgroundImage;
        _isHandleImageNotNull = handleImage;
    }

    /// <summary>
    /// Updates colors of background and handle based on slider value.
    /// </summary>
    private void ChangeColors() {
        if (recolorBackground && _isBackgroundImageNotNull)
            backgroundImage.color = Color.Lerp(backgroundColorOff, backgroundColorOn, sliderValue);

        if (recolorHandle && _isHandleImageNotNull)
            handleImage.color = Color.Lerp(handleColorOff, handleColorOn, sliderValue);
    }
}