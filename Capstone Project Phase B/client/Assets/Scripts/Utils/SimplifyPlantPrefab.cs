using System.IO;
using LoM.Super;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityMeshSimplifier;
#endif

[RequireComponent(typeof(MeshFilter))]
public class SimplifyPlantPrefab : SuperBehaviour {
    [Range(0f, 1f)] [SerializeField] private float quality = 0.5f;

    /// <summary>
    /// Simplifies the MeshFilter's mesh based on the given quality and saves the simplified mesh as a new asset.
    /// Only works in the Unity Editor.
    /// </summary>
    [ContextMenu("Simplify and Save Mesh")]
    public void SimplifyAndSafe() {
#if UNITY_EDITOR
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (!meshFilter) return;

        Mesh originalMesh = meshFilter.sharedMesh;
        Mesh clonedMesh = Instantiate(originalMesh);
        
        // Initialize the mesh simplifier with the cloned mesh
        MeshSimplifier meshSimplifier = new MeshSimplifier();
        meshSimplifier.Initialize(clonedMesh);
        
        // Simplify the mesh to the target quality (0 = most simplified, 1 = original detail)
        meshSimplifier.SimplifyMesh(quality);
        
        // Generate the simplified mesh and recalculate normals for proper shading
        Mesh simplifiedMesh = meshSimplifier.ToMesh();
        simplifiedMesh.RecalculateNormals();
        
        // Determine the original mesh asset path and its directory
        string originalPath = AssetDatabase.GetAssetPath(originalMesh);
        string directory = Path.GetDirectoryName(originalPath);
        if (directory == null) return;

        // Create a new asset path for the simplified mesh using the quality percentage
        string simplifiedPath = Path.Combine(directory, $"{originalMesh.name}_{quality * 100}_Simplified.asset");
        
        // Save the simplified mesh as a new asset
        AssetDatabase.CreateAsset(simplifiedMesh, simplifiedPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }
}