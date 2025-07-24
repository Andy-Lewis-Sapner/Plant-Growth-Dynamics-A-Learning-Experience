using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds a list of fertilizers and provides utility methods for accessing fertilizers.
/// </summary>
[CreateAssetMenu(fileName = "FertilizerListSO", menuName = "Scriptable Objects/FertilizerListSO")]
public class FertilizerListSO : ScriptableObject {
    /// <summary>
    /// List of fertilizers stored as a collection of FertilizerSO objects.
    /// Used to manage and lookup fertilizer data within the project.
    /// </summary>
    public List<FertilizerSO> fertilizers;

    /// Retrieves a fertilizer from the list based on the given name.
    /// <param name="fertilizerName">The name of the fertilizer to search for.</param>
    /// <returns>The FertilizerSO object that matches the given name, or null if no match is found.</returns>
    public FertilizerSO GetFertilizerByName(string fertilizerName) {
        return fertilizers.FirstOrDefault(fertilizer => fertilizer.fertilizerName == fertilizerName);
    }
}