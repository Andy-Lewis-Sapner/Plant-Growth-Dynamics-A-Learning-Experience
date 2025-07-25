using System.Collections.Generic;
using UnityEngine;

public class PlayerToolManager : Singleton<PlayerToolManager> {
    [SerializeField] private WateringCan wateringCan; // Reference to the watering can
    [SerializeField] private SprayBottle sprayBottle; // Reference to the spray bottle
    [SerializeField] private PruningShears pruningShears; // Reference to the pruning shears
    [SerializeField] private DrainageShovel drainageShovel; // Reference to the drainage shovel
    [SerializeField] private FertilizerJerrycan fertilizerJerrycan; // Reference to the fertilizer jerrycan

    private GameObject _activeTool; // The currently active tool
    private Dictionary<PlayerItem, Vector3> _originalScales = new(); // The original scales of each tool

    /// <summary>
    /// Initializes the player tool manager by setting up the original scales of each tool.
    /// </summary>
    protected override void AfterAwake() {
        _originalScales = new Dictionary<PlayerItem, Vector3> {
            { PlayerItem.WateringCan, wateringCan.transform.localScale },
            { PlayerItem.FungicideSpray, sprayBottle.transform.localScale },
            { PlayerItem.InsecticideSoap, sprayBottle.transform.localScale },
            { PlayerItem.NeemOil, sprayBottle.transform.localScale },
            { PlayerItem.PruningShears, pruningShears.transform.localScale },
            { PlayerItem.DrainageShovel, drainageShovel.transform.localScale },
            { PlayerItem.Fertilizer, fertilizerJerrycan.transform.localScale }
        };
    }

    /// <summary>
    /// Activates a tool for a specific player item and plantable area.
    /// </summary>
    /// <param name="playerItem">The player item to activate.</param>
    /// <param name="area">The plantable area to target.</param>
    public void UseTool(PlayerItem playerItem, PlantableArea area) {
        if (_activeTool) _activeTool.SetActive(false);

        GameObject tool = GetTool(playerItem);
        if (!tool) return;

        _activeTool = tool;
        _activeTool.SetActive(true);

        if (area.TryGetComponent(out Collider groundCollider)) {
            Vector3 targetPosition = groundCollider.bounds.center;
            float modifier = area.Environment switch {
                Environment.Ground => 1f,
                Environment.GreenHouse => 0.5f,
                Environment.House => 0.25f,
                _ => 1f
            };
            targetPosition.y += modifier * 0.5f;
            _activeTool.transform.position = targetPosition;
            _activeTool.transform.rotation = Quaternion.identity;
        }

        float scaleFactor = area.Environment switch {
            Environment.Ground => 1f,
            Environment.GreenHouse => 0.5f,
            Environment.House => 0.25f,
            _ => 1f
        };
        _activeTool.transform.localScale = _originalScales[playerItem] * scaleFactor;

        switch (playerItem) {
            case PlayerItem.WateringCan:
                wateringCan.UseWateringCan(area);
                break;
            case PlayerItem.FungicideSpray:
            case PlayerItem.InsecticideSoap:
            case PlayerItem.NeemOil:
                sprayBottle.UseSpray(playerItem, area);
                break;
            case PlayerItem.PruningShears:
                pruningShears.UseScissors(area);
                break;
            case PlayerItem.DrainageShovel:
                drainageShovel.UseShovel(area);
                break;
            case PlayerItem.Fertilizer:
                fertilizerJerrycan.UseJerrycan(area);
                break;
        }
    }

    /// <summary>
    /// Resets the active tool, disabling it and restoring its original scale.
    /// </summary>
    public void ResetTool() {
        if (!_activeTool) return;
        PlayerItem item = GetPlayerItem(_activeTool);
        if (item != PlayerItem.None) _activeTool.transform.localScale = _originalScales[item];
        _activeTool.SetActive(false);
        _activeTool = null;
    }

    /// <summary>
    /// Retrieves the game object associated with a specific player item.
    /// </summary>
    /// <param name="playerItem">The player item to retrieve the game object for.</param>
    /// <returns>The game object associated with the player item, or null if not found.</returns>
    public GameObject GetTool(PlayerItem playerItem) {
        return playerItem switch {
            PlayerItem.WateringCan => wateringCan.gameObject,
            PlayerItem.FungicideSpray => sprayBottle.gameObject,
            PlayerItem.InsecticideSoap => sprayBottle.gameObject,
            PlayerItem.NeemOil => sprayBottle.gameObject,
            PlayerItem.PruningShears => pruningShears.gameObject,
            PlayerItem.DrainageShovel => drainageShovel.gameObject,
            PlayerItem.Fertilizer => fertilizerJerrycan.gameObject,
            _ => null
        };
    }

    /// <summary>
    /// Retrieves the player item associated with a specific game object.
    /// </summary>
    /// <param name="tool">The game object to retrieve the player item for.</param>
    /// <returns>The player item associated with the game object, or PlayerItem.None if not found.</returns>
    private PlayerItem GetPlayerItem(GameObject tool) {
        if (tool == wateringCan.gameObject) return PlayerItem.WateringCan;
        if (tool == sprayBottle.gameObject) return PlayerItem.FungicideSpray;
        if (tool == pruningShears.gameObject) return PlayerItem.PruningShears;
        if (tool == drainageShovel.gameObject) return PlayerItem.DrainageShovel;
        return tool == fertilizerJerrycan.gameObject ? PlayerItem.Fertilizer : PlayerItem.None;
    }
}