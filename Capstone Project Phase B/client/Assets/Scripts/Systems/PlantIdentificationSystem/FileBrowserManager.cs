using System.IO;
using UnityEngine;
using SimpleFileBrowser;

/// <summary>
/// Handles the interaction with file browser operations.
/// </summary>
public class FileBrowserManager : Singleton<FileBrowserManager> {
    private static readonly Vector2 CenterPivot = new(0.5f, 0.5f); // Pivot point for sprite creation
    [SerializeField] private Sprite defaultSprite; // Default sprite for image display
    
    /// Configures the file browser with specific filters and default settings.
    /// This method is invoked to set up the file browser parameters. It includes:
    /// - Defining file type filters for images (e.g., `.jpg`, `.png`) and text files (e.g., `.txt`, `.pdf`).
    /// - Setting the default file type filter as `.jpg`.
    /// - Adding file extensions to be excluded from the file browsing view, such as `.lnk`, `.tmp`, `.zip`, `.rar`, and `.exe`.
    /// - Adding a quick link to the "Users" directory under `C:\Users`.
    /// This configuration prepares the file browser for consistent user interaction by managing file visibility and user shortcuts.
    private void Start() {
        FileBrowser.SetFilters(true,
            new FileBrowser.Filter("Images", ".jpg", ".png"),
            new FileBrowser.Filter("Text Files", ".txt", ".pdf"));
        
        FileBrowser.SetDefaultFilter(".jpg");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
        FileBrowser.AddQuickLink("Users", "C:\\Users");
    }

    /// <summary>
    /// Opens a file browser dialog for users to select files or folders.
    /// The behavior of the file browser is determined by the specified identification type.
    /// </summary>
    /// <param name="identificationType">The type of identification to perform,
    /// which determines the purpose of the file selection (e.g., IdentifyPlant or IdentifyDisease).</param>
    public void OpenFileBrowser(IdentificationType identificationType) {
        if (!FileBrowser.IsOpen)
            FileBrowser.ShowLoadDialog(paths => LoadImageFromFileBrowser(paths, identificationType), () => { },
                FileBrowser.PickMode.Files, false, null, null, "Select Folder");
    }

    /// <summary>
    /// Loads an image from the file browser and processes it based on the specified identification type.
    /// </summary>
    /// <param name="paths">An array of file paths selected from the file browser.</param>
    /// <param name="identificationType">The type of identification to perform: plant identification or disease classification.</param>
    private void LoadImageFromFileBrowser(string[] paths, IdentificationType identificationType) {
        Texture2D texture2D = new Texture2D(2, 2);
        byte[] imageData = File.ReadAllBytes(paths[0]);
        bool success = texture2D.LoadImage(imageData);
        Sprite imageSprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), CenterPivot);
        imageSprite.name = "plantSprite";

        if (imageData.Length == 0 || !success || !imageSprite) 
            imageSprite = defaultSprite;
        
        switch (identificationType) {
            case IdentificationType.IdentifyDisease:
                UploadingDiseaseUI.Instance.SetPlantImage(imageSprite);
                PlantDiseaseIdentifier.Instance.Classify(texture2D);
                break;
            case IdentificationType.IdentifyPlant: {
                UploadingPlantUI.Instance.SetFileBrowserImage(imageSprite);
                PlantIdentification.Instance.ClassifyImage(paths[0]);
                break;
            }
        }
    }

    /// <summary>
    /// The IdentifyPlant member of the IdentificationType enum is used to identify plant species
    /// based on the provided image data.
    /// This enumeration value triggers the corresponding classification process
    /// through the PlantIdentification system. It processes the image and assigns it
    /// to the relevant UI elements for display and further analysis.
    /// </summary>
    public enum IdentificationType {
        IdentifyPlant,
        IdentifyDisease
    }
}
