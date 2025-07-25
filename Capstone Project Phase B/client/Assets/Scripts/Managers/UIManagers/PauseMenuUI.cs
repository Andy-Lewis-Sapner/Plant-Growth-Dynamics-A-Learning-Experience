using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// Manages the pause menu UI in the game, including pausing, resuming, and exiting the game.
// Listens to the pause input and handles scene transitions when exiting.
public class PauseMenuUI : UIScreen<PauseMenuUI> {
    [SerializeField] private GameObject mainCanvas; // Reference to the main game UI canvas
    [SerializeField] private Button exitButton; // Button used to exit the game or return to main menu
    [SerializeField] private Button returnToLoginScreen;
    [SerializeField] private Button exitGameButton;
    private bool _isGameScene; // Determines if the current scene is the main game scene

    protected override void InitializeScreen() {
        // Initializes the screen and determines if we are in the GameScene.
        SetListeners(); // Attach input listeners
        _isGameScene = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene); // Check current scene
    }

    // Method to allow or disallow exiting the game
    public void AllowExit(bool allow) {
        if (allow)
            InputManager.Instance.PauseInputAction.performed += PauseInputActionOnPerformed;
        else
            InputManager.Instance.PauseInputAction.performed -= PauseInputActionOnPerformed;
        exitButton.interactable = allow;
        returnToLoginScreen.interactable = allow;
        exitGameButton.interactable = allow;
    }

    // Registers the input listener for pause functionality
    private void SetListeners() {
        InputManager.Instance.PauseInputAction.performed += PauseInputActionOnPerformed;
    }

    // Called when the pause input is performed
    private void PauseInputActionOnPerformed(InputAction.CallbackContext obj) {
        if (IsScreenOpen)
            CloseScreen(); // If menu is open, close it
        else
            OpenScreen(); // If menu is closed, open it
    }

    // Handles the exit game functionality
    public void HandleExitGame(bool headToMenuScene) {
        if (Player.Instance.HoldingPlant) InventoryManager.Instance.ReturnPlant(Player.Instance.HoldingPlant);
        StartCoroutine(LogoutThenQuit(headToMenuScene));
    }

    // Coroutine to save game data, logout, and then either return to menu or quit
    private IEnumerator LogoutThenQuit(bool headToMenuScene) {
        yield return StartCoroutine(DataManager.Instance.SaveGameCoroutine()); // Save game state
        yield return StartCoroutine(DataManager.Instance.Logout()); // Log the user out
        if (headToMenuScene)
            yield return SceneManager.LoadSceneAsync("mains_creen"); // Load the main menu scene
        else
            Application.Quit(); // Quit the game application
    }

    // Opens the pause menu screen
    public override void OpenScreen() {
        if (_isGameScene && UIManager.Instance.ActivityState)
            return; // Prevent opening if the game is active (e.g. cutscene, animation)
        mainCanvas.SetActive(false); // Hide the main canvas when pause menu is active
        base.OpenScreen(); // Show the pause menu
    }

    // Restores the main canvas when pause menu is closed
    protected override void DeactivateScreen() {
        mainCanvas.SetActive(true); // Reactivate main gameplay UI
        base.DeactivateScreen(); // Close the pause screen
    }

    // Cleanup: remove the input listener when the object is destroyed
    private void OnDestroy() {
        InputManager.Instance.PauseInputAction.performed -= PauseInputActionOnPerformed;
    }
}