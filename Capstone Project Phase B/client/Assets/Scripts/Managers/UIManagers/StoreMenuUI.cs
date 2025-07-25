using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Manages the store menu UI where players can buy tools, seeds, and fertilizers
public class StoreMenuUI : UIScreen<StoreMenuUI> {
    private readonly Dictionary<PlantSO, int> _seedQuantities = new(); // Stores selected seed quantities

    private readonly Dictionary<FertilizerSO, int>
        _fertilizerQuantities = new(); // Stores selected fertilizer quantities

    [SerializeField] private TextMeshProUGUI pointsText; // Displays current player points

    [Header("Tabs")] [SerializeField] private CanvasGroup toolsTab; // Tools section
    [SerializeField] private CanvasGroup seedsTab; // Seeds section
    [SerializeField] private CanvasGroup fertilizersTab; // Fertilizers section

    [Header("Tools Tab")] [SerializeField]
    private List<ToolButtonGroup> toolButtonGroups; // All tool buttons available in the store

    [Header("Seeds Tab")] [SerializeField]
    private List<SeedQuantityGroup> seedQuantityGroups; // Seed groups with price and quantity input

    [SerializeField] private TextMeshProUGUI seedsTotalCostText; // Displays total cost for selected seeds

    [Header("Fertilizers Tab")] [SerializeField]
    private List<FertilizerQuantityGroup> fertilizerQuantityGroups; // Fertilizer groups

    [SerializeField] private TextMeshProUGUI fertilizersTotalCostText; // Displays total cost for selected fertilizers

    private CanvasGroup _activeTabCanvasGroup; // Tracks which tab is currently active
    private int _seedsTotalCost; // Total cost for seeds
    private int _fertilizersTotalCost; // Total cost for fertilizers

    // Initialize default tab
    protected override void InitializeScreen() {
        toolsTab.alpha = 1f;
        seedsTab.alpha = 0f;
        fertilizersTab.alpha = 0f;
        _activeTabCanvasGroup = toolsTab;
        _activeTabCanvasGroup.transform.SetAsLastSibling();
    }

    // Subscribe to points changes and initialize tabs
    private void Start() {
        InventoryManager.Instance.OnPointsChanged += UpdatePointsUI;
        InitializeSeedsTab();
        InitializeFertilizersTab();
    }

    // When screen opens, refresh the points display
    public override void OpenScreen() {
        UpdatePointsUI(null, InventoryManager.Instance.Points);
        base.OpenScreen();
    }

    // Sets up tool buttons with listeners and disables buttons for owned tools
    public void InitializeToolsTab() {
        foreach (ToolButtonGroup toolButtonGroup in toolButtonGroups)
            if (InventoryManager.Instance.PlayerAvailableTools.Contains(toolButtonGroup.playerItem))
                toolButtonGroup.buyButton.interactable = false;
            else
                toolButtonGroup.buyButton.onClick.AddListener(() => PurchaseTool(toolButtonGroup));
    }

    // Purchases a tool if the player has enough points and doesn't already own it
    private static void PurchaseTool(ToolButtonGroup toolButtonGroup) {
        if (InventoryManager.Instance.PlayerAvailableTools.Contains(toolButtonGroup.playerItem)) {
            if (toolButtonGroup.buyButton.interactable) toolButtonGroup.buyButton.interactable = false;
            return;
        }

        if (!InventoryManager.Instance.SpendPoints(toolButtonGroup.price)) return;

        InventoryManager.Instance.AddTool(toolButtonGroup.playerItem);
        toolButtonGroup.buyButton.interactable = false;
        QuestManager.Instance.TrackBuyItem();
    }

    // Adds listeners to seed quantity pickers
    private void InitializeSeedsTab() {
        foreach (SeedQuantityGroup seedQuantityGroup in seedQuantityGroups)
            seedQuantityGroup.quantityPicker.OnQuantityChanged += RecalculateTotalSeedsAmount;
    }

    // Calculates total cost of selected seeds
    private void RecalculateTotalSeedsAmount(object sender, EventArgs e) {
        _seedsTotalCost = seedQuantityGroups.Sum(seedQuantityGroup =>
            seedQuantityGroup.price * seedQuantityGroup.quantityPicker.Quantity);
        seedsTotalCostText.text = $"Total cost: {_seedsTotalCost} points";
    }

