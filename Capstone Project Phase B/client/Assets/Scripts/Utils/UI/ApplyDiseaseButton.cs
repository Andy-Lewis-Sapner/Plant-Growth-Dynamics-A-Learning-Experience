using System;
using LoM.Super;

public class ApplyDiseaseButton : SuperBehaviour {
    private const string OffsetFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ"; // ISO 8601
    
    private string _diseaseName; // The name of the disease
    private PlantInstance _currentPlant; // The current plant

    // Button listener to apply the disease (same as when loading saved game)
    public void ApplyDisease() {
        _currentPlant.PlantDiseaseSystem.SetDiseaseFromPlantData(_diseaseName, 0f, 1f,
            DateTimeOffset.UtcNow.ToString(OffsetFormat));
        UploadingDiseaseUI.Instance.CloseScreen();
    }

    // Set the properties of the button
    public void SetProperties(string diseaseName, PlantInstance currentPlant) {
        _diseaseName = diseaseName;
        _currentPlant = currentPlant;
    }
}