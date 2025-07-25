using System;
using System.Collections.Generic;
using LoM.Super;
using UnityEngine;

/// <summary>
/// The PlantDiseaseSystem class serves as the base abstract class
/// for implementing disease management systems in various plant species.
/// It provides functionality for monitoring and managing plant diseases.
/// </summary>
public abstract class PlantDiseaseSystem : SuperBehaviour {
    /// <summary>
    /// An event triggered when there is a change in the disease state of a plant.
    /// This event is raised whenever the current disease on the plant changes,
    /// specifically when transitioning between having no disease and being diseased.
    /// Subscribers to this event will receive a boolean argument indicating whether a disease is present (true) or absent (false).
    /// </summary>
    public event EventHandler<bool> OnDiseaseChanged;

    /// Represents the base system for managing plant diseases. This class provides
    /// the foundation for handling diseases in plants, including properties and methods
    /// for disease progress, effects, cures, and disease-specific actions.
    protected enum Disease : byte {
        None
    }

    /// <summary>
    /// Gets or sets the current disease affecting the plant.
    /// This property represents the disease currently impacting the plant's health.
    /// Changing the value triggers the <c>OnDiseaseChanged</c> event if the new disease state is different.
    /// </summary>
    protected Disease CurrentDisease {
        get => _currentDisease;
        set {
            if (_currentDisease == value) return;
            _currentDisease = value;
            OnDiseaseChanged?.Invoke(this, _currentDisease != Disease.None);
        }
    }

    /// <summary>
    /// Represents a collection of player items that can be used to cure specific plant diseases.
    /// </summary>
    public abstract IReadOnlyList<PlayerItem> CuresForDiseases { get; }

    /// <summary>
    /// Represents the progression level of the current disease affecting the plant.
    /// </summary>
    public float DiseaseProgress { get; protected set; }

    /// <summary>
    /// Represents a factor modifying the growth rate of a plant under the influence of a disease.
    /// </summary>
    /// <value>
    /// A float representing the slowing effect on plant growth due to the active disease.
    /// </value>
    public float DiseaseSlowingGrowthFactor { get; protected set; } = 1f;

    /// <summary>
    /// Represents the timestamp of the last disease check performed on the plant.
    /// </summary>
    public DateTimeOffset LastDiseaseCheck { get; set; }

    /// <summary>
    /// Represents the current disease affecting the plant within the plant disease system.
    /// Changes to this field trigger the <see cref="OnDiseaseChanged"/> event to notify subscribers about the updated disease state.
    /// Set to <see cref="Disease.None"/> by default, indicating no active disease.
    /// </summary>
    private Disease _currentDisease = Disease.None;

    /// <summary>
    /// Represents the environment settings related to a plant, interacting as part of the broader disease
    /// and growth systems.
    /// </summary>
    protected PlantEnvironment PlantEnvironment;

    /// <summary>
    /// Represents the system responsible for managing water-related functionalities of a plant.
    /// This system is designed to monitor, calculate, and adjust the moisture levels of the plant
    /// to ensure optimal hydration and prevent diseases caused by improper moisture conditions.
    /// </summary>
    protected PlantWaterSystem PlantWaterSystem;

    /// Initializes the PlantDiseaseSystem by setting up references to the
    /// PlantEnvironment and PlantWaterSystem components. This method is called
    /// when the script instance is being loaded.
    private void Awake() {
        PlantEnvironment = GetComponent<PlantEnvironment>();
        PlantWaterSystem = GetComponent<PlantWaterSystem>();
    }

