using System;
using System.Collections.Generic;
using DG.Tweening;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Controls the UI for managing and selecting available plants in the player's inventory
public class PlantsMenuUI : UIScreen<PlantsMenuUI> {
    public event EventHandler OnPlantsMenuOpened;
    [SerializeField] private CanvasGroup storeButton; // UI button for accessing the store
    [SerializeField] private List<PlantItem> plantItems; // List of all plant items in the UI

    // Called when the screen is initialized
    protected override void InitializeScreen() {
        SetListeners(); // Register input and UI listeners
    }

    // Called on Start; updates UI and subscribes to inventory change events
    private void Start() {
        InventoryManagerOnPlantsInventoryChanged(null, EventArgs.Empty); // Initialize plant list display
        InventoryManager.Instance.OnPlantsInventoryChanged +=
            InventoryManagerOnPlantsInventoryChanged; // Subscribe to updates
    }

    public override void OpenScreen() {
        base.OpenScreen();
        OnPlantsMenuOpened?.Invoke(this, EventArgs.Empty);
    }

    // Sets input and button listeners
    private void SetListeners() {
        InputManager.Instance.AccessMenuInputAction.performed += ToggleMenu; // Listen for menu toggle input

        foreach (PlantItem plantItem in plantItems) {
            // When clicking on a plant's main button, select the plant
            plantItem.plantButton.onClick.AddListener(() => SelectPlant(plantItem));
            // When clicking on the info button, show details
            plantItem.infoButton.onClick.AddListener(() => ShowPlantInfo(plantItem));
        }
    }

    // Called when the player's inventory of plants changes
    private void InventoryManagerOnPlantsInventoryChanged(object sender, EventArgs e) {
        foreach (PlantItem plantItem in plantItems) {
            // If player has 0 of the plant, disable the button
            if (!InventoryManager.Instance.PlayerPlantsInventory.TryGetValue(plantItem.plantSo, out int amount) ||
                amount <= 0) {
                plantItem.plantAmountText.text = "0";
                plantItem.plantButton.interactable = false;
            } else {
                // Otherwise, show the amount and enable selection
                plantItem.plantAmountText.text = amount.ToString();
                plantItem.plantButton.interactable = true;
            }
        }
    }

    public void OnUploadButtonClicked() {
        CloseScreen();
        FileBrowserManager.Instance.OpenFileBrowser(FileBrowserManager.IdentificationType.IdentifyPlant);
    }

    // Called when a plant is selected for planting
    private void SelectPlant(PlantItem item) {
        InventoryManager.Instance.UsePlant(item.plantSo); // Reduce the plant from inventory
        InputManager.Instance.CancelInputAction.performed += ReturnPlantToInventory; // Allow returning the plant
        NotificationPanelUI.Instance.ShowNotification("Press X to return the seed", 1f); // Notify user
        CloseScreen(); // Close the menu
        Player.Instance.HoldingPlant = item.plantSo; // Set the plant as being held by the player
    }

    // Called when the player wants to return the plant they are holding
    private static void ReturnPlantToInventory(InputAction.CallbackContext obj) {
        if (!Player.Instance.HoldingPlant) {
            InputManager.Instance.CancelInputAction.performed -= ReturnPlantToInventory;
            return;
        }

        InventoryManager.Instance.ReturnPlant(Player.Instance.HoldingPlant); // Return the plant to inventory
        Player.Instance.HoldingPlant = null; // Clear the held plant
        InputManager.Instance.CancelInputAction.performed -= ReturnPlantToInventory; // Unsubscribe input
    }

    // Opens the plant info screen for a specific plant
    private static void ShowPlantInfo(PlantItem item) {
        PlantInfoScreenUI.Instance.OpenScreen(); // Show the info screen
        PlantInfoScreenUI.Instance.SetScreenInfo(item.plantSo); // Set the data to display
    }

    // Toggles the visibility of the plants menu
    private void ToggleMenu(InputAction.CallbackContext obj) {
        // Prevent opening if other screens or file browser is active
        if (FileBrowser.IsOpen || PlantInfoScreenUI.Instance.IsScreenOpen || UploadingPlantUI.Instance.IsScreenOpen)
            return;

        if (IsScreenOpen)
            CloseScreen();
        else
            OpenScreen();
    }

    // Highlights the store button by fading it in and out
    public void IndicateStoreButton() {
        if (!storeButton) return;
        storeButton.DOKill(); // Stop existing animations
        Sequence sequence = DOTween.Sequence(); // Create animation sequence
        sequence.Append(storeButton.DOFade(0.5f, 0.5f)).Append(storeButton.DOFade(1f, 0.5f))
            .SetLoops(10); // Blink animation
    }

    // Cleanup event listeners when the object is destroyed
    private void OnDestroy() {
        InputManager.Instance.AccessMenuInputAction.performed -= ToggleMenu; // Remove menu toggle input
        foreach (PlantItem plantItem in plantItems) {
            plantItem.plantButton.onClick.RemoveAllListeners(); // Clear listeners to avoid memory leaks
            plantItem.infoButton.onClick.RemoveAllListeners();
        }
    }

    // Represents each plant's UI components in the list
    [Serializable]
    private class PlantItem {
        public Button plantButton; // Button to select this plant
        public Button infoButton; // Button to show plant info
        public PlantSO plantSo; // The actual plant data
        public TextMeshProUGUI plantAmountText; // Text showing how many of this plant the player owns
    }
}