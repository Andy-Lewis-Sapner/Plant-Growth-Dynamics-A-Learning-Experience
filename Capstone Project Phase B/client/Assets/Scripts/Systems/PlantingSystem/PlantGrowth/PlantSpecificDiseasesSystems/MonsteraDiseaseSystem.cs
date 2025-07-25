using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages the disease system for Monstera plants, including checking for diseases,
/// updating the progress of existing diseases, applying effects, and handling cures.
/// </summary>
public class MonsteraDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// Represents the moisture threshold above which the Root Rot disease may develop in the plant.
    /// </summary>
    private const float RootRotMoistureThreshold = 80f;

    /// <summary>
    /// Represents the humidity threshold below which the plant becomes susceptible to spider mites infestation.
    /// </summary>
    private const float SpiderMitesHumidityThreshold = 40f;

    /// <summary>
    /// Specifies the humidity threshold below which the Monstera plant becomes susceptible to Mealybugs infestation.
    /// </summary>
    /// <value>
    /// A constant floating-point value indicating the humidity percentage required to prevent
    /// the risk of Mealybugs infestation. Measured in percentage.
    /// </value>
    private const float MealybugsHumidityThreshold = 50f;

    /// <summary>
    /// A constant value that determines the rate at which a disease progresses in the Monstera disease system.
    /// </summary>
    private const float DiseaseProgressRate = 0.03f;

    /// <summary>
    /// Represents the types of diseases that a plant can experience within the system.
    /// </summary>
    private new enum Disease : byte {
        None,
        RootRot,
        SpiderMites,
        Mealybugs
    }

    /// <summary>
    /// Gets or sets the disease currently affecting the plant.
    /// </summary>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// A static, read-only list containing the cures (of type PlayerItem) applicable for various plant diseases in the
    /// MonsteraDiseaseSystem. This list defines specific items that can be used to treat certain diseases identified within the system.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.FungicideSpray, PlayerItem.InsecticideSoap, PlayerItem.NeemOil
    }.AsReadOnly();

    /// <summary>
    /// Gets an immutable list of items that are used to cure diseases affecting plants.
    /// </summary>
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// <summary>
    /// Checks if the Monstera plant contracts any diseases based on current environmental conditions
    /// such as moisture and humidity levels. Updates the disease state and initializes disease
    /// progression if a disease is detected. Diseases include Root Rot, Spider Mites, and Mealybugs.
    /// </summary>
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        if (moisture > RootRotMoistureThreshold && Random.value < 0.05f) {
            CurrentDisease = Disease.RootRot;
            DiseaseProgress = 0f;
        } else
            switch (humidity) {
                case < SpiderMitesHumidityThreshold when Random.value < 0.04f:
                    CurrentDisease = Disease.SpiderMites;
                    DiseaseProgress = 0f;
                    break;
                case < MealybugsHumidityThreshold when Random.value < 0.03f:
                    CurrentDisease = Disease.Mealybugs;
                    DiseaseProgress = 0f;
                    break;
            }
    }

    /// Updates the progress of the current disease based on environmental factors
    /// such as soil moisture and air humidity. This method increases disease severity
    /// if conditions favor the progression of the disease.
    /// The disease progress is capped at a maximum value and triggers the application
    /// of related disease effects. If no active disease is present, the method exits early.
    public override void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        switch (CurrentDisease) {
            case Disease.RootRot:
                if (moisture > RootRotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.SpiderMites:
                if (humidity < SpiderMitesHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.Mealybugs:
                if (humidity < MealybugsHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyDiseaseEffects();
    }

    /// <summary>
    /// Applies the effects of the active disease to the plant by adjusting its growth rate based on the disease type
    /// and severity. This method modifies the DiseaseSlowingGrowthFactor depending on the current disease and
    /// its progress.
    /// </summary>
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.SpiderMites => Mathf.Lerp(1f, 0.7f, severity),
            Disease.Mealybugs => Mathf.Lerp(1f, 0.7f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// <summary>
    /// Applies a cure to the current disease affecting the plant, if applicable.
    /// </summary>
    /// <param name="cureItem">
    /// The item used to attempt curing the disease. Only specific items are effective
    /// against certain diseases, and the cure action will succeed only if the provided
    /// item matches the current disease's cure.
    /// </param>
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.FungicideSpray when CurrentDisease == Disease.RootRot:
                PlantWaterSystem.ReduceMoisture(20f);
                CureDisease();
                break;
            case PlayerItem.InsecticideSoap when CurrentDisease == Disease.SpiderMites:
            case PlayerItem.NeemOil when CurrentDisease == Disease.Mealybugs:
                CureDisease();
                break;
        }
    }

    /// Returns the name of the current disease by converting the enumeration value
    /// to a readable string format with camel-case separation.
    /// <returns>The name of the current disease as a formatted string.</returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// <summary>
    /// Retrieves the name of the cure specific to the current disease affecting the plant.
    /// </summary>
    /// <returns>
    /// A string representing the name of the cure for the current disease. If no specific disease is present, returns "Unknown".
    /// </returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Fungicide Spray",
            Disease.SpiderMites => "Insecticidal Soap",
            Disease.Mealybugs => "Neem Oil",
            _ => "Unknown"
        };
    }

    /// Retrieves the list of items required to cure the current disease affecting the plant.
    /// <returns>
    /// A list of PlayerItem objects representing the cure items for the current disease.
    /// If there is no disease, an empty list is returned.
    /// </returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.FungicideSpray },
            Disease.SpiderMites => new List<PlayerItem> { PlayerItem.InsecticideSoap },
            Disease.Mealybugs => new List<PlayerItem> { PlayerItem.NeemOil },
            _ => new List<PlayerItem>()
        };
    }

    /// <summary>
    /// Assigns disease-related data to the plant disease system from the provided plant data.
    /// </summary>
    /// <param name="plantDataDisease">The name of the disease as a string from the plant data.</param>
    /// <param name="plantDataDiseaseProgress">The progress of the disease as a float, where higher values indicate greater severity.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">A float value determining how much the disease slows the plant's growth.</param>
    /// <param name="plantDataLastDiseaseCheck">A string representing the last time the disease was checked, formatted as a date and time.</param>
    public override void SetDiseaseFromPlantData(string plantDataDisease, float plantDataDiseaseProgress,
        float plantDataDiseaseSlowingGrowthFactor, string plantDataLastDiseaseCheck) {
        DiseaseProgress = plantDataDiseaseProgress;
        DiseaseSlowingGrowthFactor = plantDataDiseaseSlowingGrowthFactor;
        try {
            CurrentDisease = (Disease)Enum.Parse(typeof(Disease), plantDataDisease.RemoveSpaces());
            LastDiseaseCheck = DateTimeOffset.Parse(plantDataLastDiseaseCheck, CultureInfo.InvariantCulture);
        } catch (Exception) {
            CurrentDisease = Disease.None;
            LastDiseaseCheck = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Retrieves the names of all diseases.
    /// </summary>
    /// <returns>A list of strings containing the names of all diseases.</returns>
    public override List<string> GetDiseasesNames() {
        return Enum.GetNames(typeof(Disease)).ToList();
    }

    /// <summary>
    /// Maps a model label to a disease name.
    /// </summary>
    /// <param name="modelLabel">The model label to map.</param>
    /// <returns>The mapped disease name.</returns>
    public override string MapModelLabelToDiseaseName(string modelLabel) {
        return modelLabel switch {
            "Healthy" => nameof(Disease.None),
            "RootRot" => nameof(Disease.RootRot),
            "SpiderMites" => nameof(Disease.SpiderMites),
            "PowderyMildew" => nameof(Disease.Mealybugs),
            _ => nameof(Disease.None)
        };
    }
}