    // Handles purchasing all selected seeds
    public void BuyAllSeeds() {
        foreach (SeedQuantityGroup seedQuantityGroup in seedQuantityGroups) {
            _seedQuantities.TryAdd(seedQuantityGroup.plantSo, 0);
            _seedQuantities[seedQuantityGroup.plantSo] = seedQuantityGroup.quantityPicker.Quantity;
        }

        if (!InventoryManager.Instance.SpendPoints(_seedsTotalCost)) return;

        InventoryManager.Instance.AddPlants(_seedQuantities);
        int totalQuantity = _seedQuantities.Values.Sum();
        QuestManager.Instance.TrackBuyItem(totalQuantity);
        seedsTotalCostText.text = $"Total cost: 0 points";

        foreach (SeedQuantityGroup seedQuantityGroup in seedQuantityGroups)
            seedQuantityGroup.quantityPicker.ResetQuantity();
        _seedQuantities.Clear();
    }

    // Adds listeners to fertilizer quantity pickers
    private void InitializeFertilizersTab() {
        foreach (FertilizerQuantityGroup fertilizerQuantityGroup in fertilizerQuantityGroups)
            fertilizerQuantityGroup.quantityPicker.OnQuantityChanged += RecalculateTotalFertilizersAmount;
    }

    // Calculates total cost of selected fertilizers
    private void RecalculateTotalFertilizersAmount(object sender, EventArgs e) {
        _fertilizersTotalCost = fertilizerQuantityGroups.Sum(fertilizerQuantityGroup =>
            fertilizerQuantityGroup.price * fertilizerQuantityGroup.quantityPicker.Quantity);
        fertilizersTotalCostText.text = $"Total cost: {_fertilizersTotalCost} points";
    }

    // Handles purchasing all selected fertilizers
    public void BuyAllFertilizers() {
        foreach (FertilizerQuantityGroup fertilizerQuantityGroup in fertilizerQuantityGroups) {
            _fertilizerQuantities.TryAdd(fertilizerQuantityGroup.fertilizerSo, 0);
            _fertilizerQuantities[fertilizerQuantityGroup.fertilizerSo] =
                fertilizerQuantityGroup.quantityPicker.Quantity;
        }

        if (!InventoryManager.Instance.SpendPoints(_fertilizersTotalCost)) return;

        InventoryManager.Instance.AddFertilizers(_fertilizerQuantities);
        int totalQuantity = _fertilizerQuantities.Values.Sum();
        QuestManager.Instance.TrackBuyItem(totalQuantity);
        fertilizersTotalCostText.text = $"Total cost: 0 points";

        foreach (FertilizerQuantityGroup fertilizerQuantityGroup in fertilizerQuantityGroups)
            fertilizerQuantityGroup.quantityPicker.ResetQuantity();
        _fertilizerQuantities.Clear();
    }

    // Changes the visible tab in the store based on name
    public void SetStoreTab(string tabName) {
        CanvasGroup newTab = tabName switch {
            "Tools" => toolsTab,
            "Seeds" => seedsTab,
            "Fertilizers" => fertilizersTab,
            _ => toolsTab
        };
        StartCoroutine(SwitchTab(newTab));
    }

    // Coroutine to smoothly fade between tabs
    private IEnumerator SwitchTab(CanvasGroup tab) {
        float t = 0f;
        while (t < 0.3f) {
            t += Time.deltaTime / 0.3f;
            _activeTabCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        _activeTabCanvasGroup.alpha = 0f;
        _activeTabCanvasGroup = tab;
        _activeTabCanvasGroup.transform.SetAsLastSibling();

        t = 0f;
        while (t < 0.3f) {
            t += Time.deltaTime / 0.3f;
            _activeTabCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        _activeTabCanvasGroup.alpha = 1f;
    }

    // Updates the UI display of player's current points
    public void UpdatePointsUI(object sender, int points) {
        if (pointsText) pointsText.text = "Points: " + points;
    }

    // Unsubscribe from all listeners to prevent memory leaks
    private void OnDestroy() {
        foreach (ToolButtonGroup toolButtonGroup in toolButtonGroups)
            toolButtonGroup.buyButton.onClick.RemoveAllListeners();
        foreach (SeedQuantityGroup seedQuantityGroup in seedQuantityGroups)
            seedQuantityGroup.quantityPicker.OnQuantityChanged -= RecalculateTotalSeedsAmount;
        foreach (FertilizerQuantityGroup fertilizerQuantityGroup in fertilizerQuantityGroups)
            fertilizerQuantityGroup.quantityPicker.OnQuantityChanged -= RecalculateTotalFertilizersAmount;
    }

    // Represents a tool UI button group
    [Serializable]
    private class ToolButtonGroup {
        public PlayerItem playerItem;
        public int price;
        public Button buyButton;
    }

    // Represents a seed UI group with quantity selection
    [Serializable]
    private class SeedQuantityGroup {
        public PlantSO plantSo;
        public int price;
        public QuantityPicker quantityPicker;
    }

    // Represents a fertilizer UI group with quantity selection
    [Serializable]
    private class FertilizerQuantityGroup {
        public FertilizerSO fertilizerSo;
        public int price;
        public QuantityPicker quantityPicker;
    }
}