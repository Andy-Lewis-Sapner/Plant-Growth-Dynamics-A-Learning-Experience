using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

// Manages the Quest UI screen, including loading quests, displaying them, and showing notifications
public class QuestUI : UIScreen<QuestUI> {
    [SerializeField] private Transform questList; // Parent transform to hold quest entries in the UI
    [SerializeField] private GameObject questEntryPrefab; // Prefab used to display individual quest entries
    [SerializeField] private TextMeshProUGUI notificationText; // Text field for displaying quest-related notifications

    private readonly List<QuestEntry> _questEntries = new(); // Cached list of current quest UI entries
    public string CurrentTab { get; private set; } = "Daily"; // Currently active quest tab ("Daily", "Weekly", etc.)

    // Subscribe to events
    private void Start() {
        QuestManager.Instance.OnMissionProgressUpdated += UpdateQuestEntry;
        QuestManager.Instance.OnMissionCompleted += QuestManagerMissionCompleted;
    }

    private void QuestManagerMissionCompleted(object sender, MissionData mission) {
        ShowNotification($"{mission.description} completed!");
    }

    // Called when this screen is opened
    public override void OpenScreen() {
        ShowQuests("Daily"); // Show daily quests by default
        base.OpenScreen();
    }

    // Displays the quests of a specific type (e.g., Daily, Weekly)
    public void ShowQuests(string type) {
        CurrentTab = type; // Update current tab type

        // Clear existing quest entries from the UI
        foreach (QuestEntry questEntry in _questEntries) Destroy(questEntry.gameObject);
        _questEntries.Clear();

        // Get list of missions of the requested type
        List<MissionData> quests = QuestManager.Instance.Missions.Where(m => m.type == type).ToList();

        // Create UI entries for each mission
        foreach (MissionData quest in quests) {
            GameObject entryObject = Instantiate(questEntryPrefab, questList); // Instantiate prefab under questList
            entryObject.TryGetComponent(out QuestEntry entry); // Get the QuestEntry component
            entry.Initialize(quest); // Initialize it with quest data
            _questEntries.Add(entry); // Add to local list
        }
    }

    // Updates the UI for a specific quest
    private void UpdateQuestEntry(object sender, MissionData quest) {
        QuestEntry entry = _questEntries.Find(e => e.Quest.missionId == quest.missionId);
        if (entry)
            entry.UpdateUI(quest);
        else
            ShowQuests(CurrentTab);
    }

    // Displays a fading notification on the screen
    private void ShowNotification(string message) {
        notificationText.text = message; // Set notification message

        // Animate text to fade in, wait, and fade out repeatedly
        Sequence sequence = DOTween.Sequence();
        sequence.Append(notificationText.DOFade(1f, 0.5f)) // Fade in
            .AppendInterval(1f) // Stay visible
            .Append(notificationText.DOFade(0f, 0.5f)) // Fade out
            .SetLoops(5); // Repeat 5 times
    }

    // Unsubscribe from events
    private void OnDestroy() {
        QuestManager.Instance.OnMissionProgressUpdated -= UpdateQuestEntry;
        QuestManager.Instance.OnMissionCompleted -= QuestManagerMissionCompleted;
    }
}