using System;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : Singleton<DialogueUI> {
    // Event triggered when the dialogue ends
    public event EventHandler OnDialogueEnd;

    // Indicates whether a dialogue is currently active
    public bool IsDialogueActive { get; private set; }

    [SerializeField] private GameObject dialoguePanel; // The UI panel that displays the dialogue
    [SerializeField] private TypeWriterEffect dialogueText; // Component that types out dialogue text with an effect
    [SerializeField] private Button nextButton; // Button used to show the next line of dialogue

    private string[] _currentLines; // Array of dialogue lines to display
    private int _currentLineIndex; // Index of the currently displayed line

    private void Start() {
        // Called on initialization - hides the dialogue panel and sets up the button listener
        dialoguePanel.SetActive(false);
        nextButton.onClick.AddListener(ShowNextLine);
    }

    public void ShowDialogue(string[] lines) {
        // Starts showing the dialogue with the given lines
        _currentLines = lines;
        _currentLineIndex = 0;
        dialogueText.Text = _currentLines[_currentLineIndex];
        dialoguePanel.SetActive(true);

        IsDialogueActive = true;
        Player.Instance.DisableMovement = true;
        MouseMovement.Instance.UpdateCursorSettings(true);
    }

    private void ShowNextLine() {
        // Shows the next line or ends the dialogue if all lines have been shown
        _currentLineIndex++;
        if (_currentLineIndex < _currentLines.Length) {
            dialogueText.Text = _currentLines[_currentLineIndex];
        } else {
            EndDialogue();
        }
    }

    private void EndDialogue() {
        // Ends the dialogue, hides the panel, and restores player control
        dialoguePanel.SetActive(false);
        Player.Instance.DisableMovement = false;
        MouseMovement.Instance.UpdateCursorSettings(false);
        IsDialogueActive = false;
        OnDialogueEnd?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy() {
        // Removes button listeners when the object is destroyed to prevent memory leaks
        nextButton.onClick.RemoveAllListeners();
    }
}