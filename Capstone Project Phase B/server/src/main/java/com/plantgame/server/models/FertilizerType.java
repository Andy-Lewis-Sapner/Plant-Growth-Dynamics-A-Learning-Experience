package com.plantgame.server.models;

import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbPartitionKey;

/**
 * Represents a type of fertilizer with associated attributes.
 * This class is a DynamoDB Bean and supports persistence
 * using the AWS DynamoDB Enhanced Client.
 * <p>
 * The `FertilizerType` class provides information including
 * - The name of the fertilizer, which serves as the primary key.
 * - The category or type of fertilizer.
 * - The base nutrient amount associated with the fertilizer.
 * - The duration for which the fertilizer is active in hours.
 * <p>
 * Key Annotations:
 * - `@DynamoDbBean` signifies that this class is a DynamoDB mappable entity.
 * - `@DynamoDbPartitionKey` identifies the primary key for DynamoDB storage.
 */
@DynamoDbBean
public class FertilizerType {
    private String fertilizerName;
    private String fertilizerType;
    private float baseNutrientAmount;
    private float durationHours;

    @DynamoDbPartitionKey
    public String getFertilizerName() { return fertilizerName; }

    public void setFertilizerName(String fertilizerName) { this.fertilizerName = fertilizerName; }

    public String getFertilizerType() { return fertilizerType; }

    public void setFertilizerType(String fertilizerType) { this.fertilizerType = fertilizerType; }

    public float getBaseNutrientAmount() { return baseNutrientAmount; }

    public void setBaseNutrientAmount(float baseNutrientAmount) { this.baseNutrientAmount = baseNutrientAmount; }

    public float getDurationHours() { return durationHours; }

    public void setDurationHours(float durationHours) { this.durationHours = durationHours; }
}
