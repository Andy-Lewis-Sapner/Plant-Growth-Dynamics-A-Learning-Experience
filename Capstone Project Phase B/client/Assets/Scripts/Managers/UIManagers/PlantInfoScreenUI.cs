using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// This class controls the UI screen that displays information about a selected plant
public class PlantInfoScreenUI : UIScreen<PlantInfoScreenUI> {
    [SerializeField] private TypeWriterEffect plantName; // UI element for displaying the plant's name
    [SerializeField] private Image plantImage; // UI element for showing the plant's image
    [SerializeField] private TypeWriterEffect plantDescription; // UI element for showing the plant's description
    [SerializeField] private TypeWriterEffect plantRecommendation; // UI element for showing planting recommendation

    private string _lastPlantName; // Stores the last displayed plant name to avoid redundant updates
    private string _cachedRecommendation; // Caches the last selected recommendation string

    // Updates the screen with information from a given PlantSO
    public void SetScreenInfo(PlantSO plantSo) {
        if (!plantSo) return; // Exit if the provided plant is null

        // Set the plant name if it has changed
        if (plantName.Text != plantSo.plantName) plantName.Text = plantSo.plantName;

        // Set the plant description if it has changed
        if (plantDescription.Text != plantSo.briefDescription) plantDescription.Text = plantSo.briefDescription;

        // Always update the image sprite
        plantImage.sprite = plantSo.plantSprite;

        // If the plant is new or there is no cached recommendation, choose a new one randomly
        if (_lastPlantName != plantSo.plantName || string.IsNullOrEmpty(_cachedRecommendation)) {
            int growingRecommendations = plantSo.growingRecommendations.Count;
            _cachedRecommendation = growingRecommendations > 0
                ? plantSo.growingRecommendations
                    [Random.Range(0, growingRecommendations)] // Pick a random recommendation
                : string.Empty;

            _lastPlantName = plantSo.plantName; // Update the cached plant name
        }

        // Set the recommendation text
        plantRecommendation.Text = _cachedRecommendation;
    }
}