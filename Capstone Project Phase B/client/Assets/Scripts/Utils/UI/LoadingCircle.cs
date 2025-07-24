using LoM.Super;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a rotating loading circle UI with progress fill and visibility toggling.
/// </summary>
public class LoadingCircle : SuperBehaviour, IUpdateObserver {
    private const float RotateSpeed = 100f; // Speed of the loading circle rotation
    
    [SerializeField] private RectTransform progressRect; // RectTransform for rotation
    [SerializeField] private Image loadingCircleImage; // Image for the loading circle
    [SerializeField] private Image progressImage; // Image for the progress fill
    
    private Color _loadingCircleColorHidden; // Hidden color for loading circle
    private Color _loadingCircleColorVisible; // Visible color for loading circle
    private Color _progressColorHidden; // Hidden color for progress fill
    private Color _progressColorVisible; // Visible color for progress fill

    /// <summary>
    /// Initializes colors and subscribes to growth toggle events.
    /// </summary>
    private void Start() {
        PlantGrowthManager.Instance.OnAdvanceGrowthToggled += PlantsGrowthManagerOnAdvanceGrowthToggled;
        
        SetColors();
        progressImage.color = _progressColorHidden;
        loadingCircleImage.color = _loadingCircleColorHidden;
    }

    /// <summary>
    /// Sets visible and hidden colors for the loading circle and progress images.
    /// </summary>
    private void SetColors() {
        _loadingCircleColorVisible = loadingCircleImage.color;
        _loadingCircleColorHidden = new Color(_loadingCircleColorHidden.r, _loadingCircleColorHidden.g,
            _loadingCircleColorHidden.b, 0f);
        _progressColorVisible = progressImage.color;
        _progressColorHidden = new Color(_progressColorHidden.r, _progressColorHidden.g, _progressColorHidden.b, 0f);
    }

    /// <summary>
    /// Updates the rotation and progress fill of the loading circle.
    /// </summary>
    public void ObservedUpdate() {
        progressRect.Rotate(0f, 0f, RotateSpeed * Time.deltaTime);
        progressImage.fillAmount = PlantGrowthManager.Instance.AdvancementProgress;
    }

    /// <summary>
    /// Toggles visibility and update registration based on growth state.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="state">Growth toggle state.</param>
    private void PlantsGrowthManagerOnAdvanceGrowthToggled(object sender, bool state) {
        progressImage.color = state ? _progressColorVisible : _progressColorHidden;
        loadingCircleImage.color = state ? _loadingCircleColorVisible : _loadingCircleColorHidden;

        if (state) {
            progressImage.fillAmount = 0f;
            UpdateManager.RegisterObserver(this);
        } else {
            UpdateManager.UnregisterObserver(this);
        }
    }

    /// <summary>
    /// Unsubscribes from growth toggle events on destroy.
    /// </summary>
    private void OnDestroy() {
        PlantGrowthManager.Instance.OnAdvanceGrowthToggled -= PlantsGrowthManagerOnAdvanceGrowthToggled;
    }
}