package com.plantgame.server.models;

import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbAttribute;

/**
 * LocationData represents environmental thresholds for various parameters such as temperature,
 * humidity, light levels, and soil moisture. These thresholds can be used to monitor or control
 * conditions in a specific location, such as for plant growth or environmental analysis.
 * <p>
 * The class is annotated with @DynamoDbBean to facilitate mapping with an AWS DynamoDB table.
 * Each attribute has a corresponding getter and setter, and is annotated with @DynamoDbAttribute
 * to define the specific attribute name in the DynamoDB table.
 */
@DynamoDbBean
public class LocationData {
    private Float minTemperature;
    private Float maxTemperature;
    private Float minHumidity;
    private Float maxHumidity;
    private Float minLight;
    private Float maxLight;
    private Float minMoisture;
    private Float maxMoisture;

    @DynamoDbAttribute("minTemperature")
    public Float getMinTemperature() {
        return minTemperature;
    }

    public void setMinTemperature(Float minTemperature) {
        this.minTemperature = minTemperature;
    }

    @DynamoDbAttribute("maxTemperature")
    public Float getMaxTemperature() {
        return maxTemperature;
    }

    public void setMaxTemperature(Float maxTemperature) {
        this.maxTemperature = maxTemperature;
    }

    @DynamoDbAttribute("minHumidity")
    public Float getMinHumidity() {
        return minHumidity;
    }

    public void setMinHumidity(Float minHumidity) {
        this.minHumidity = minHumidity;
    }

    @DynamoDbAttribute("maxHumidity")
    public Float getMaxHumidity() {
        return maxHumidity;
    }

    public void setMaxHumidity(Float maxHumidity) {
        this.maxHumidity = maxHumidity;
    }

    @DynamoDbAttribute("minLight")
    public Float getMinLight() {
        return minLight;
    }

    public void setMinLight(Float minLight) {
        this.minLight = minLight;
    }

    @DynamoDbAttribute("maxLight")
    public Float getMaxLight() {
        return maxLight;
    }

    public void setMaxLight(Float maxLight) {
        this.maxLight = maxLight;
    }

    @DynamoDbAttribute("minMoisture")
    public Float getMinMoisture() {
        return minMoisture;
    }

    public void setMinMoisture(Float minMoisture) {
        this.minMoisture = minMoisture;
    }

    @DynamoDbAttribute("maxMoisture")
    public Float getMaxMoisture() {
        return maxMoisture;
    }

    public void setMaxMoisture(Float maxMoisture) {
        this.maxMoisture = maxMoisture;
    }
}