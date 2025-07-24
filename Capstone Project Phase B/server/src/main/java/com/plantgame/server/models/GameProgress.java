package com.plantgame.server.models;

import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbPartitionKey;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbSortKey;

import java.util.List;
import java.util.Map;

/**
 * Represents the progress of a game for a specific user. Captures various aspects
 * of the game state such as weather information, points, inventory, and system states.
 * <p>
 * This class is annotated for use with DynamoDB Enhanced Client for persistence.
 * The primary key for the DynamoDB table is defined by `username` (partition key)
 * and `progressId` (sort key).
 * <p>
 * Key Functionality:
 * - Tracks the user's game progress through a unique identifier (`progressId`).
 * - Maintains the user's current points.
 * - Records current weather data and the time of the latest weather update.
 * - Contains states of various systems, such as lights, air conditioners, sprinklers, etc.
 * - Manages the inventory of tools, plants, and fertilizers available to the user.
 * <p>
 * Annotations:
 * - The `@DynamoDbBean` annotation marks this class as a DynamoDB entity.
 * - `@DynamoDbPartitionKey` specifies `username` as the partition key.
 * - `@DynamoDbSortKey` specifies `progressId` as the sort key.
 */
@DynamoDbBean
public class GameProgress {

    private String username;
    private String progressId;
    private String lastWeatherUpdate;
    private List<HourlyWeatherEntry> hourlyWeather;
    private boolean houseLightsOn;
    private boolean houseAirConditionersOn;
    private boolean greenHouseLightsOn;
    private boolean greenHouseFansOn;
    private boolean greenHouseIrrigationOn;
    private boolean groundSprinklersOn;
    private boolean groundLightsOn;
    private int points;
    private List<String> playerAvailableTools;
    private Map<String, Integer> playerPlantsInventory;
    private Map<String, Integer> playerFertilizersInventory;

    @DynamoDbPartitionKey
    public String getUsername() {
        return username;
    }
    public void setUsername(String username) {
        this.username = username;
    }

    @DynamoDbSortKey
    public String getProgressId() {
        return progressId;
    }
    public void setProgressId(String progressId) {
        this.progressId = progressId;
    }

    public String getLastWeatherUpdate() {
        return lastWeatherUpdate;
    }
    public void setLastWeatherUpdate(String lastWeatherUpdate) {
        this.lastWeatherUpdate = lastWeatherUpdate;
    }

    public List<HourlyWeatherEntry> getHourlyWeather() {
        return hourlyWeather;
    }

    public void setHourlyWeather(List<HourlyWeatherEntry> hourlyWeather) {
        this.hourlyWeather = hourlyWeather;
    }

    public boolean getHouseLightsOn() {
        return houseLightsOn;
    }
    public void setHouseLightsOn(boolean houseLightsOn) {
        this.houseLightsOn = houseLightsOn;
    }

    public boolean getHouseAirConditionersOn() {
        return houseAirConditionersOn;
    }
    public void setHouseAirConditionersOn(boolean houseAirConditionersOn) {
        this.houseAirConditionersOn = houseAirConditionersOn;
    }

    public boolean getGreenHouseLightsOn() {
        return greenHouseLightsOn;
    }
    public void setGreenHouseLightsOn(boolean greenHouseLightsOn) {
        this.greenHouseLightsOn = greenHouseLightsOn;
    }

    public boolean getGreenHouseFansOn() {
        return greenHouseFansOn;
    }
    public void setGreenHouseFansOn(boolean greenHouseFansOn) {
        this.greenHouseFansOn = greenHouseFansOn;
    }

    public boolean getGreenHouseIrrigationOn() {
        return greenHouseIrrigationOn;
    }
    public void setGreenHouseIrrigationOn(boolean greenHouseIrrigationOn) {
        this.greenHouseIrrigationOn = greenHouseIrrigationOn;
    }

    public boolean getGroundSprinklersOn() {
        return groundSprinklersOn;
    }
    public void setGroundSprinklersOn(boolean groundSprinklersOn) {
        this.groundSprinklersOn = groundSprinklersOn;
    }

    public int getPoints() {
        return points;
    }

    public void setPoints(int points) {
        this.points = points;
    }

    public List<String> getPlayerAvailableTools() {
        return playerAvailableTools;
    }

    public void setPlayerAvailableTools(List<String> playerAvailableTools) {
        this.playerAvailableTools = playerAvailableTools;
    }

    public Map<String, Integer> getPlayerPlantsInventory() {
        return playerPlantsInventory;
    }

    public void setPlayerPlantsInventory(Map<String, Integer> playerPlantsInventory) {
        this.playerPlantsInventory = playerPlantsInventory;
    }

    public Map<String, Integer> getPlayerFertilizersInventory() {
        return playerFertilizersInventory;
    }

    public void setPlayerFertilizersInventory(Map<String, Integer> playerFertilizersInventory) {
        this.playerFertilizersInventory = playerFertilizersInventory;
    }

    public boolean getGroundLightsOn() {
        return groundLightsOn;
    }

    public void setGroundLightsOn(boolean groundLightsOn) {
        this.groundLightsOn = groundLightsOn;
    }
}
