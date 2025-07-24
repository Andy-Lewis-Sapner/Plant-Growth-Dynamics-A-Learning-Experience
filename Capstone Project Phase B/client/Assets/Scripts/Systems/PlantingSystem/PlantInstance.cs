using System;
using System.Globalization;
using LoM.Super;

/// <summary>
/// Manages a plant instance with its growth, water, disease, environment, and fertilizer systems.
/// </summary>
public class PlantInstance : SuperBehaviour {
    public PlantGrowthCore PlantGrowthCore { get; private set; } // Core growth system
    public PlantWaterSystem PlantWaterSystem { get; private set; } // Water management system
    public PlantDiseaseSystem PlantDiseaseSystem { get; private set; } // Disease management system
    public PlantEnvironment PlantEnvironment { get; private set; } // Environment settings
    public PlantFertilizerSystem PlantFertilizerSystem { get; private set; } // Fertilizer system
    public bool IsPlanted { get; private set; } // Tracks if the plant is planted
    
    private PlantableArea _plantableArea; // Associated plantable area
    private PlantData _plantData; // Data for the plant instance

    /// <summary>
    /// Initializes plant systems on awake.
    /// </summary>
    private void Awake() {
        PlantGrowthCore = GetComponent<PlantGrowthCore>();
        PlantWaterSystem = GetComponent<PlantWaterSystem>();
        PlantDiseaseSystem = GetComponent<PlantDiseaseSystem>();
        PlantEnvironment = GetComponent<PlantEnvironment>();
        PlantFertilizerSystem = GetComponent<PlantFertilizerSystem>();
    }

    /// <summary>
    /// Initializes the plant with data and associates it with a plantable area.
    /// </summary>
    /// <param name="data">Plant data to initialize with.</param>
    /// <param name="area">Plantable area to associate with.</param>
    public void Initialize(PlantData data, PlantableArea area) {
        _plantData = data;
        SetPlantableArea(area);
        PlantGrowthCore.SetSavedScale(_plantData.scale, _plantData.reachedMaxScale);
        PlantDiseaseSystem.SetDiseaseFromPlantData(_plantData.disease, _plantData.diseaseProgress,
            _plantData.diseaseSlowingGrowthFactor, _plantData.lastDiseaseCheck);
        PlantWaterSystem.SetSavedMoistureLevel(_plantData.moistureLevel);
        PlantFertilizerSystem.SetSavedFertilizer(
            _plantData.nutrientLevel,
            _plantData.remainingEffectTime,
            _plantData.fertilizerName
        );
        if (area.Environment == Environment.Ground)
            PlantGrowthCore.ToolsManager.ShadeTent.SetOpened(_plantData.shadeTentCounter, area);
    }

    /// <summary>
    /// Resets the plant instance for object pooling.
    /// </summary>
    public void ResetForPool() {
        _plantableArea.RemovePlant();
        IsPlanted = false;
        _plantData = null;
        _plantableArea = null;
        PlantGrowthCore.ResetGrowth();
        PlantWaterSystem.ResetSystem();
        PlantDiseaseSystem.ResetSystem();
        PlantFertilizerSystem.ResetSystem();
    }
    
    /// <summary>
    /// Updates and returns the plant's data with current state.
    /// </summary>
    /// <returns>Updated plant data.</returns>
    public PlantData UpdatePlantData() {
        _plantData ??= new PlantData {
            plantId = Guid.NewGuid().ToString(),
            username = DataManager.Instance.LoggedInUsername
        };
        
        _plantData.plantName = PlantGrowthCore.PlantName;
        _plantData.position = Vector3Data.FromVector3(transform.position);
        _plantData.plantableArea = _plantableArea.GetHierarchyPath();
        _plantData.scale = PlantGrowthCore.CurrentScale;
        _plantData.disease = PlantDiseaseSystem.GetDiseaseName();
        _plantData.diseaseProgress = PlantDiseaseSystem.DiseaseProgress;
        _plantData.plantingLocationType = PlantEnvironment.Environment.ToString();
        _plantData.moistureLevel = PlantWaterSystem.MoistureLevel;
        _plantData.lastGrowthUpdate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        _plantData.lastDiseaseCheck = PlantDiseaseSystem.LastDiseaseCheck.ToString("o", CultureInfo.InvariantCulture);
        _plantData.diseaseSlowingGrowthFactor = PlantDiseaseSystem.DiseaseSlowingGrowthFactor;
        if (_plantableArea.Environment == Environment.Ground)
            _plantData.shadeTentCounter = _plantableArea.ToolsManager.ShadeTent.TimeOpened;
        _plantData.reachedMaxScale = PlantGrowthCore.ReachedMaxScale;
        _plantData.nutrientLevel = PlantFertilizerSystem.NutrientLevel;
        _plantData.remainingEffectTime = PlantFertilizerSystem.RemainingEffectTime;
        _plantData.fertilizerName = PlantFertilizerSystem.FertilizerName;

        return _plantData;
    }

    /// <summary>
    /// Sets the plantable area and marks the plant as planted.
    /// </summary>
    /// <param name="area">The plantable area to associate with.</param>
    public void SetPlantableArea(PlantableArea area) {
        _plantableArea = area;
        IsPlanted = true;
    }
}