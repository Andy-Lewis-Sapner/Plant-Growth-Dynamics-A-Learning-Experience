using LoM.Super;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying and interacting with a quest entry.
/// </summary>
public class QuestEntry : SuperBehaviour {
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button claimButton;

    /// <summary>
    /// The mission data associated with this quest entry.
    /// </summary>
    public MissionData Quest { get; private set; }

    /// <summary>
    /// Initializes the quest entry with mission data and sets up button listeners.
    /// </summary>
    /// <param name="quest">The mission data to display.</param>
    public void Initialize(MissionData quest) {
        Quest = quest;
        UpdateUI(quest);
        claimButton.onClick.AddListener(() => QuestManager.Instance.ClaimMissionReward(quest.missionId));
        guideButton.onClick.AddListener(() => MissionGuideManager.Instance.StartGuide(quest.missionId));
    }

    /// <summary>
    /// Updates the UI elements based on the given mission data.
    /// </summary>
    /// <param name="quest">The mission data to reflect in the UI.</param>
    public void UpdateUI(MissionData quest) {
        descriptionText.text = quest.description;
        progressSlider.value = (float)quest.currentProgress / quest.targetProgress;
        progressText.text = $"{quest.currentProgress}/{quest.targetProgress}";
        rewardText.text = quest.pointsReward > 0 ? $"{quest.pointsReward}P" : string.Empty;
        claimButton.interactable = quest.completed && quest.pointsReward > 0;
    }

    /// <summary>
    /// Removes all listeners when the component is destroyed.
    /// </summary>
    private void OnDestroy() {
        claimButton.onClick.RemoveAllListeners();
        guideButton.onClick.RemoveAllListeners();
    }
}
