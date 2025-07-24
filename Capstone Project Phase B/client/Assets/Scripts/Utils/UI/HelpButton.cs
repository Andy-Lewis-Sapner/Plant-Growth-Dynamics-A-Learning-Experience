using LoM.Super;
using UnityEngine;

public class HelpButton : SuperBehaviour {
    [TextArea] [SerializeField] private string helpText; // The text to be displayed in the help panel

    /// <summary>
    /// Displays the help panel with the specified text.
    /// </summary>
    public void ShowHelp() {
        HelpPanelUI.Instance.ShowHelp(helpText);
    }
}