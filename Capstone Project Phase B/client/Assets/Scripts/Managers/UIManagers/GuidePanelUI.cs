using System;
using System.Collections;
using System.Linq;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanelUI : Singleton<GuidePanelUI> {
    private const float SlideDuration = 0.25f; // Duration of panel sliding animation

    [Header("Guidance Panel")] [SerializeField]
    private RectTransform guidancePanel; // The main guidance panel UI element

    [Header("Guides")] [SerializedDictionary("Tool", "Guide")] [SerializeField]
    private SerializedDictionary<PlayerItem, GameObject> toolsGuides; // Mapping of tools to their corresponding guides

    [SerializeField] private GameObject interactGuide; // Guide shown for interaction actions

    [Header("UI Elements")] [SerializeField]
    private Slider moistureSlider; // UI slider to show soil moisture

    [SerializeField] private TextMeshProUGUI moistureText; // Text showing the moisture percentage
    [SerializeField] private TextMeshProUGUI scissorsMoveRotateText1; // First text element for scissors state
    [SerializeField] private TextMeshProUGUI scissorsMoveRotateText2; // Second text element for scissors state
    [SerializeField] private TextMeshProUGUI fertilizerType; // Text showing fertilizer name
    [SerializeField] private TextMeshProUGUI fertilizerAmount; // Text showing fertilizer amount

    private GameObject[] _guideValues; // Array of all guide GameObjects
    private Vector2 _guidancePanelHiddenPos; // Hidden panel position (offscreen)
    private Vector2 _guidancePanelVisiblePos; // Visible panel position (onscreen)
    private Tween _guidancePanelTween; // Tween animation for panel sliding

    // Initialize UI state
    private void Start() {
        _guideValues = toolsGuides.Values.ToArray();
        SetAllGuidesInvisible();

        if (guidancePanel) {
            _guidancePanelVisiblePos = guidancePanel.anchoredPosition;
            _guidancePanelHiddenPos = new Vector2(-guidancePanel.rect.width - 10, guidancePanel.anchoredPosition.y);
            guidancePanel.anchoredPosition = _guidancePanelHiddenPos;
        }
    }

    // Disables all tool and interact guides
    private void SetAllGuidesInvisible() {
        Span<GameObject> guides = _guideValues.AsSpan();
        foreach (GameObject guide in guides)
            if (guide && guide.activeSelf)
                guide.SetActive(false);

        if (interactGuide && interactGuide.activeSelf) interactGuide.SetActive(false);
    }

    // Activates or deactivates a specific tool guide
    public void SetToolGuideActive(PlayerItem playerItem, bool isActive) {
        if (toolsGuides.TryGetValue(playerItem, out GameObject toolGuide))
            UpdateGuidanceVisibility(toolGuide, isActive);
    }

    // Updates the moisture slider and percentage text
    public void SetMoisture(float moisture) {
        moistureSlider.value = moisture;
        moistureText.text = $"{moisture:F0}%";
    }

    // Activates or deactivates the interaction guide
    public void SetInteractGuide(bool isActive) {
        UpdateGuidanceVisibility(interactGuide, isActive);
    }

    // Handles showing/hiding the guidance panel with animation
    private void UpdateGuidanceVisibility(GameObject guide, bool isActive) {
        if (!guide || !gameObject.activeInHierarchy) return;

        if (isActive) {
            if (HasOtherActiveGuides(guide)) {
                // Slide panel out before switching guides
                _guidancePanelTween = guidancePanel.DOAnchorPos(_guidancePanelHiddenPos, SlideDuration)
                    .SetEase(Ease.InQuad).OnComplete(() => {
                        DeactivateOtherGuides(guide);
                        StartCoroutine(SlideGuidePanel(guide, true));
                    });
            } else {
                StartCoroutine(SlideGuidePanel(guide, true));
            }
        } else {
            StartCoroutine(SlideGuidePanel(guide, false));
        }
    }

    // Coroutine to slide the panel in or out and activate/deactivate guide
    private IEnumerator SlideGuidePanel(GameObject guide, bool isActive) {
        if (_guidancePanelTween.IsActive()) yield return _guidancePanelTween.WaitForCompletion();

        if (isActive) {
            _guidancePanelTween = guidancePanel.DOAnchorPos(_guidancePanelVisiblePos, SlideDuration)
                .SetEase(Ease.OutQuad)
                .OnStart(() => guide.SetActive(true));
        } else {
            _guidancePanelTween = guidancePanel.DOAnchorPos(_guidancePanelHiddenPos, SlideDuration).SetEase(Ease.InQuad)
                .OnComplete(() => guide.SetActive(false));
        }
    }

    // Checks if other guides (except the given one) are currently active
    private bool HasOtherActiveGuides(GameObject excludeGuide) {
        Span<GameObject> guides = _guideValues.AsSpan();
        foreach (GameObject otherGuide in guides) {
            if (otherGuide && otherGuide != excludeGuide && otherGuide.activeSelf) return true;
        }

        return false;
    }

    // Deactivates all guides except the given one
    private void DeactivateOtherGuides(GameObject excludeGuide) {
        Span<GameObject> guides = _guideValues.AsSpan();
        foreach (GameObject otherGuide in guides) {
            if (otherGuide && otherGuide != excludeGuide && otherGuide.activeSelf) otherGuide.SetActive(false);
        }
    }

    // Updates the scissors control text to show either "Move" or "Rotate"
    public void SwitchScissorsMoveRotateText(bool isMoveState) {
        string text = isMoveState ? "Move" : "Rotate";
        scissorsMoveRotateText1.text = text;
        scissorsMoveRotateText2.text = text;
    }

    // Updates the fertilizer type and amount display
    public void SetFertilizerTypeAndAmount(FertilizerSO fertilizer, int amount) {
        if (!fertilizer)
            fertilizerType.text = "Fertilizer: None";
        else if (!fertilizerType.text.Contains(fertilizer.fertilizerName))
            fertilizerType.text = $"Fertilizer: {fertilizer.fertilizerName}";

        fertilizerAmount.text = $"Amount: {amount}";
    }

    // Kills the tween if still running to avoid animation errors
    private void OnDestroy() {
        _guidancePanelTween?.Kill();
    }
}