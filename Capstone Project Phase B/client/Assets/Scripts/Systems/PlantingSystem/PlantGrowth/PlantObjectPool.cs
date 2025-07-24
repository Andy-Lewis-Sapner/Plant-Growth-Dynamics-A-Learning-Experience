using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Provides an object pool for managing reusable plant GameObjects in Unity.
/// Extends the Singleton pattern to ensure a single instance exists.
public class PlantObjectPool : Singleton<PlantObjectPool> {
    /// <summary>
    /// Stores a mapping of plant prefab GameObjects to their corresponding object pools.
    /// Each pool is represented by a queue of inactive GameObject instances.
    /// </summary>
    private readonly Dictionary<GameObject, Queue<GameObject>> _plantPools = new();

    /// <summary>
    /// Default initial size for the plant object pool.
    /// </summary>
    private const int InitialPoolSize = 35;

    /// Represents the total number of plant pools managed by the object pool.
    public int PoolSize => _plantPools.Count;

    /// <summary>
    /// Initializes the plant object pool by iterating through the list of plants
    /// and setting up object pools for their respective prefabs.
    /// </summary>
    private void Start() {
        PlantsListSO plantsListSo = DataManager.Instance.PlantsListSo;
        foreach (GameObject plantPrefab in plantsListSo.plants.Select(plantSo => plantSo.plantPrefab))
            InitializePool(plantPrefab);
    }

    /// Initializes a pool for a specified plant prefab with a set or default size.
    /// <param name="plantPrefab">The plant prefab to initialize a pool for.</param>
    /// <param name="poolSize">The number of instances to include in the pool. Defaults to the preset size if not provided.</param>
    private void InitializePool(GameObject plantPrefab, int poolSize = -1) {
        if (_plantPools.ContainsKey(plantPrefab)) return;
        if (poolSize == -1) poolSize = InitialPoolSize;
        _plantPools[plantPrefab] = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++) {
            GameObject plant = Instantiate(plantPrefab);
            plant.SetActive(false);
            _plantPools[plantPrefab].Enqueue(plant);
        }
    }

    /// Retrieves a plant object from the pool or instantiates one if none are available.
    /// <param name="plantPrefab">The prefab of the plant to retrieve or instantiate.</param>
    /// <param name="position">The position where the plant should be placed.</param>
    /// <returns>A GameObject representing the plant.</returns>
    public GameObject GetPlant(GameObject plantPrefab, Vector3 position) {
        if (!_plantPools.ContainsKey(plantPrefab)) InitializePool(plantPrefab);
        
        GameObject plant;
        if (_plantPools[plantPrefab].Count > 0) {
            plant = _plantPools[plantPrefab].Dequeue();
            plant.SetActive(true);
        } else {
            plant = Instantiate(plantPrefab);
        }
        
        plant.transform.position = position;
        return plant;
    }

    /// Returns a plant object to the specified plant pool or destroys it if the pool does not exist.
    /// <param name="plant">The plant GameObject to be returned to the pool.</param>
    /// <param name="plantPrefab">The prefab representing the plant pool to return the plant to.</param>
    public void ReturnPlant(GameObject plant, GameObject plantPrefab) {
        if (_plantPools.TryGetValue(plantPrefab, out Queue<GameObject> plantPool)) {
            if (plant.TryGetComponent(out PlantInstance plantInstance)) plantInstance.ResetForPool();
            StartCoroutine(DataManager.Instance.SaveGameCoroutine());
            plant.SetActive(false);
            plantPool.Enqueue(plant);
        } else {
            Destroy(plant);
        }
    }
}