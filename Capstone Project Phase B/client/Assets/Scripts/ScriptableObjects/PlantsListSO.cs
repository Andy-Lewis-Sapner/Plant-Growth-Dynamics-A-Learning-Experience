using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// PlantsListSO is a ScriptableObject that maintains a list of PlantSO objects.
/// It acts as a central repository for plant data, providing methods to search and retrieve specific PlantSO objects.
/// </summary>
[CreateAssetMenu(fileName = "PlantsListSO", menuName = "Scriptable Objects/PlantsListSO")]
public class PlantsListSO : ScriptableObject {
    /// <summary>
    /// A list of plants represented as Scriptable Objects.
    /// This variable holds a collection of PlantSO instances, each containing data such as
    /// plant name, prefab, sprite, growth details, and other attributes used in the game logic.
    /// </summary>
    public List<PlantSO> plants;

    /// Finds a plant ScriptableObject (PlantSO) by its name within the list of available plants.
    /// <param name="plantName">The name of the plant to be searched for.</param>
    /// <returns>
    /// A reference to the PlantSO instance that matches the specified name, or null if no matching plant is found.
    /// </returns>
    public PlantSO FindPlantSoByName(string plantName) {
        return (from plantSo in plants
            where string.Equals(plantSo.plantName, plantName, StringComparison.InvariantCultureIgnoreCase)
            select plantSo).FirstOrDefault();
    }
}

