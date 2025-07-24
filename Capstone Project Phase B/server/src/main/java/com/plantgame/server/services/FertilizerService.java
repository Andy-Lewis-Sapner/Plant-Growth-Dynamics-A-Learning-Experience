package com.plantgame.server.services;

import com.plantgame.server.models.FertilizerType;
import com.plantgame.server.models.Plant;
import com.plantgame.server.models.PlantType;
import com.plantgame.server.utils.EnvironmentUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

/**
 * Service class responsible for handling operations related to fertilizers and their effect
 * on plants and plant types in the system.
 * <p>
 * This service interacts with a DynamoDB table to retrieve and cache fertilizer information,
 * and calculates the nutrient level depletion and fertilizer boost for plants based
 * on environmental factors and plant attributes.
 * <p>
 * Key Responsibilities:
 * - Manages the nutrient levels and effect durations of fertilizers applied to plants.
 * - Applies environmental conditions like light levels and precipitation to adjust
 *   nutrient depletion.
 * - Provides functionality to calculate the growth boost of plants based on fertilizers.
 * <p>
 * Dependencies:
 * - DynamoDbTable<FertilizerType>: Interacts with the FertilizerType table in DynamoDB to
 *   retrieve fertilizer details.
 * - EnvironmentUtils: Provides utilities to fetch environmental effects like effective
 *   humidity, temperature, and light levels.
 * - CacheService: Manages a cache for fertilizer data to reduce database calls.
 */
@Service
public class FertilizerService {
    @Autowired
    private DynamoDbTable<FertilizerType> fertilizerTypeTable;

    @Autowired
    private EnvironmentUtils environmentUtils;

    @Autowired
    private CacheService cacheService;

    private static final float UPDATE_INTERVAL = 60f;

    /**
     * Updates the fertilizer effect on a given plant based on its current state, plant type,
     * environmental conditions, and fertilizer characteristics. Adjusts the nutrient level and
     * remaining effect time of the fertilizer applied to the plant, and removes the fertilizer
     * if its effect diminishes or is absent.
     *
     * @param plant the plant instance whose fertilizer properties are being updated
     * @param plantType the type of the plant, containing specific nutrient depletion characteristics
     * @param lightLevel the level of light exposure affecting nutrient depletion, measured in wm2
     * @param precipitationMm the amount of precipitation affecting nutrient depletion, measured in millimeters
     */
    public void updateFertilizer(Plant plant, PlantType plantType, float lightLevel, float precipitationMm) {
        if (plant.getNutrientLevel() <= 0 || plant.getRemainingEffectTime() <= 0) {
            plant.setNutrientLevel(0f);
            plant.setRemainingEffectTime(0f);
            plant.setFertilizerName(null);
            return;
        }

        FertilizerType fertilizerType = cacheService.getFertilizerTypeCache().get(plant.getFertilizerName());
        if (plant.getFertilizerName() != null) {
            try {
                Key fertilizerKey = Key.builder().partitionValue(plant.getFertilizerName()).build();
                fertilizerType = fertilizerTypeTable.getItem(fertilizerKey);
                if (fertilizerType != null) {
                    cacheService.getFertilizerTypeCache().put(plant.getFertilizerName(), fertilizerType);
                }
            } catch (DynamoDbException e) {
                System.err.println("Error loading FertilizerType for " + plant.getFertilizerName() + ": " + e.getMessage());
            }
        }

        if (fertilizerType == null) {
            System.err.println("FertilizerType not found for " + plant.getFertilizerName());
            plant.setNutrientLevel(0f);
            plant.setRemainingEffectTime(0f);
            plant.setFertilizerName(null);
            return;
        }

        float depletion = plantType.getNutrientDepletionRate() * UPDATE_INTERVAL / 3600f;
        depletion *= (0.8f + (lightLevel / 1400f) * 0.4f);
        if (plant.getPlantingLocationType().equals("GreenHouse")) {
            depletion *= 0.5f;
        }
        if (precipitationMm > 0) {
            depletion *= 1.2f;
        }

        plant.setNutrientLevel(Math.max(plant.getNutrientLevel() - depletion, 0f));
        plant.setRemainingEffectTime(Math.max(plant.getRemainingEffectTime() - UPDATE_INTERVAL, 0f));

        if (plant.getNutrientLevel() <= 0 || plant.getRemainingEffectTime() <= 0) {
            plant.setNutrientLevel(0f);
            plant.setRemainingEffectTime(0f);
            plant.setFertilizerName(null);
        }
    }

    /**
     * Retrieves the growth boost factor provided by the fertilizer applied to a plant.
     * The boost depends on the plant's current nutrient level, the remaining effect time of the fertilizer,
     * the compatibility of the fertilizer type with the plant type, and the specific characteristics
     * of the plant type.
     *
     * @param plant the plant instance to which the fertilizer is applied
     * @param plantType the type of the plant with specific fertilizer preferences and growth characteristics
     * @return the growth boost factor as a float value; returns 1f if no boost is applicable
     */
    public float getFertilizerBoost(Plant plant, PlantType plantType) {
        if (plant.getNutrientLevel() <= 0 || plant.getRemainingEffectTime() <= 0) {
            return 1f;
        }

        FertilizerType fertilizerType = cacheService.getFertilizerTypeCache().get(plant.getFertilizerName());
        if (fertilizerType == null) {
            try {
                Key fertilizerKey = Key.builder().partitionValue(plant.getFertilizerName()).build();
                fertilizerType = fertilizerTypeTable.getItem(fertilizerKey);
                if (fertilizerType != null) {
                    cacheService.getFertilizerTypeCache().put(plant.getFertilizerName(), fertilizerType);
                }
            } catch (DynamoDbException e) {
                System.err.println("Error loading FertilizerType for " + plant.getFertilizerName() + ": " + e.getMessage());
            }
        }

        if (fertilizerType == null) {
            System.err.println("FertilizerType not found for " + plant.getFertilizerName());
            return 1f;
        }

        float boost = plantType.getFertilizerGrowthBoost();
        if (!fertilizerType.getFertilizerType().equals(plantType.getPreferredFertilizerType())) {
            boost *= 0.8f;
        }
        return boost;
    }
}
