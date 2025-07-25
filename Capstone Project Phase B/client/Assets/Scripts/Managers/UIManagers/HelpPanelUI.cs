using DG.Tweening;
using UnityEngine;

public class HelpPanelUI : Singleton<HelpPanelUI> {
    [SerializeField] private Transform helpPanel; // Reference to the help panel
    [SerializeField] private TypeWriterEffect helpText; // Reference to the help text

    /// <summary>
    /// Hides the help panel on start.
    /// </summary>
    private void Start() {
        helpPanel.localScale = Vector3.zero;
        helpText.Text = string.Empty;
        helpPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the help panel with the specified text.
    /// </summary>
    /// <param name="text">The text to be displayed in the help panel.</param>
    public void ShowHelp(string text) {
        helpPanel.gameObject.SetActive(true);
        helpText.Text = text;
        helpPanel.DOScale(Vector3.one, 0.75f).SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// Hides the help panel.
    /// </summary>
    public void HideHelp() {
        helpPanel.DOScale(Vector3.zero, 0.75f).SetEase(Ease.InCirc).OnComplete(() => {
            helpText.Text = string.Empty;
            helpPanel.gameObject.SetActive(false);
        });
    }
}