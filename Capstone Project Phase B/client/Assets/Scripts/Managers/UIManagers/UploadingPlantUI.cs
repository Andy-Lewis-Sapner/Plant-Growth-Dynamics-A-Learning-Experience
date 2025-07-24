using UnityEngine;
using UnityEngine.UI;

// Handles the UI for uploading a plant image and confirming the predicted plant
public class UploadingPlantUI : UIScreen<UploadingPlantUI> {
    [Header("Image and Text Components")]
    [SerializeField] private Image fileBrowserImage; // Displays the uploaded image
    [SerializeField] private TypeWriterEffect predictLabel; // Displays the predicted plant name
    [SerializeField] private TypeWriterEffect errorMessage; // Displays an error message if prediction fails

    // Called when the user clicks the continue button to confirm prediction
    public void OnContinueButtonClicked() {
        PlantSO plantSo = DataManager.Instance.PlantsListSo.FindPlantSoByName(predictLabel.Text); // Try to find plant by predicted name
        GameObject plantPrefab = plantSo.plantPrefab;
        if (plantPrefab) {
            Player.Instance.HoldingPlant = plantSo; // Assign plant to player
            CloseScreen(); // Close the upload screen
        } else {
            errorMessage.Text = "Couldn't find the predicted plant. Try Again."; // Show error
        }
    }
    
    public void OnUploadButtonClicked() {
        CloseScreen();
        FileBrowserManager.Instance.OpenFileBrowser(FileBrowserManager.IdentificationType.IdentifyPlant);
    }

    // Displays the selected image in the UI and opens the screen
    public void SetFileBrowserImage(Sprite sprite) {
        if (fileBrowserImage.sprite != sprite)
            fileBrowserImage.sprite = sprite;
        OpenScreen();
    }

    // Sets the predicted label text with capitalization
    public void SetPredictLabel(string labelText) {
        if (string.IsNullOrEmpty(labelText)) {
            predictLabel.Text = string.Empty;
            return;
        }

        char[] chars = labelText.ToCharArray();
        chars[0] = char.ToUpper(chars[0]); // Capitalize the first letter
        predictLabel.Text = new string(chars);
    }

    // Called when the screen is deactivated to clear UI
    protected override void DeactivateScreen() {
        predictLabel.Text = string.Empty; // Clear prediction text
        errorMessage.Text = string.Empty; // Clear error message
        base.DeactivateScreen();
    }
}
