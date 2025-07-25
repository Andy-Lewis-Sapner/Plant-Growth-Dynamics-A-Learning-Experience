using LoM.Super;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a password input field with toggleable visibility using an eye button.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class PasswordFieldController : SuperBehaviour {
    [SerializeField] private Sprite openEyeSprite; // Sprite for visible password state
    [SerializeField] private Sprite closedEyeSprite; // Sprite for hidden password state

    private Button _eyeButton; // Button to toggle password visibility
    private TMP_InputField _inputField; // Password input field
    private bool _showPassword; // Tracks if password is visible
    private bool _isProcessing; // Unused flag for processing state

    /// <summary>
    /// Initializes the input field and eye button, sets up password mode.
    /// </summary>
    private void Awake() {
        _inputField = GetComponent<TMP_InputField>();
        _eyeButton = GetComponentInChildren<Button>();
        if (_eyeButton) _eyeButton.onClick.AddListener(TogglePasswordVisibility);

        _inputField.contentType = TMP_InputField.ContentType.Password;
        _inputField.asteriskChar = '•';
        _inputField.ForceLabelUpdate();
        _eyeButton.image.sprite = closedEyeSprite;
    }

    /// <summary>
    /// Toggles password visibility between hidden and visible states.
    /// </summary>
    private void TogglePasswordVisibility() {
        _showPassword = !_showPassword;
        _inputField.contentType =
            _showPassword ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        _inputField.ForceLabelUpdate();
        _eyeButton.image.sprite = _showPassword ? closedEyeSprite : openEyeSprite;
    }
}