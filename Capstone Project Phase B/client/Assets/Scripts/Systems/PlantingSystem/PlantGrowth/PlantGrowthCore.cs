using DG.Tweening;
using LoM.Super;
using UnityEngine;

/// <summary>
/// The PlantGrowthCore class manages the growth behavior of a plant within a game environment.
/// It handles scale modifications, tracks the current state of the plant's growth, and integrates with tools
/// and environmental conditions to simulate realistic plant developments.
/// </summary>
public class PlantGrowthCore : SuperBehaviour {
    /// <summary>
    /// The threshold used to determine whether the scale difference is significant enough
    /// to trigger a scale update for the plant. If the difference in scale is below the
    /// square of this value, the scale update will not occur.
    /// </summary>
    private const float ScaleUpdateThreshold = 0.01f;

    /// <summary>
    /// Represents the squared distance threshold used to determine whether a full update
    /// of the plant's scale animation is necessary based on the player's distance from the plant.
    /// </summary>
    private const float FullUpdateDistanceSqr = 2500f;

    /// <summary>
    /// The duration of the growth animation applied when scaling a plant's size in the game.
    /// </summary>
    private const float GrowthAnimationDuration = 0.5f;

    /// <summary>
    /// Represents the interval time, in seconds, used for updating plant growth calculations and moisture reduction processes.
    /// </summary>
    public const float UpdateInterval = 1f;

    /// Represents the maximum scale a plant can achieve during its growth process.
    /// This constant is used as the default maximum size for plants when specific environmental
    /// or location-based scale values are not provided.
    private const float MaxScale = 0.5f;

    /// <summary>
    /// Gets the name of the plant.
    /// </summary>
    public string PlantName => plantSo.plantName; // TODO: Check if works, if not revert to plantName

    /// <summary>
    /// Represents the ScriptableObject associated with a plant in the game.
    /// Provides access to the plant's data, such as name, prefab, and sprite.
    /// </summary>
    public PlantSO PlantSo => plantSo;

    /// <summary>
    /// Represents the current scale of the plant. This value is used to determine the plant's growth over time.
    /// </summary>
    public double CurrentScale { get; private set; }

    /// <summary>
    /// Provides access to and manages interactions with tools related to the plantable area where the plant resides.
    /// </summary>
    public PlantableAreaToolsManager ToolsManager { get; private set; }

    /// <summary>
    /// Indicates whether the plant has reached its maximum scale.
    /// </summary>
    public bool ReachedMaxScale { get; private set; }

    /// <summary>
    /// Indicates whether the pruning shears are currently in collision with the plant's growth core.
    /// This property is updated based on collision events such as when pruning shears enter or exit contact
    /// with the growth core during gameplay interactions.
    /// </summary>
    public bool DoesPruningShearsCollide { get; private set; }

    /// <summary>
    /// The name of the plant as a string.
    /// </summary>
    [SerializeField] private string plantName;

    /// <summary>
    /// A serialized instance of the PlantSO class that represents the associated plant's scriptable object.
    /// </summary>
    [SerializeField] private PlantSO plantSo;

    /// <summary>
    /// Represents the environmental context in which the plant grows.
    /// Used to manage interactions with environmental factors such as
    /// light levels, humidity, and location-specific attributes.
    /// </summary>
    private PlantEnvironment _plantEnvironment;

    /// <summary>
    /// Represents the plant instance associated with the current plant growth core.
    /// Provides references to systems such as growth, water, disease, and fertilizer
    /// for orchestrating plant-related behaviors and interactions.
    /// </summary>
    private PlantInstance _plantInstance;

    /// <summary>
    /// Represents the initial scale of the plant as a Vector3.
    /// This value is set during the Awake lifecycle method based on the plant's
    /// initial transform scale. It is used as a reference for resetting the plant's growth
    /// or ensuring consistency in animation and scaling operations.
    /// </summary>
    private Vector3 _initialScale;

    /// <summary>
    /// Unity lifecycle method called when the script instance is being loaded.
    /// Initializes essential elements and data required for the plant's growth management system.
    /// </summary>
    private void Awake() {
        _plantInstance = GetComponent<PlantInstance>();
        _plantEnvironment = GetComponent<PlantEnvironment>();
        _initialScale = transform.localScale;
        CurrentScale = _initialScale.x;
    }

