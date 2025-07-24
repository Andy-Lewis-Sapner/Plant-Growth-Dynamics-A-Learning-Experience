using DG.Tweening;
using LoM.Super;
using UnityEngine;

/// <summary>
/// The PlantableAreaMaterialUpdater is responsible for updating the visual material of a plantable area
/// based on player proximity and environmental conditions.
/// </summary>
public class PlantableAreaMaterialUpdater : SuperBehaviour, IUpdateObserver {
    /// <summary>
    /// A constant that specifies the amount of alpha transparency change per unit of moisture.
    /// Primarily used in adjusting material transparency based on moisture levels in the environment.
    /// </summary>
    private const float AlphaChangePerMoisture = 0.007f;

    /// <summary>
    /// Represents the time interval in seconds after which the update logic is executed.
    /// </summary>
    private const float UpdateInterval = 0.5f;

    /// <summary>
    /// Represents the square of the distance within which the player is considered near the ground
    /// for updating the material of the plantable area.
    /// </summary>
    private const float UpdateMaterialDistanceSqr = 9f;

    /// <summary>
    /// A material used to represent the ground's appearance when the player is near.
    /// </summary>
    [SerializeField] private Material playerIsNearGroundMaterial;

    /// <summary>
    /// A material used to visually represent a watered state on the ground.
    /// </summary>
    [SerializeField] private Material wateredBrownMaterial;

    /// <summary>
    /// References an instance of the <see cref="PlantableArea"/> component attached to the same GameObject.
    /// Used to manage properties and functionality specific to the plantable area, such as planting interactions
    /// and determining environmental settings.
    /// </summary>
    private PlantableArea _plantableArea;

    /// <summary>
    /// The renderer component responsible for managing the material applied to the object.
    /// </summary>
    private Renderer _materialRenderer;

    /// <summary>
    /// Represents the state indicating whether the player was near the ground in the previous frame.
    /// </summary>
    private bool _wasPlayerNearGroundLastFrame;

    /// <summary>
    /// Tracks the time elapsed since the last update operation in seconds.
    /// </summary>
    private float _timeSinceLastUpdate;

    /// <summary>
    /// Unity lifecycle method called when the script instance is being loaded.
    /// Initializes the required components for managing plantable area materials.
    /// </summary>
    private void Awake() {
        _plantableArea = GetComponent<PlantableArea>();
        _materialRenderer = GetComponent<Renderer>();
        _materialRenderer.sharedMaterial = wateredBrownMaterial;
    }

    /// <summary>
    /// Method called as part of the observer pattern to process updates depending on the state of the game.
    /// </summary>
    public void ObservedUpdate() {
        if (_plantableArea.Environment != Environment.Ground) return;
        
        _timeSinceLastUpdate += Time.deltaTime;
        if (_timeSinceLastUpdate < UpdateInterval) return;
        _timeSinceLastUpdate -= UpdateInterval;
        
        float sqrDistance = (transform.position - Player.Instance.PlayerPosition).sqrMagnitude;
        bool isPlayerNearGround = sqrDistance <= UpdateMaterialDistanceSqr;

        if (!_plantableArea.PlantInstance && isPlayerNearGround != _wasPlayerNearGroundLastFrame)
            UpdateMaterial(isPlayerNearGround);
        _wasPlayerNearGroundLastFrame = isPlayerNearGround;
    }

    /// <summary>
    /// Updates the material of the plantable area based on the player's proximity.
    /// </summary>
    /// <param name="isPlayerNearGround">Indicates whether the player is near the plantable area.</param>
    public void UpdateMaterial(bool isPlayerNearGround) {
        _materialRenderer.sharedMaterial = isPlayerNearGround
            ? playerIsNearGroundMaterial
            : wateredBrownMaterial;
    }

    /// <summary>
    /// Updates the moisture display of the material by adjusting its alpha value
    /// based on the given moisture level. The transition to the new alpha value is animated.
    /// </summary>
    /// <param name="moisture">The current moisture level to be visually represented.</param>
    public void UpdateMoistureMaterial(float moisture) {
        Material material = _materialRenderer.material;

        float newAlphaValue = Mathf.Clamp(moisture * AlphaChangePerMoisture, 0f, 0.7f);
        DOTween.To(() => material.color.a,
            x => material.color = new Color(material.color.r, material.color.g, material.color.b, x), newAlphaValue,
            1f);
    }

    /// <summary>
    /// Called when the object becomes enabled and active in the scene.
    /// Registers the current instance as an update observer for the UpdateManager
    /// if the associated PlantableArea's environment is set to Ground.
    /// </summary>
    private void OnEnable() {
        if (_plantableArea && _plantableArea.Environment == Environment.Ground)
            UpdateManager.RegisterObserver(this);
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Unregisters the current instance from the <see cref="UpdateManager"/>
    /// if the associated <see cref="PlantableArea"/> has its environment set to <see cref="Environment.Ground"/>.
    /// This ensures the instance does not continue to receive update notifications when it is no longer in use.
    /// </summary>
    private void OnDisable() {
        if (_plantableArea && _plantableArea.Environment == Environment.Ground)
            UpdateManager.UnregisterObserver(this);
    }
}