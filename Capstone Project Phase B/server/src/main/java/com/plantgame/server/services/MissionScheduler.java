package com.plantgame.server.services;

import com.plantgame.server.config.MissionConfig;
import com.plantgame.server.models.Mission;
import com.plantgame.server.models.User;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;

import static com.plantgame.server.config.MissionConfig.MISSION_TEMPLATES;

/**
 * The MissionScheduler class is responsible for managing and resetting daily missions
 * for all users. It interacts with DynamoDB tables to retrieve and update user-specific
 * missions and is scheduled to run daily at midnight UTC.
 * <p>
 * This class is annotated as a Spring Service and uses scheduled tasks to automate
 * the reset process. The main tasks involve fetching all users, querying their
 * missions, and resetting daily missions that require updates.
 * <p>
 * Features include:
 * - Scanning the user table to retrieve all users.
 * - Querying associated missions for each user from the mission table.
 * - Resetting progress and updating details for missions marked as "Daily".
 * - Ensuring updated missions are persisted in the database.
 * <p>
 * The reset process involves:
 * - Resetting the progress and completion status of a mission.
 * - Updating the reset date to the current date.
 * - Applying a mission template for descriptions, targets, and rewards where applicable.
 * <p>
 * Exceptions during the operation, such as failures to save updated missions,
 * or database-related issues, are logged to assist in debugging and ensuring
 * reliability of the system.
 */
@Service
public class MissionScheduler {

    @Autowired
    private DynamoDbTable<User> userTable;

    @Autowired
    private DynamoDbTable<Mission> missionTable;

    /**
     * Resets the daily missions for all users. This method is scheduled to run daily at midnight UTC.
     * <p>
     * The process involves the following steps:
     * 1. Retrieves all users from the user table in DynamoDB.
     * 2. For each user, identifies their missions that are of type "Daily" and checks if they need to be reset
     *    based on their reset date.
     * 3. Updates the missions that meet the reset conditions:
     *    - Resets the current progress to 0.
     *    - Marks the mission as not completed.
     *    - Sets the reset date to the current date.
     *    - Updates attributes like description, target progress, and reward points using pre-defined mission templates.
     * 4. Save the updated missions back to the mission table in DynamoDB.
     * <p>
     * If any errors occur during the process (e.g., failure to interact with the database), they will be logged
     * for further investigation.
     * <p>
     * This method ensures that all daily missions are properly reset and reflect the current day's state for
     * all users in the system.
     */
    @Scheduled(cron = "0 0 0 * * ?") // Run daily at midnight UTC
    public void resetDailyMissionsForAllUsers() {
        try {
            List<User> users = userTable.scan()
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .toList();

            List<Mission> updatedMissions = new ArrayList<>();
            for (User user : users) {
                Key missionKey = Key.builder().partitionValue(user.getUsername()).build();
                QueryConditional queryConditional = QueryConditional.keyEqualTo(missionKey);
                List<Mission> missions = missionTable.query(queryConditional)
                        .stream()
                        .flatMap(page -> page.items().stream())
                        .toList();

                String today = LocalDate.now().toString();
                for (Mission mission : missions) {
                    if (mission.getType().equals("Daily") &&
                            (mission.getResetDate() == null || !mission.getResetDate().equals(today))) {
                        mission.setCurrentProgress(0);
                        mission.setCompleted(false);
                        mission.setResetDate(today);
                        MissionConfig.MissionTemplate template = MISSION_TEMPLATES.stream()
                                .filter(t -> t.missionId.equals(mission.getMissionId()))
                                .findFirst()
                                .orElse(null);
                        if (template != null) {
                            mission.setDescription(template.descriptionFormat);
                            mission.setTargetProgress(template.targetProgress[0]);
                            mission.setPointsReward(template.pointsReward);
                        }
                        updatedMissions.add(mission);
                    }
                }
            }

            // Save updated missions
            for (Mission mission : updatedMissions) {
                try {
                    missionTable.putItem(mission);
                } catch (DynamoDbException e) {
                    System.err.println("Error saving mission " + mission.getMissionId() + " for user " + mission.getUsername() + ": " + e.getMessage());
                }
            }

            System.out.println("Reset " + updatedMissions.size() + " daily missions across all users.");
        } catch (DynamoDbException e) {
            System.err.println("Error resetting daily missions: " + e.getMessage());
        }
    }
}
