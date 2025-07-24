using System;
using UnityEngine;

/// <summary>
/// Manages the state of UI interactions and activity within the application.
/// Tracks whether the UI is currently being used or active, and notifies observers of state changes.
/// Implements <see cref="IUpdateObserver"/>.
/// </summary>
public class UIManager : Singleton<UIManager>, IUpdateObserver {
    /// <summary>
    /// Event triggered when the state of UI usage changes.
    /// </summary>
    /// <event>
    /// The event handler receives two parameters:
    /// - The sender object, which is the <see cref="UIManager"/> instance triggering the event.
    /// - A boolean value indicating the new UI usage state (true if the UI is being used, false otherwise).
    /// </event>
    public event EventHandler<bool> OnUIUsageChanged;

    /// Event triggered when the activity state changes.
    /// This event is raised whenever the `ActivityState` property of the `UIManager` class is updated.
    /// The `ActivityState` represents whether the system is in an active state or not.
    /// Listeners can subscribe to this event to perform actions based on the active or inactive
    /// state of the application.
    /// The first parameter passed to the event handler is the sender (instance of `UIManager`),
    /// and the second parameter is a boolean indicating the current activity state.
    public event EventHandler<bool> OnActivityStateChanged;

    /// <summary>
    /// A serialized private GameObject reference used to represent
    /// the centered dot UI element. This element is typically used
    /// as a part of the user interface to display or provide visual
    /// feedback in the central area of the screen.
    /// </summary>
    [SerializeField] private GameObject centeredDot;

    /// <summary>
    /// A serialized UI element that displays the name of an interactable object in the scene.
    /// This element is toggled alongside the centered dot when user indicators are enabled or disabled.
    /// </summary>
    [SerializeField] private GameObject interactableObjectNameText;

    /// <summary>
    /// A serialized private GameObject reference used to represent
    /// the player buttons UI element. This element includes buttons
    /// for various UIs, such as the pause menu, plants menu and so on.
    /// </summary>
    [SerializeField] private GameObject playerButtons;

    /// <summary>
    /// Indicates whether any UI screen is currently open.
    /// This includes various screens such as the file browser,
    /// plants menu, uploading screen, plant info screen, statistics screen,
    /// location selection screen, pause menu, manage screen, store menu, and quests.
    /// </summary>
    private static bool IsAnyScreenOpen => SimpleFileBrowser.FileBrowser.IsOpen ||
                                           PlantsMenuUI.Instance.IsScreenOpen ||
                                           UploadingPlantUI.Instance.IsScreenOpen ||
                                           UploadingDiseaseUI.Instance.IsScreenOpen ||
                                           PlantInfoScreenUI.Instance.IsScreenOpen ||
                                           StatisticsScreenUI.Instance.IsScreenOpen ||
                                           SelectLocationUI.Instance.IsScreenOpen ||
                                           PauseMenuUI.Instance.IsScreenOpen ||
                                           ManageScreenUI.Instance.IsScreenOpen || 
                                           StoreMenuUI.Instance.IsScreenOpen ||
                                           QuestUI.Instance.IsScreenOpen ||
                                           InformationUI.Instance.IsScreenOpen;

    /// <summary>
    /// Represents a private boolean field that tracks whether the user interface (UI) is currently in use.
    /// </summary>
    private bool _isUIUsed;

    /// Indicates whether any UI element is currently in use, such as a specific menu or screen being open.
    /// If true, user interactions and input related to UI elements are actively engaged, which may impact
    /// game mechanics such as disabling player movement or interactions with objects.
    /// This property is updated internally based on the state of various UI screens or escape key usage.
    /// It triggers the `OnUIUsageChanged` event whenever its value changes.
    public bool IsUIUsed {
        get => _isUIUsed;
        private set {
            if (_isUIUsed == value) return;
            _isUIUsed = value;
            SetUserIndicatorsUsage(!_isUIUsed);
            OnUIUsageChanged?.Invoke(this, _isUIUsed);
        }
    }

    /// <summary>
    /// Represents the current state of activity for the application or user interface.
    /// This variable is used to track whether the system is currently in an active state
    /// where interactions or updates should occur. It is updated when ActivityState is set
    /// and triggers UI changes or event notifications.
    /// </summary>
    private bool _activityState;

    /// <summary>
    /// Represents the active state of the application or session.
    /// When set to true, the system operates in an interactive mode, potentially disabling certain UI elements or toggling specific user interactions.
    /// Setting this property triggers actions such as updating user indicators, invoking associated state change events, and influencing dependent systems or UI components.
    /// </summary>
    public bool ActivityState {
        get => _activityState;
        set {
            if (_activityState == value) return;
            _activityState = value;
            SetUserIndicatorsUsage(!_activityState);
            playerButtons.SetActive(!_activityState);
            OnActivityStateChanged?.Invoke(this, _activityState);
        }
    }

    /// <summary>
    /// Monitors and updates the state of the UI within the game environment.
    /// This method is called to determine whether any UI elements or menus
    /// are currently open and adjusts the <see cref="IsUIUsed"/> property accordingly.
    /// It also ensures that this check is bypassed if the <see cref="ActivityState"/> is active.
    /// </summary>
    public void ObservedUpdate() {
        if (_activityState) return;
        IsUIUsed = InputManager.Instance.EscapeToUI || IsAnyScreenOpen;
    }

    /// Sets the usage state of user indicators, enabling or disabling them based on the provided parameter.
    /// <param name="enable">A boolean value where true indicates enabling user indicators and false indicates disabling them.</param>
    private void SetUserIndicatorsUsage(bool enable) {
        centeredDot.SetActive(enable);
        interactableObjectNameText.SetActive(enable);
    }

    /// <summary>
    /// Unity method that is called when the object becomes enabled and active.
    /// This is typically used to initialize or reset variables, set up references,
    /// subscribe to events, or start necessary processes when the object is enabled.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// This method is called when the MonoBehaviour becomes disabled or inactive.
    /// It is commonly used to perform cleanup actions or release resources when the object is no longer active.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}
