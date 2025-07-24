package com.plantgame.server.services;

import com.plantgame.server.models.FertilizerType;
import com.plantgame.server.models.PlantType;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

import java.util.HashMap;
import java.util.Map;

/**
 * CacheService is responsible for managing in-memory caches for plant and fertilizer types.
 * The caching mechanism reduces the need for repeated queries to the DynamoDB tables,
 * improving the performance and responsiveness of applications that rely on this data.
 * <p>
 * Key Responsibilities:
 * - Cache plant types from a DynamoDB table with a specified time-to-live (TTL).
 * - Cache fertilizer types from a DynamoDB table with the same TTL mechanism.
 * - Provide access to the current state of the cached data.
 * - Determine if the cache needs to be updated based on the TTL.
 * - Update caches by fetching data from the respective DynamoDB tables.
 *
 * Annotations:
 * - `@Service` marks this class as a Spring service for dependency injection.
 * - `@Autowired` injects required dependencies like DynamoDB tables.
 * <p>
 * Exception Management:
 * - Handles `DynamoDbException` during cache updates by logging to the standard error stream.
 * <p>
 * Behavior:
 * - The cache will refresh its data if the time elapsed since the last update exceeds the TTL.
 * - Caches are stored as HashMaps for efficient lookups by their key values
 *   (plant name for PlantType, fertilizer name for FertilizerType).
 */
@Service
public class CacheService {
    @Autowired
    private DynamoDbTable<PlantType> plantTypeTable;

    @Autowired
    private DynamoDbTable<FertilizerType> fertilizerTypeTable;

    private final Map<String, PlantType> plantTypeCache = new HashMap<>();
    private final Map<String, FertilizerType> fertilizerTypeCache = new HashMap<>();
    private long lastCacheUpdate = 0;
    private static final long CACHE_TTL = 3600_000; // 1 hour in milliseconds

    public Map<String, PlantType> getPlantTypeCache() {
        return plantTypeCache;
    }

    public Map<String, FertilizerType> getFertilizerTypeCache() {
        return fertilizerTypeCache;
    }

    /**
     * Updates the in-memory caches for plant types and fertilizer types by retrieving
     * the latest data from the respective DynamoDB tables. This operation ensures that
     * the cached data remains consistent and up to date with the underlying data sources.
     * <p>
     * Responsibility:
     * - Clears the existing plant type and fertilizer type caches.
     * - Fetches all items from the plant type DynamoDB table and populates the plant type cache.
     * - Fetches all items from the fertilizer type DynamoDB table and populates the fertilizer type cache.
     * - Updates the timestamp indicating when the cache was last refreshed.
     * <p>
     * Exception Handling:
     * - Catches and logs `DynamoDbException` if an error occurs during the DynamoDB scan operation.
     * <p>
     * Behavior:
     * - Each cache is a map where the key corresponds to the unique name of the type
     *   (e.g., plant name for plant types, fertilizer name for fertilizer types).
     * - Utilizes DynamoDB table scanning to retrieve all items, processes the result pages
     *   and updates the caches in memory.
     */
    public void updateCaches() {
        try {
            plantTypeCache.clear();
            plantTypeTable.scan()
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .forEach(pt -> plantTypeCache.put(pt.getPlantName(), pt));

            fertilizerTypeCache.clear();
            fertilizerTypeTable.scan()
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .forEach(ft -> fertilizerTypeCache.put(ft.getFertilizerName(), ft));

            lastCacheUpdate = System.currentTimeMillis();
        } catch (DynamoDbException e) {
            System.err.println("Error updating caches: " + e.getMessage());
        }
    }

    public boolean shouldUpdateCache() {
        return System.currentTimeMillis() - lastCacheUpdate > CACHE_TTL;
    }

    public long getLastCacheUpdate() {
        return lastCacheUpdate;
    }
}
