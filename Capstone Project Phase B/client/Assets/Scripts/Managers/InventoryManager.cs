using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages inventory-related operations, including tools, plants, fertilizers, and points for the player.
/// This class follows the Singleton pattern and provides data structures and events
/// that allow other components to interact with the player's inventory state.
/// </summary>
public class InventoryManager : Singleton<InventoryManager> {
    /// <summary>
    /// Event triggered whenever changes are made to the player's plants inventory.
    /// </summary>
    public event EventHandler OnPlantsInventoryChanged;

    /// <summary>
    /// Event triggered whenever the player's points are updated.
    /// The event passes the current points value as an integer parameter.
    /// </summary>
    public event EventHandler<int> OnPointsChanged;

    /// <summary>
    /// Event triggered whenever the player changes the item they are currently holding.
    /// </summary>
    public event EventHandler<PlayerItem> OnPlayerHoldingItemChanged;

    /// <summary>
    /// Represents the constant amount of fertilizer received per purchase transaction
    /// in the game's inventory system. This value is used to calculate the total
    /// quantity of fertilizers added to the player's inventory whenever a fertilizer purchase is made.
    /// </summary>
    private const int FertilizerAmountInPurchase = 20;

    /// <summary>
    /// Represents the fixed amount of fertilizer used when a fertilizer action is performed.
    /// </summary>
    private const int FertilizerUsageAmount = 10;

    /// <summary>
    /// A collection of tools currently available to the player.
    /// </summary>
    public List<PlayerItem> PlayerAvailableTools { get; } = new() { PlayerItem.None };

    /// <summary>
    /// Represents the player's inventory of plants, where each plant type is associated with a quantity.
    /// </summary>
    public Dictionary<PlantSO, int> PlayerPlantsInventory { get; } = new();

    /// <summary>
    /// Represents the inventory of fertilizers available to the player in the game.
    /// </summary>
    public Dictionary<FertilizerSO, int> PlayerFertilizersInventory { get; } = new();

    /// <summary>
    /// Represents the currently selected or held player item in the player's inventory.
    /// </summary>
    public PlayerItem HoldingPlayerItem { get; private set; }

    /// <summary>
    /// Represents the player's accumulated points in the game.
    /// </summary>
    public int Points {
        get => _points;
        set {
            _points = Mathf.Max(0, value);
            OnPointsChanged?.Invoke(this, _points);
        }
    }

    /// <summary>
    /// Stores the current points of the player in the inventory system.
    /// This value is always kept non-negative, clamped to a minimum of zero.
    /// Updates to this variable trigger the <see cref="InventoryManager.OnPointsChanged"/> event.
    /// </summary>
    private int _points;

    /// <summary>
    /// Represents the current index of the item being tracked or selected within the player's available tools.
    /// This variable is used to facilitate cycling through items in the player's inventory.
    /// </summary>
    private int _itemIndex;

    /// Initializes the player's inventory and sets up input actions for toggling through available tools.
    /// This method is executed when the `InventoryManager` is initialized.
    /// Specifically, it sets the default holding item to `PlayerItem.None`, ensuring the player doesn't hold
    /// an item by default. It also subscribes to the `PreviousInputAction` and `NextInputAction` input events
    /// using the `InputManager` to allow cycling through the player's available tools.
    private void Start() {
        HoldingPlayerItem = PlayerItem.None;
        InputManager.Instance.PreviousInputAction.performed += _ => CycleThroughItems(-1);
        InputManager.Instance.NextInputAction.performed += _ => CycleThroughItems(1);
    }

    /// <summary>
    /// Cycles through the list of available tools for the player based on the given change value.
    /// This method updates the player's currently held item and fires the OnPlayerHoldingItemChanged event.
    /// </summary>
    /// <param name="change">The value indicating the direction and amount of change for cycling through the player's available tools (e.g., -1 for previous, 1 for next).</param>
    private void CycleThroughItems(int change) {
        if (UIManager.Instance?.ActivityState == true || UIManager.Instance?.IsUIUsed == true) return;
        _itemIndex = (_itemIndex + change).CycleInRange(0, PlayerAvailableTools.Count);
        PlayerItem playerItem = PlayerAvailableTools[_itemIndex];
        HoldingPlayerItem = playerItem;
        NotificationPanelUI.Instance?.ShowNotification(playerItem == PlayerItem.None
            ? "No item is being held"
            : $"Holding {playerItem.ToString().SeparateCamelCase()}");
        OnPlayerHoldingItemChanged?.Invoke(this, playerItem);
    }

