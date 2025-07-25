using LoM.Super;

/// <summary>
/// Manages tool interactions for a specific plantable area in the game environment.
/// </summary>
public class PlantableAreaToolsManager : SuperBehaviour {
    /// <summary>
    /// Gets the instance of the <see cref="PlantableArea"/> associated with the parent transform.
    /// This represents the specific area where plants can be planted, managed, or interacted with.
    /// </summary>
    public PlantableArea PlantableArea { get; private set; }

    /// <summary>
    /// Represents a ShadeTent tool used to provide shade for plants within a PlantableArea.
    /// The ShadeTent can influence plant growth and other environmental factors when used appropriately.
    /// It is primarily instantiated as a child component within a <see cref="PlantableAreaToolsManager"/>.
    /// </summary>
    public ShadeTent ShadeTent { get; private set; }

    /// <summary>
    /// Initializes the PlantableAreaToolsManager by retrieving and assigning relevant tools and the PlantableArea component.
    /// This method is automatically called when the script is loaded or the object is instantiated.
    /// It ensures the required components are properly set up for tool interaction within the PlantableArea environment.
    /// </summary>
    private void Awake() {
        PlantableArea = transform.parent.GetComponentInChildren<PlantableArea>();
        ShadeTent = GetComponentInChildren<ShadeTent>(true);
    }

    /// <summary>
    /// Executes the functionality for using a specific tool on the plantable area based on the given player item.
    /// </summary>
    /// <param name="playerItem">The tool or item held by the player that needs to be used on the plantable area.</param>
    public void UseTool(PlayerItem playerItem) {
        NotificationPanelUI.Instance.ShowNotification($"Using {playerItem.ToString().SeparateCamelCase()} tool");
        QuestManager.Instance.TrackUseTool();

        if (playerItem == PlayerItem.ShadeTent) {
            ShadeTent.UseShadeTent(PlantableArea);
        } else {
            PlayerToolManager.Instance.UseTool(playerItem, PlantableArea);
        }
    }
}