    /// Updates the progress of the current disease affecting the plant.
    /// This method determines the progression of a disease based on internal factors
    /// such as disease severity or elapsed time. If no disease is present, the method
    /// exits without performing any operations. Otherwise, it applies the effects of the
    /// disease to the plant. This method can be overridden by subclasses to implement
    /// specific behavior for different types of plants or diseases.
    public virtual void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;
        ApplyDiseaseEffects();
    }

    /// <summary>
    /// Applies the disease effects to the plant system by modifying the relevant growth and health parameters.
    /// This method is typically overridden in derived classes to implement specific behavior for different plant types.
    /// Default implementation interpolates the DiseaseSlowingGrowthFactor based on the disease progression.
    /// </summary>
    protected virtual void ApplyDiseaseEffects() {
        DiseaseSlowingGrowthFactor = Mathf.Lerp(1f, 0.5f, DiseaseProgress);
    }

    /// Removes the current disease from the plant, resetting all associated disease-related properties.
    /// This method sets the current disease to `None`, resets the disease progress to `0%`,
    /// and restores the disease-slowing growth factor to its default value of `1.0`. Additionally,
    /// it triggers the `TrackCureDisease` method in the `QuestManager` to potentially track any quest progress related to curing diseases.
    protected void CureDisease() {
        CurrentDisease = Disease.None;
        DiseaseProgress = 0f;
        DiseaseSlowingGrowthFactor = 1f;
        QuestManager.Instance.TrackCureDisease();
    }

    /// <summary>
    /// Determines the severity of the current disease affecting a plant based on the disease progress.
    /// </summary>
    /// <returns>
    /// A string representing the severity level of the disease.
    /// Possible values include: "None", "Minor", "Moderate", "Severe", or "Critical".
    /// </returns>
    public string GetDiseaseSeverity() {
        return DiseaseProgress switch {
            <= 0f => "None",
            < 0.25f => "Minor",
            < 0.5f => "Moderate",
            < 0.75f => "Severe",
            _ => "Critical"
        };
    }

    /// <summary>
    /// Resets the plant disease system to its default state, clearing any current disease
    /// and resetting related properties.
    /// </summary>
    public void ResetSystem() {
        CurrentDisease = Disease.None;
        DiseaseProgress = 0f;
        DiseaseSlowingGrowthFactor = 1f;
        LastDiseaseCheck = DateTimeOffset.Now;
    }

    /// <summary>
    /// Checks for the presence of diseases in the plant system and updates the
    /// current disease state if any conditions for diseases are met.
    /// </summary>
    public abstract void CheckForDiseases();

    /// <summary>
    /// Applies a cure to the plant's current disease using the specified item, if applicable.
    /// </summary>
    /// <param name="cureItem">The item used to apply the cure. Must match the required cure item for the current disease.</param>
    public abstract void ApplyCure(PlayerItem cureItem);

    /// <summary>
    /// Retrieves the name of the disease affecting the plant.
    /// </summary>
    /// <returns>The name of the plant disease as a string.</returns>
    public abstract string GetDiseaseName();

    /// Retrieves the name of the cure for the current disease affecting the plant.
    /// <returns>
    /// A string representing the name of the cure for the current disease.
    /// </returns>
    public abstract string GetDiseaseCureName();

    /// Retrieves a list of items that can be used to cure the current disease in the plant disease system.
    /// <returns>
    /// A list of PlayerItem representing the cure items for the current disease.
    /// </returns>
    public abstract List<PlayerItem> GetDiseaseCureItem();

    /// <summary>
    /// Abstract method to set the disease attributes for a plant based on provided data.
    /// </summary>
    /// <param name="plantDataDisease">The name or type of the disease affecting the plant.</param>
    /// <param name="plantDataDiseaseProgress">The progress level of the disease, represented as a floating-point value.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">The factor by which the disease is slowing the plant's growth, represented as a floating-point value.</param>
    /// <param name="plantDataLastDiseaseCheck">A timestamp or identifier indicating the most recent disease check for the plant.</param>
    public abstract void SetDiseaseFromPlantData(string plantDataDisease, float plantDataDiseaseProgress,
        float plantDataDiseaseSlowingGrowthFactor, string plantDataLastDiseaseCheck);

    /// <summary>
    /// Retrieves a list of possible diseases for a plant.
    /// </summary>
    /// <returns>A list of strings representing the possible diseases for the plant.</returns>
    public abstract List<string> GetDiseasesNames();

    /// <summary>
    /// Maps a model label to a disease name.
    /// </summary>
    /// <param name="modelLabel">The model label to map.</param>
    /// <returns>The mapped disease name.</returns>
    public abstract string MapModelLabelToDiseaseName(string modelLabel);
}