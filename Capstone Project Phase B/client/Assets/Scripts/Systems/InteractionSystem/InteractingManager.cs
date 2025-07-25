using LoM.Super;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The InteractingManager class is responsible for managing interactions within the game.
/// It is a subclass of SuperBehaviour and implements the IUpdateObserver interface.
/// </summary>
public class InteractingManager : SuperBehaviour, IUpdateObserver {
    /// <summary>
    /// The squared distance value used to determine whether an interaction
    /// between the player and an object is allowed.
    /// This constant is used for efficiency to avoid calculating square roots
    /// when comparing distances, as the squared distance is sufficient
    /// for comparison purposes.
    /// </summary>
    private const float InteractionDistanceSqr = 4f;

    /// <summary>
    /// A UI element of type TextMeshProUGUI that displays the name of the currently interactable object.
    /// </summary>
    [SerializeField] private TextMeshProUGUI interactableObjectNameText;

    /// <summary>
    /// Represents the primary camera used for rendering and interaction in the scene.
    /// </summary>
    private Camera _mainCamera;

    /// <summary>
    /// Stores a reference to the currently detected interactable object within interaction range.
    /// </summary>
    private IInteractableObject _interactableObject;

    /// <summary>
    /// Represents the layer mask used to determine which objects can be interacted with during gameplay.
    /// </summary>
    private LayerMask _interactableLayerMask;

    /// <summary>
    /// A private boolean field used to determine if the player is currently able to perform an interaction.
    /// When true, interaction inputs are processed to trigger interactions with the designated interactable object.
    /// The value is updated based on the presence of an interactable object and game state conditions.
    /// </summary>
    private bool _canInteract;

    /// <summary>
    /// Stores the time elapsed, in seconds, since the last update occurred.
    /// Used to track and manage gameplay logic or interactions that require time-based updates.
    /// </summary>
    private float _timeSinceLastUpdate;

    /// <summary>
    /// Indicates whether the current scene is the GameScene.
    /// </summary>
    private bool _isGameScene;

    /// <summary>
    /// Initializes key elements and settings for the InteractingManager instance.
    /// </summary>
    private void Start() {
        _mainCamera = Camera.main;
        interactableObjectNameText.text = string.Empty;
        _isGameScene = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene);

        if (_isGameScene) {
            _interactableLayerMask = ~LayerMask.GetMask("Plant");
            UIManager.Instance.OnActivityStateChanged += UIManagerOnActivityStateChanged;
            UIManager.Instance.OnUIUsageChanged += UIManagerOnUIUsageChanged;
        } else {
            _interactableLayerMask = ~0;
        }
    }

    /// <summary>
    /// Handles changes in the UI usage state. This method is subscribed to the <c>UIManager.OnUIUsageChanged</c> event.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="uiUsageState">
    /// A <c>bool</c> value indicating whether the UI is currently in use.
    /// </param>
    private void UIManagerOnUIUsageChanged(object sender, bool uiUsageState) {
        if (uiUsageState) ClearInteraction();
    }

    /// <summary>
    /// Handles the activity state change event received from the UIManager.
    /// Clears any ongoing interactions if the activity state is enabled.
    /// </summary>
    /// <param name="sender">The source of the event, typically the UIManager instance that triggered the event.</param>
    /// <param name="activityState">A boolean indicating the current activity state. If true, interactions are cleared.</param>
    private void UIManagerOnActivityStateChanged(object sender, bool activityState) {
        if (activityState) ClearInteraction();
    }

    /// <summary>
    /// Handles the event triggered when the interaction input action is performed.
    /// If interaction is allowed, and the game scene conditions and UI usage states are valid,
    /// it invokes the interaction behavior on the associated interactable object.
    /// </summary>
    /// <param name="obj">The context data of the input action, containing information about the event that triggered it.</param>
    private void InteractInputActionOnPerformed(InputAction.CallbackContext obj) {
        if (!_canInteract || _isGameScene && UIManager.Instance.IsUIUsed) return;
        AudioManager.Instance.PlayClickSoundEffect();
        _interactableObject?.Interact();
    }

    /// <summary>
    /// Reacts to update events and handles player interaction logic.
    /// This method is invoked as part of the IUpdateObserver implementation for observing updates within the system.
    /// </summary>
    public void ObservedUpdate() {
        HandlePlayerInteraction();
    }

    /// <summary>
    /// Handles the player's interaction logic with objects in the game environment.
    /// </summary>
    private void HandlePlayerInteraction() {
        if (_isGameScene) {
            if (UIManager.Instance.ActivityState || UIManager.Instance.IsUIUsed) return;
        } else {
            if (DialogueUI.Instance.IsDialogueActive) {
                ClearInteraction();
                return;
            }
        }

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, _interactableLayerMask)) {
            ClearInteraction();
            return;
        }

        Transform hitTransform = hitInfo.transform;
        if (!hitTransform.TryGetComponent(out IInteractableObject interactable)) {
            ClearInteraction();
            return;
        }

        if ((transform.position - hitTransform.position).sqrMagnitude > InteractionDistanceSqr) return;

        if (_interactableObject == interactable &&
            interactableObjectNameText.text == _interactableObject.ObjectName)
            return;

        SetInteraction(interactable);
    }

    /// <summary>
    /// Sets the interaction context to the specified interactable object, enabling interaction features
    /// such as input action listening and interface updates.
    /// </summary>
    /// <param name="interactable">The interactable object to set as the current interaction target.</param>
    private void SetInteraction(IInteractableObject interactable) {
        InputManager.Instance.InteractInputAction.performed += InteractInputActionOnPerformed;
        _interactableObject = interactable;
        interactableObjectNameText.text = _interactableObject.ObjectName;
        _canInteract = true;
        GuidePanelUI.Instance.SetInteractGuide(true);
    }

    /// Clears the current interaction state and updates related UI elements.
    /// This method is responsible for resetting the interaction system when
    /// interaction with an object is no longer valid or required. 
    private void ClearInteraction() {
        if (!_canInteract) return;

        InputManager.Instance.InteractInputAction.performed -= InteractInputActionOnPerformed;
        GuidePanelUI.Instance.SetInteractGuide(false);
        _canInteract = false;
        _interactableObject = null;

        if (interactableObjectNameText.text.Length > 0) interactableObjectNameText.text = string.Empty;
    }

    /// <summary>
    /// Called when the object becomes enabled and active in the scene.
    /// This method registers the current instance of the <see cref="InteractingManager"/>
    /// as an update observer with the <see cref="UpdateManager"/>.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the object is disabled. Unregisters the current instance
    /// of the <see cref="InteractingManager"/> class from the <see cref="UpdateManager"/>,
    /// ensuring that it no longer receives update notifications as part of the
    /// <see cref="IUpdateObserver"/> interface.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);

    /// <summary>
    /// Handles the destruction process for the InteractingManager instance.
    /// Unsubscribes from any event handlers for `UIManager` events related to UI usage and activity state changes
    /// to prevent memory leaks or unintended behavior after the object is destroyed.
    /// This cleanup is only performed if the object is in a game scene context.
    /// </summary>
    private void OnDestroy() {
        if (!_isGameScene) return;
        UIManager.Instance.OnActivityStateChanged -= UIManagerOnActivityStateChanged;
        UIManager.Instance.OnUIUsageChanged -= UIManagerOnUIUsageChanged;
    }
}