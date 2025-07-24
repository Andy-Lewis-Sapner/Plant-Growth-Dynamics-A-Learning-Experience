#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class InventoryManagerTests {
    private InventoryManager _inventoryManager;

    [SetUp]
    public void SetUp() {
        GameObject gameObject = new GameObject("InventoryManager");
        _inventoryManager = gameObject.AddComponent<InventoryManager>();
        typeof(Singleton<InventoryManager>).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            ?.SetValue(null, _inventoryManager);
        
        gameObject = new GameObject("NotificationPanelUI");
        NotificationPanelUI notificationPanelUI = gameObject.AddComponent<NotificationPanelUI>();
        typeof(Singleton<NotificationPanelUI>).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            ?.SetValue(null, notificationPanelUI);
    }   

    [Test]
    public void Points_CannotBeNegative() {
        _inventoryManager.Points = -100;
        Assert.AreEqual(0, _inventoryManager.Points);
    }

    [Test]
    public void AddTool_AddsToolIfNotExists() {
        const PlayerItem tool = PlayerItem.DrainageShovel;
        _inventoryManager.AddTool(tool);
        Assert.IsTrue(_inventoryManager.HasItem(tool));
    }

    [Test]
    public void AddPlants_UpdatesInventory() {
        PlantSO plantSo = ScriptableObject.CreateInstance<PlantSO>();
        Dictionary<PlantSO, int> plants = new Dictionary<PlantSO, int> { { plantSo, 3 } };

        bool eventTriggered = false;
        _inventoryManager.OnPlantsInventoryChanged += (_, _) => eventTriggered = true;
        
        _inventoryManager.AddPlants(plants);
        
        Assert.IsTrue(_inventoryManager.PlayerPlantsInventory.ContainsKey(plantSo));
        Assert.AreEqual(3, _inventoryManager.PlayerPlantsInventory[plantSo]);
        Assert.IsTrue(eventTriggered);
    }

    [Test]
    public void UsePlant_DecrementsQuantity() {
        PlantSO plantSo = ScriptableObject.CreateInstance<PlantSO>();
        _inventoryManager.PlayerPlantsInventory[plantSo] = 2;
        
        _inventoryManager.UsePlant(plantSo);
        
        Assert.AreEqual(1, _inventoryManager.PlayerPlantsInventory[plantSo]);
    }

    [Test]
    public void ReturnPlant_IncrementsQuantity() {
        PlantSO plantSo = ScriptableObject.CreateInstance<PlantSO>();
        _inventoryManager.PlayerPlantsInventory[plantSo] = 1;
        
        _inventoryManager.ReturnPlant(plantSo);
        
        Assert.AreEqual(2, _inventoryManager.PlayerPlantsInventory[plantSo]);
    }
    
    [Test]
    public void HasAnySeeds_ReturnsTrueWhenSeedsExist()
    {
        PlantSO plant = ScriptableObject.CreateInstance<PlantSO>();
        _inventoryManager.PlayerPlantsInventory[plant] = 1;

        Assert.IsTrue(_inventoryManager.HasAnySeeds());
    }

    [Test]
    public void AddFertilizers_AddsCorrectAmount()
    {
        FertilizerSO fertilizerSo = ScriptableObject.CreateInstance<FertilizerSO>();
        Dictionary<FertilizerSO, int> dict = new Dictionary<FertilizerSO, int> { { fertilizerSo, 2 } };

        _inventoryManager.AddFertilizers(dict);

        Assert.AreEqual(40, _inventoryManager.PlayerFertilizersInventory[fertilizerSo]); // 2 * 20
    }

    [Test]
    public void UseFertilizer_DecreasesAmount()
    {
        FertilizerSO fertilizerSo = ScriptableObject.CreateInstance<FertilizerSO>();
        _inventoryManager.PlayerFertilizersInventory[fertilizerSo] = 50;

        _inventoryManager.UseFertilizer(fertilizerSo);

        Assert.AreEqual(40, _inventoryManager.PlayerFertilizersInventory[fertilizerSo]);
    }

    [Test]
    public void SpendPoints_Succeeds_WhenEnoughPoints()
    {
        _inventoryManager.Points = 100;
        bool success = _inventoryManager.SpendPoints(50);

        Assert.IsTrue(success);
        Assert.AreEqual(50, _inventoryManager.Points);
    }

    [Test]
    public void SpendPoints_Fails_WhenNotEnoughPoints()
    {
        _inventoryManager.Points = 10;
        bool success = _inventoryManager.SpendPoints(50);

        Assert.IsFalse(success);
        Assert.AreEqual(10, _inventoryManager.Points);
    }
}
#endif