    /// <summary>
    /// Determines whether the specified item exists in the collection.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>Returns true if the item exists in the collection; otherwise, false.</returns>
    public bool HasItem(PlayerItem item) => PlayerAvailableTools.Contains(item);

    /// Determines whether the inventory contains any of the specified items.
    /// <param name="items">A list of <see cref="PlayerItem"/> objects to check for in the inventory.</param>
    /// <returns>True if at least one of the specified items is in the inventory; otherwise, false.</returns>
    public bool HasItem(List<PlayerItem> items) => items.Any(HasItem);

    /// Adds a specified tool to the player's available tools if it is not already present.
    /// <param name="item">The tool to be added to the player's inventory.</param>
    public void AddTool(PlayerItem item) {
        if (!PlayerAvailableTools.Contains(item)) PlayerAvailableTools.Add(item);
    }

    /// Adds a batch of plants to the player's inventory.
    /// Updates the player's plant inventory with the given collection of plants and quantities.
    /// Raises the OnPlantsInventoryChanged event after processing the addition of plants.
    /// <param name="plantsToPurchase">A dictionary where the key represents the PlantSO to add,
    /// and the value is the quantity of that plant to be added.</param>
    public void AddPlants(Dictionary<PlantSO, int> plantsToPurchase) {
        foreach (KeyValuePair<PlantSO, int> plantToPurchase in plantsToPurchase) {
            if (!PlayerPlantsInventory.ContainsKey(plantToPurchase.Key))
                PlayerPlantsInventory.Add(plantToPurchase.Key, plantToPurchase.Value);
            else
                PlayerPlantsInventory[plantToPurchase.Key] += plantToPurchase.Value;
        }

        OnPlantsInventoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Decreases the quantity of a specified plant in the player's inventory
    /// and triggers the OnPlantsInventoryChanged event if applicable.
    /// </summary>
    /// <param name="plantSo">The PlantSO object representing the plant to use from the inventory.</param>
    public void UsePlant(PlantSO plantSo) {
        if (!PlayerPlantsInventory.ContainsKey(plantSo)) return;
        PlayerPlantsInventory[plantSo]--;
        OnPlantsInventoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns a specified plant back to the player's inventory, increasing the inventory count
    /// for the specified plant. Triggers the OnPlantsInventoryChanged event if the operation
    /// affects the inventory.
    /// </summary>
    /// <param name="plantSo">The PlantSO object representing the plant to be returned to the player's inventory.</param>
    public void ReturnPlant(PlantSO plantSo) {
        if (!PlayerPlantsInventory.ContainsKey(plantSo)) return;
        PlayerPlantsInventory[plantSo]++;
        OnPlantsInventoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// Determines whether the player has any seeds available in the inventory.
    /// <returns>
    /// true if there is at least one seed with a quantity greater than zero in the player's inventory; otherwise, false.
    /// </returns>
    public bool HasAnySeeds() {
        return PlayerPlantsInventory.Values.Any(amount => amount > 0);
    }

    /// Adds specified fertilizers to the player's inventory. The quantity for each fertilizer
    /// is multiplied by a predefined constant amount per purchase before being added to the inventory.
    /// <param name="fertilizersToPurchase">A dictionary containing the fertilizers to add,
    /// where the key is the FertilizerSO and the value is the quantity to purchase.</param>
    public void AddFertilizers(Dictionary<FertilizerSO, int> fertilizersToPurchase) {
        foreach (KeyValuePair<FertilizerSO, int> fertilizerToPurchase in fertilizersToPurchase) {
            if (!PlayerFertilizersInventory.ContainsKey(fertilizerToPurchase.Key))
                PlayerFertilizersInventory.Add(fertilizerToPurchase.Key,
                    fertilizerToPurchase.Value * FertilizerAmountInPurchase);
            else
                PlayerFertilizersInventory[fertilizerToPurchase.Key] +=
                    fertilizerToPurchase.Value * FertilizerAmountInPurchase;
        }
    }

    /// <summary>
    /// Reduces the amount of a specific fertilizer in the player's inventory
    /// by a predefined usage amount if the fertilizer is available.
    /// </summary>
    /// <param name="fertilizerSo">The fertilizer to be used, identified by its scriptable object.</param>
    public void UseFertilizer(FertilizerSO fertilizerSo) {
        if (!PlayerFertilizersInventory.ContainsKey(fertilizerSo)) return;
        PlayerFertilizersInventory[fertilizerSo] -= FertilizerUsageAmount;
    }

    /// Spends a specified amount of points if available. If the player has sufficient points,
    /// the specified amount is deducted from their total points. If not enough points are available,
    /// a notification is displayed.
    /// <param name="amount">The amount of points to be spent.</param>
    /// <returns>
    /// True if the points were successfully spent; otherwise, false.
    /// </returns>
    public bool SpendPoints(int amount) {
        if (Points >= amount) {
            Points -= amount;
            return true;
        }

        NotificationPanelUI.Instance.ShowNotification("Not enough points");
        return false;
    }

    /// Loads the player's inventory and points data from the server and initializes the inventory state.
    /// This method retrieves game progress data from the server, including the player's available tools, plants inventory,
    /// fertilizer inventory, and points. It updates the relevant inventory dictionaries and player points within the
    /// InventoryManager, ensuring they reflect the data retrieved from the server. Additionally, it initializes the UI
    /// elements related to the inventory in the store menu.
    public void LoadFromServer() {
        GameProgressData data = DataManager.Instance.GameProgress;
        Points = data.points;
        StoreMenuUI.Instance.UpdatePointsUI(null, Points);

        PlayerAvailableTools.Clear();
        if (data.playerAvailableTools != null)
            foreach (string toolName in data.playerAvailableTools)
                if (Enum.TryParse(toolName, out PlayerItem item))
                    PlayerAvailableTools.Add(item);

        if (!PlayerAvailableTools.Contains(PlayerItem.None)) PlayerAvailableTools.Add(PlayerItem.None);
        StoreMenuUI.Instance.InitializeToolsTab();

        PlayerPlantsInventory.Clear();
        if (data.playerPlantsInventory != null)
            foreach (KeyValuePair<string, int> pair in data.playerPlantsInventory) {
                PlantSO plant = DataManager.Instance.PlantsListSo.FindPlantSoByName(pair.Key);
                if (plant && pair.Value > 0) PlayerPlantsInventory[plant] = pair.Value;
            }

        PlayerFertilizersInventory.Clear();
        if (data.playerFertilizersInventory != null)
            foreach (KeyValuePair<string, int> pair in data.playerFertilizersInventory) {
                FertilizerSO fertilizer = DataManager.Instance.FertilizerListSo.GetFertilizerByName(pair.Key);
                if (fertilizer && pair.Value > 0) PlayerFertilizersInventory[fertilizer] = pair.Value;
            }
    }

    /// <summary>
    /// Updates the <see cref="GameProgressData"/> object with the current state of the inventory and points.
    /// </summary>
    public void UpdateGameProgressData() {
        GameProgressData data = DataManager.Instance.GameProgress;
        data.points = Points;

        data.playerAvailableTools = PlayerAvailableTools
            .Where(item => item != PlayerItem.None)
            .Select(item => item.ToString())
            .ToList();

        data.playerPlantsInventory = PlayerPlantsInventory
            .ToDictionary(pair => pair.Key.plantName, pair => pair.Value);

        data.playerFertilizersInventory = PlayerFertilizersInventory
            .ToDictionary(pair => pair.Key.fertilizerName, pair => pair.Value);
    }
}