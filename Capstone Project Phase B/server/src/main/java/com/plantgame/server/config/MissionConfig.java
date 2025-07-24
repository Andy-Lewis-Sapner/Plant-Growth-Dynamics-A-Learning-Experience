package com.plantgame.server.config;

import java.util.Arrays;
import java.util.List;

/**
 * The MissionConfig class provides configuration details for mission templates used
 * in the application. It defines a set of milestones and a list of predefined mission
 * templates for daily and permanent missions.
 *
 * This class contains an inner static class, MissionTemplate, which represents the
 * structure of individual mission templates, including properties like mission ID,
 * type, description format, target progress, and points reward.
 *
 * Key Features:
 * - Defines milestone thresholds for various missions as an array of integers.
 * - Includes predefined mission templates categorized into "Daily" and "Permanent" types.
 */
public class MissionConfig {
    public static final int[] MILESTONES = new int[]{3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 40, 50, 60, 70, 80, 90, 100};

    public static final List<MissionTemplate> MISSION_TEMPLATES = Arrays.asList(
            // Daily Missions
            new MissionTemplate("Daily_CheckPlants", "Daily", "Check 5 plants", 5, 10),
            new MissionTemplate("Daily_WaterPlant", "Daily", "Water a plant", 1, 8),
            new MissionTemplate("Daily_ApplyFertilizer", "Daily", "Apply fertilizer to a plant", 1, 12),
            new MissionTemplate("Daily_ToggleSetting", "Daily", "Toggle an environment setting", 1, 5),
            new MissionTemplate("Daily_CureDisease", "Daily", "Cure a plant disease", 1, 15),
            new MissionTemplate("Daily_BuyItem", "Daily", "Buy an item from the store", 1, 10),
            new MissionTemplate("Daily_PlantSeed", "Daily", "Plant a seed", 1, 8),
            new MissionTemplate("Daily_CheckWeather", "Daily", "Check the weather forecast", 1, 5),
            new MissionTemplate("Daily_UseTool", "Daily", "Use a tool", 1, 7),
            // Permanent Missions
            new MissionTemplate("Permanent_PlantSeeds", "Permanent", "Plant %d seeds", MILESTONES, 20),
            new MissionTemplate("Permanent_CureDiseases", "Permanent", "Cure %d plant diseases", MILESTONES, 30),
            new MissionTemplate("Permanent_BuyItems", "Permanent", "Buy %d items", MILESTONES, 20),
            new MissionTemplate("Permanent_GrowMaxScale", "Permanent", "Grow %d plants to max scale", MILESTONES, 30)
    );

    public static class MissionTemplate {
        public String missionId;
        public String type;
        public String descriptionFormat;
        public int[] targetProgress;
        public int pointsReward;

        MissionTemplate(String missionId, String type, String descriptionFormat, int targetProgress, int pointsReward) {
            this.missionId = missionId;
            this.type = type;
            this.descriptionFormat = descriptionFormat;
            this.targetProgress = new int[]{targetProgress};
            this.pointsReward = pointsReward;
        }

        MissionTemplate(String missionId, String type, String descriptionFormat, int[] targetProgress, int pointsReward) {
            this.missionId = missionId;
            this.type = type;
            this.descriptionFormat = descriptionFormat;
            this.targetProgress = targetProgress;
            this.pointsReward = pointsReward;
        }
    }
}