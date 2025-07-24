package com.plantgame.server.models;

import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbAttribute;

import java.util.Map;

/**
 * Represents a single hourly weather entry with various weather attributes.
 * This class is annotated with DynamoDbBean for integration with AWS DynamoDB.
 */
@DynamoDbBean
public class HourlyWeatherEntry {
    private String time;
    private double temperatureC;
    private int humidity;
    private double precipitationMm;
    private Map<String, String> condition;
    private int weatherCode;
    private double directRadiationWm2;
    private double diffuseRadiationWm2;

    @DynamoDbAttribute("time")
    public String getTime() { return time; }
    public void setTime(String time) { this.time = time; }

    @DynamoDbAttribute("temperatureC")
    public double getTemperatureC() { return temperatureC; }
    public void setTemperatureC(double temperatureC) { this.temperatureC = temperatureC; }

    @DynamoDbAttribute("humidity")
    public int getHumidity() { return humidity; }
    public void setHumidity(int humidity) { this.humidity = humidity; }

    @DynamoDbAttribute("precipitationMm")
    public double getPrecipitationMm() { return precipitationMm; }
    public void setPrecipitationMm(double precipitationMm) { this.precipitationMm = precipitationMm; }

    @DynamoDbAttribute("weatherCode")
    public int getWeatherCode() { return weatherCode; }
    public void setWeatherCode(int weatherCode) { this.weatherCode = weatherCode; }

    @DynamoDbAttribute("condition")
    public Map<String, String> getCondition() {
        return condition;
    }

    public void setCondition(Map<String, String> condition) {
        this.condition = condition;
    }

    @DynamoDbAttribute("directRadiationWm2")
    public double getDirectRadiationWm2() { return directRadiationWm2; }
    public void setDirectRadiationWm2(double directRadiationWm2) { this.directRadiationWm2 = directRadiationWm2; }

    @DynamoDbAttribute("diffuseRadiationWm2")
    public double getDiffuseRadiationWm2() { return diffuseRadiationWm2; }
    public void setDiffuseRadiationWm2(double diffuseRadiationWm2) { this.diffuseRadiationWm2 = diffuseRadiationWm2; }
}