    /// Changes the scale of the plant based on the provided growth rate and environmental factors.
    /// The method ensures the plant's scale does not exceed the maximum allowed scale for its location and
    /// applies smooth scaling animation when necessary.
    /// <param name="growthRate">
    /// The rate at which the plant's scale should increase, adjusted by external factors such as
    /// environment and fertilizer.
    /// </param>
    public void ChangePlantScale(float growthRate) {
        if (!_plantInstance.IsPlanted) return;

        PlantLocationDetails details = _plantEnvironment.GetLocationDetails();
        double maxScale = details?.maxScale.x ?? MaxScale;

        CurrentScale = NumberExtensions.Min(CurrentScale + growthRate * UpdateInterval, maxScale);
        if (CurrentScale >= maxScale) {
            if (ReachedMaxScale) return;
            CurrentScale = maxScale;
            ReachedMaxScale = true;
            QuestManager.Instance.TrackGrowMaxScale();
            return;
        }

        float scaleDifference = transform.localScale.x - (float)CurrentScale;
        float playerDistance = (transform.position - Player.Instance.PlayerPosition).sqrMagnitude;

        if (scaleDifference < ScaleUpdateThreshold * ScaleUpdateThreshold || playerDistance > FullUpdateDistanceSqr)
            return;
        transform.DOKill();
        transform.DOScale((float)CurrentScale * Vector3.one, GrowthAnimationDuration).SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Fixed);
    }

    /// <summary>
    /// Determines whether the plant's current scale is enough for pruning.
    /// </summary>
    /// <returns>
    /// True if the plant's scale is at least 20% of its maximum scale; otherwise, false.
    /// </returns>
    public bool IsPlantScaleEnoughForPruning() {
        PlantLocationDetails details = _plantEnvironment.GetLocationDetails();
        Vector3 maxScale = details?.maxScale ?? 0.5f * Vector3.one;
        return transform.localScale.IsGreaterThan(0.2f * maxScale);
    }

    /// Sets the saved scale and updates the maximum scale status of the plant growth.
    /// <param name="scale">The new scale value to set for the plant.</param>
    /// <param name="reachedMaxScale">Indicates whether the plant's growth has reached its maximum scale.</param>
    public void SetSavedScale(double scale, bool reachedMaxScale) {
        CurrentScale = scale;
        transform.localScale = (float)scale * Vector3.one;
        ReachedMaxScale = reachedMaxScale;
    }

    /// <summary>
    /// Handles the collision logic when another object remains in contact with the plant
    /// during physics updates.
    /// </summary>
    /// <param name="other">The Collision object containing details about the other object
    /// that is in contact with this GameObject.</param>
    private void OnCollisionStay(Collision other) {
        GameObject tool = other.gameObject;
        if (tool == PlayerToolManager.Instance.GetTool(PlayerItem.PruningShears)) {
            DoesPruningShearsCollide = true;
            if (tool.TryGetComponent(out PruningShears pruningShears) && pruningShears.IsCutting)
                ToolsManager.PlantableArea.ApplyCure(PlayerItem.PruningShears);
        }
    }

    /// <summary>
    /// Called when this collider/rigidbody has stopped colliding with another rigidbody/collider.
    /// Resets the DoesPruningShearsCollide property to false if the collision involves the pruning shears.
    /// </summary>
    /// <param name="other">The collision information associated with the collider that is no longer colliding.</param>
    private void OnCollisionExit(Collision other) {
        if (other.gameObject == PlayerToolManager.Instance.GetTool(PlayerItem.PruningShears))
            DoesPruningShearsCollide = false;
    }

    /// <summary>
    /// Sets the tools manager with the provided instance.
    /// </summary>
    /// <param name="toolsManager">The tools manager instance to be set.</param>
    public void SetToolsManager(PlantableAreaToolsManager toolsManager) {
        ToolsManager = toolsManager;
    }

    /// <summary>
    /// Resets the growth state of the plant to its initial conditions.
    /// </summary>
    public void ResetGrowth() {
        ReachedMaxScale = false;
        DoesPruningShearsCollide = false;
        ToolsManager = null;
        CurrentScale = _initialScale.x;
        transform.localScale = _initialScale;
        transform.DOKill();
    }
}