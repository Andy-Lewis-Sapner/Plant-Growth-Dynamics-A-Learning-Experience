package com.plantgame.server.controllers;


import java.time.LocalDate;
import java.util.*;
import java.util.stream.Collectors;

import com.plantgame.server.config.MissionConfig;
import com.plantgame.server.models.*;
import com.plantgame.server.utils.Vector3;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbIndex;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.services.dynamodb.model.*;

import static com.plantgame.server.config.MissionConfig.MISSION_TEMPLATES;

/**
 * The GameController class is responsible for handling REST endpoints related to game functionality,
 * including managing user actions, saving and loading game data, and providing mission details. It
 * interacts with AWS DynamoDB via the injected DynamoDbClient to persist and retrieve game-related
 * data for users.
 * <p>
 * This class includes methods for user management, saving game progress, retrieving user information,
 * and loading game states. It also includes private utility methods to manipulate game data and user
 * information.
 */
@RestController
@RequestMapping("/api/game")
public class GameController {

    @Autowired
    private DynamoDbTable<User> userTable;

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    @Autowired
    private DynamoDbTable<Plant> plantTable;

    @Autowired
    private DynamoDbTable<Mission> missionTable;

    @Autowired
    private DynamoDbClient dynamoDbClient;

    private static final String PLANT_TABLE_NAME = "Plants";
    /**
     * A constant set of valid tool names used within the application for game-related functionalities.
     * This set defines the permissible tools that a user can use or interact with in the system.
     * It is used to validate game logic constraints.
     */
    private static final Set<String> VALID_TOOLS = new HashSet<>(Arrays.asList(
            "None", "WateringCan", "DrainageShovel", "FungicideSpray", "PruningShears",
            "InsecticideSoap", "ShadeTent", "NeemOil", "Fertilizer"
    ));

    /**
     * Retrieves a User object from the DynamoDB table based on the provided token.
     * The method queries the "token-index" secondary index to find a matching User entry.
     *
     * @param token The token used to identify the user in the "token-index".
     * @return The User object associated with the given token, or null if no match is found.
     */
    private User getUserByToken(String token) {
        DynamoDbIndex<User> tokenIndex = userTable.index("token-index");
        QueryConditional queryConditional = QueryConditional.keyEqualTo(
                Key.builder().partitionValue(token).build()
        );
        List<User> users = tokenIndex.query(queryConditional)
                .stream()
                .flatMap(page -> page.items().stream())
                .toList();
        return users.isEmpty() ? null : users.get(0);
    }


    /**
     * Handles a GET request to retrieve user information based on the provided token in the Authorization header.
     * This method validates the token, retrieves the user, and returns their details if authenticated.
     * In case of invalid token or database errors, an appropriate error response is returned.
     *
     * @param token The Authorization token provided in the request header for identifying the user.
     * @return A ResponseEntity containing a map with user data if the token is valid, or a map with an error message
     *         if the token is invalid or an error occurs during processing.
     */
    @GetMapping("/user")
    public ResponseEntity<Map<String, Object>> getUser(@RequestHeader("Authorization") String token) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid Token"));

            Map<String, Object> userData = getStringObjectMap(user);
            return ResponseEntity.ok(userData);
        } catch (Exception e) {
            System.err.println("Error fetching user: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Database error"));
        }
    }

    /**
     * Creates a map with key-value pairs of user properties. The keys are strings
     * representing property names, and the values are the properties of the User object.
     *
     * @param user the User object containing the data to be added to the map
     * @return a map where the keys are strings representing user property names
     *         and the values are the corresponding property values from the User object
     */
    private static Map<String, Object> getStringObjectMap(User user) {
        Map<String, Object> userData = new HashMap<>();
        userData.put("username", user.getUsername());
        userData.put("email", user.getEmail());
        userData.put("latitude", user.getLatitude());
        userData.put("longitude", user.getLongitude());
        userData.put("country", user.getCountry());
        userData.put("city", user.getCity());
        userData.put("isPlaying", user.getIsPlaying());
        userData.put("token", user.getToken());
        return userData;
    }

    /**
     * Saves the current game state for the authenticated user. The method handles saving game progress,
     * plants, and mission data. Upon successful saving, a response containing a success message is returned.
     * If any errors occur during the process, an appropriate error response is generated.
     *
     * @param token The Authorization token provided in the header to authenticate the user.
     * @param saveData A map containing the game data to be saved, including:
     *                 - "gameProgress": a map representing the game progress data
     *                 - "plants": a list of maps representing plant data
     *                 - "missions": a list of maps representing mission data
     * @return A ResponseEntity containing:
     *         - A map with a success message if the save operation is completed successfully.
     *         - A map with an error message if an error occurs (e.g., invalid token, database error).
     */
    @PostMapping("/save")
    public ResponseEntity<Map<String, Object>> saveGame(@RequestHeader("Authorization") String token, @RequestBody Map<String, Object> saveData) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid Token"));

            // Save GameProgress
            Map<String, Object> progressData = (Map<String, Object>) saveData.get("gameProgress");
            ResponseEntity<Map<String, Object>> progressResult = saveGameProgress(user, progressData);
            if (progressResult.getStatusCode().isError()) {
                return progressResult;
            }

            // Save Plants
            List<Map<String, Object>> plantsData = (List<Map<String, Object>>) saveData.get("plants");
            ResponseEntity<Map<String, Object>> plantsResult = savePlants(user, plantsData);
            if (plantsResult.getStatusCode().isError()) {
                return plantsResult;
            }

            // Save Missions
            List<Map<String, Object>> missionsData = (List<Map<String, Object>>) saveData.get("missions");
            ResponseEntity<Map<String, Object>> missionsResult = saveMissions(user, missionsData);
            if (missionsResult.getStatusCode().isError()) {
                return missionsResult;
            }

            return ResponseEntity.ok(Map.of("message", "Game saved successfully"));
        } catch (DynamoDbException e) {
            System.err.println("Error saving game: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Database error"));
        }
    }

    /**
     * Saves the game progress for a specified user to the database.
     * This method retrieves or creates a game progress record for the user, updates it using the provided progress data,
     * and saves it back to the database. If errors occur during database interaction, they are handled appropriately.
     *
     * @param user          The user for whom the game progress is being saved. The user's username is used as a key
     *                      to fetch or create the corresponding game progress record.
     * @param progressData  A map containing the game progress data, including various statuses and inventories that
     *                      need to be saved for the user. The keys and values in this map represent specific progress
     *                      attributes.
     * @return A ResponseEntity containing:
     *         - A map with a success message if the game progress is saved successfully.
     *         - A map with an error message and HTTP status 503 if an error occurs during the save operation.
     */
    private ResponseEntity<Map<String, Object>> saveGameProgress(User user, Map<String, Object> progressData) {
        try {
            // Fetch or create GameProgress
            Key progressKey = Key.builder().partitionValue(user.getUsername()).sortValue("default").build();
            GameProgress progress = gameProgressTable.getItem(progressKey);
            if (progress == null) {
                progress = new GameProgress();
                progress.setUsername(user.getUsername());
                progress.setProgressId("default");
            }

            // Update GameProgress fields
            progress.setLastWeatherUpdate((String) progressData.get("lastWeatherUpdate"));
            progress.setHouseLightsOn((Boolean) progressData.get("houseLightsOn"));
            progress.setHouseAirConditionersOn((Boolean) progressData.get("houseAirConditionersOn"));
            progress.setGreenHouseLightsOn((Boolean) progressData.get("greenHouseLightsOn"));
            progress.setGreenHouseFansOn((Boolean) progressData.get("greenHouseFansOn"));
            progress.setGreenHouseIrrigationOn((Boolean) progressData.get("greenHouseIrrigationOn"));
            progress.setGroundSprinklersOn((Boolean) progressData.get("groundSprinklersOn"));
            progress.setGroundLightsOn((Boolean) progressData.get("groundLightsOn"));

            Number points = (Number) progressData.get("points");
            if (points != null && points.intValue() >= 0) {
                progress.setPoints(points.intValue());
            } else {
                progress.setPoints(0);
            }

            // Validate tool names
            List<String> tools = (List<String>) progressData.get("playerAvailableTools");
            if (tools != null) {
                // Validate tool names
                List<String> validTools = tools.stream()
                        .filter(VALID_TOOLS::contains)
                        .collect(Collectors.toList());
                progress.setPlayerAvailableTools(validTools);
            } else {
                progress.setPlayerAvailableTools(new ArrayList<>());
            }

            // Validate plant and fertilizer inventories
            Map<String, Number> plantsInventory = (Map<String, Number>) progressData.get("playerPlantsInventory");
            if (plantsInventory != null) {
                Map<String, Integer> validPlantsInventory = plantsInventory.entrySet().stream()
                        .filter(entry -> entry.getValue().intValue() >= 0)
                        .collect(Collectors.toMap(Map.Entry::getKey, entry -> entry.getValue().intValue()));
                progress.setPlayerPlantsInventory(validPlantsInventory);
            } else {
                progress.setPlayerPlantsInventory(new HashMap<>());
            }

            // Validate plant and fertilizer inventories
            Map<String, Number> fertilizersInventory = (Map<String, Number>) progressData.get("playerFertilizersInventory");
            if (fertilizersInventory != null) {
                Map<String, Integer> validFertilizersInventory = fertilizersInventory.entrySet().stream()
                        .filter(entry -> entry.getValue().intValue() >= 0)
                        .collect(Collectors.toMap(
                                Map.Entry::getKey,
                                entry -> entry.getValue().intValue()
                        ));
                progress.setPlayerFertilizersInventory(validFertilizersInventory);
            } else {
                progress.setPlayerFertilizersInventory(new HashMap<>());
            }

            gameProgressTable.putItem(progress);
            return ResponseEntity.ok(Map.of("message", "GameProgress saved successfully"));
        } catch (DynamoDbException e) {
            System.err.println("Error saving GameProgress: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Error saving GameProgress"));
        }
    }

    /**
     * Saves plant data for a given user. This method handles deleting plants from the database
     * that are not included in the client request, as well as saving or updating plant data
     * from the client request. The plant data is processed in batch operations for efficiency.
     *
     * @param user The user associated with the plant data to be saved.
     * @param plantsData A list of plant data represented as maps, where each map contains
     *                   key-value pairs representing the properties of a plant.
     * @return A ResponseEntity containing a map with a message indicating the result of the operation.
     */
    private ResponseEntity<Map<String, Object>> savePlants(User user, List<Map<String, Object>> plantsData) {
        try {
            // Fetch existing plants from database
            Key plantKey = Key.builder().partitionValue(user.getUsername()).build();
            QueryConditional queryConditional = QueryConditional.keyEqualTo(plantKey);
            List<Plant> existingPlants = plantTable.query(queryConditional)
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .toList();

            // Get plant IDs from client request
            Set<String> clientPlantIds = plantsData != null
                    ? plantsData.stream()
                    .map(plantData -> (String) plantData.get("plantId"))
                    .collect(Collectors.toSet())
                    : new HashSet<>();

            // Identify plants to delete (in database but not in client request)
            List<WriteRequest> deleteRequests = existingPlants.stream()
                    .filter(plant -> !clientPlantIds.contains(plant.getPlantId()))
                    .map(plant -> WriteRequest.builder()
                            .deleteRequest(DeleteRequest.builder()
                                    .key(Map.of(
                                            "username", AttributeValue.builder().s(plant.getUsername()).build(),
                                            "plantId", AttributeValue.builder().s(plant.getPlantId()).build()
                                    ))
                                    .build())
                            .build())
                    .collect(Collectors.toList());

            // Perform batch deletes
            if (!deleteRequests.isEmpty()) {
                List<List<WriteRequest>> deleteBatches = new ArrayList<>();
                for (int i = 0; i < deleteRequests.size(); i += 25) {
                    deleteBatches.add(deleteRequests.subList(i, Math.min(i + 25, deleteRequests.size())));
                }

                for (List<WriteRequest> batch : deleteBatches) {
                    try {
                        BatchWriteItemResponse response = dynamoDbClient.batchWriteItem(
                                BatchWriteItemRequest.builder()
                                        .requestItems(Map.of(PLANT_TABLE_NAME, batch))
                                        .build()
                        );
                        if (!response.unprocessedItems().isEmpty()) {
                            System.err.println("Unprocessed delete items: " + response.unprocessedItems());
                        } else {
                            System.out.println("Deleted " + batch.size() + " plants for user " + user.getUsername());
                        }
                    } catch (DynamoDbException e) {
                        System.err.println("Error batch deleting plants: " + e.getMessage());
                    }
                }
            }

            // Save or update plants from the client
            if (plantsData != null && !plantsData.isEmpty()) {
                List<Plant> plants = plantsData.stream().map(plantData -> {
                    Plant plant = new Plant();
                    plant.setUsername(user.getUsername());
                    plant.setPlantId((String) plantData.get("plantId"));
                    plant.setPlantName((String) plantData.get("plantName"));
                    plant.setPlantingLocationType((String) plantData.get("plantingLocationType"));
                    Map<String, Object> position = (Map<String, Object>) plantData.get("position");
                    plant.setPosition(new Vector3(
                            ((Number) position.get("x")).floatValue(),
                            ((Number) position.get("y")).floatValue(),
                            ((Number) position.get("z")).floatValue()
                    ));
                    plant.setScale(((Number) plantData.get("scale")).doubleValue());
                    plant.setMoistureLevel(((Number) plantData.get("moistureLevel")).floatValue());
                    plant.setLastGrowthUpdate((String) plantData.get("lastGrowthUpdate"));
                    plant.setLastDiseaseCheck((String) plantData.get("lastDiseaseCheck"));
                    plant.setDisease((String) plantData.get("disease"));
                    plant.setDiseaseProgress(((Number) plantData.get("diseaseProgress")).floatValue());
                    plant.setDiseaseSlowingGrowthFactor(((Number) plantData.get("diseaseSlowingGrowthFactor")).floatValue());
                    plant.setShadeTentCounter(((Number) plantData.getOrDefault("shadeTentCounter", 0)).floatValue());
                    plant.setPlantableArea((String) plantData.get("plantableArea"));
                    plant.setReachedMaxScale((Boolean) plantData.get("reachedMaxScale"));
                    plant.setNutrientLevel(((Number) plantData.getOrDefault("nutrientLevel", 0)).floatValue());
                    plant.setRemainingEffectTime(((Number) plantData.getOrDefault("remainingEffectTime", 0)).floatValue());
                            plant.setFertilizerName((String) plantData.get("fertilizerName"));
                    return plant;
                }).toList();

                // Batch-write plants
                List<WriteRequest> writeRequests = plants.stream()
                        .map(plant -> WriteRequest.builder()
                                .putRequest(PutRequest.builder()
                                        .item(plant.toAttributeMap())
                                        .build())
                                .build())
                        .collect(Collectors.toList());

                // Split into batches of 25
                List<List<WriteRequest>> batches = new ArrayList<>();
                for (int i = 0; i < writeRequests.size(); i += 25) {
                    batches.add(writeRequests.subList(i, Math.min(i + 25, writeRequests.size())));
                }

                // Perform batch writes
                for (List<WriteRequest> batch : batches) {
                    try {
                        BatchWriteItemResponse response = dynamoDbClient.batchWriteItem(
                                BatchWriteItemRequest.builder()
                                        .requestItems(Map.of(PLANT_TABLE_NAME, batch))
                                        .build()
                        );
                        if (!response.unprocessedItems().isEmpty()) {
                            System.err.println("Unprocessed write items: " + response.unprocessedItems());
                        }
                    } catch (DynamoDbException e) {
                        System.err.println("Error batch writing plants: " + e.getMessage());
                        return ResponseEntity.status(503).body(Map.of("message", "Error saving plants"));
                    }
                }
            }

            return ResponseEntity.ok(Map.of("message", "Plants saved successfully"));
        } catch (DynamoDbException e) {
            System.err.println("Error saving plants: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Error saving plants"));
        }
    }

    /**
     * Saves mission data for the specified user. This method initializes missions if none exist for the user,
     * updates the mission progress based on the provided data, and saves the updated missions to the database.
     * If an error occurs during the process, an appropriate response is returned.
     *
     * @param user The user associated with the missions to be saved. The username is used to retrieve
     *             or initialize the missions.
     * @param missionsData A list of mission data represented as maps, where each map contains key-value
     *                     pairs representing mission properties such as "missionId", "currentProgress",
     *                     "completed", "pointsReward", "description", and "targetProgress".
     * @return A ResponseEntity containing:
     *         - A map with a success message if the missions are saved successfully.
     *         - A map with an error message and HTTP status 503 if an error occurs during the save operation.
     */
    private ResponseEntity<Map<String, Object>> saveMissions(User user, List<Map<String, Object>> missionsData) {
        try {
            // Initialize missions if none exist
            List<Mission> missions = loadMissions(user.getUsername());
            if (missions.isEmpty()) {
                missions = initializeMissions(user.getUsername());
                for (Mission mission : missions) {
                    missionTable.putItem(mission);
                }
            }

            // Update mission progress
            if (missionsData != null) {
                for (Map<String, Object> missionData : missionsData) {
                    String missionId = (String) missionData.get("missionId");
                    Mission mission = missions.stream()
                            .filter(m -> m.getMissionId().equals(missionId))
                            .findFirst()
                            .orElse(null);
                    if (mission == null) continue;

                    Number currentProgress = (Number) missionData.get("currentProgress");
                    if (currentProgress != null) mission.setCurrentProgress(currentProgress.intValue());

                    Boolean completed = (Boolean) missionData.get("completed");
                    if (completed != null) mission.setCompleted(completed);

                    Number pointsReward = (Number) missionData.get("pointsReward");
                    if (pointsReward != null) mission.setPointsReward(pointsReward.intValue());

                    String description = (String) missionData.get("description");
                    if (description != null) mission.setDescription(description);

                    Number targetProgress = (Number) missionData.get("targetProgress");
                    if (targetProgress != null) mission.setTargetProgress(targetProgress.intValue());

                    missionTable.putItem(mission);
                }
            }

            return ResponseEntity.ok(Map.of("message", "Missions saved successfully"));
        } catch (DynamoDbException e) {
            System.err.println("Error saving missions: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Error saving missions"));
        }
    }

    /**
     * Handles a POST request to claim a mission for an authenticated user.
     * This method validates the user's token, checks the mission's eligibility for claiming,
     * updates the user's game progress based on points earned, and resets or progresses the mission status.
     * It returns appropriate error messages for invalid inputs, database errors, or ineligible missions.
     *
     * @param token The Authorization token provided in the request header to authenticate the user.
     * @param request A map containing the mission details, expected to include:
     *                - "missionId": The unique identifier of the mission being claimed.
     * @return A ResponseEntity containing:
     *         - A map with a success message, the updated total points, and the mission details if the claim is successful.
     *         - A map with an error message and an appropriate HTTP status if the claim operation fails (e.g., invalid token,
     *           missing missionId, mission not found, mission not completed, mission already claimed, database error).
     */
    @PostMapping("/claim-mission")
    public ResponseEntity<Map<String, Object>> claimMission(@RequestHeader("Authorization") String token, @RequestBody Map<String, String> request) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid Token"));

            String missionId = request.get("missionId");
            if (missionId == null)
                return ResponseEntity.status(400).body(Map.of("message", "Missing missionId"));

            Key missionKey = Key.builder()
                    .partitionValue(user.getUsername())
                    .sortValue(missionId)
                    .build();
            Mission mission = missionTable.getItem(missionKey);
            if (mission == null)
                return ResponseEntity.status(404).body(Map.of("message", "Mission not found"));

            if (!mission.isCompleted())
                return ResponseEntity.status(400).body(Map.of("message", "Mission not completed"));
            if (mission.getPointsReward() == 0)
                return ResponseEntity.status(400).body(Map.of("message", "Mission already claimed"));

            // Update points in GameProgress
            Key progressKey = Key.builder().partitionValue(user.getUsername()).sortValue("default").build();
            GameProgress progress = gameProgressTable.getItem(progressKey);
            if (progress == null)
                return ResponseEntity.status(404).body(Map.of("message", "Game progress not found"));

            progress.setPoints(progress.getPoints() + mission.getPointsReward());
            mission.setPointsReward(0);

            // Handle mission progression
            if (mission.getType().equals("Permanent")) {
                MissionConfig.MissionTemplate template = MISSION_TEMPLATES.stream()
                        .filter(t -> t.missionId.equals(missionId))
                        .findFirst()
                        .orElse(null);
                if (template != null) {
                    int currentIndex = Arrays.binarySearch(template.targetProgress, mission.getTargetProgress());
                    if (currentIndex >= 0 && currentIndex + 1 < template.targetProgress.length) {
                        mission.setTargetProgress(template.targetProgress[currentIndex + 1]);
                        mission.setDescription(String.format(template.descriptionFormat, mission.getTargetProgress()));
                        mission.setPointsReward(template.pointsReward);
                        mission.setCompleted(false);
                    }
                }
            } else {
                mission.setCompleted(false);
            }

            gameProgressTable.putItem(progress);
            missionTable.putItem(mission);
            return ResponseEntity.ok(Map.of(
                    "message", "Mission claimed",
                    "points", progress.getPoints(),
                    "mission", mission
            ));
        } catch (DynamoDbException e) {
            System.err.println("Error claiming mission: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Database error"));
        }
    }

    /**
     * Handles a GET request to retrieve the list of missions for an authenticated user.
     * Validates the user's authorization token, retrieves their associated missions,
     * and returns the mission data. If missions are not found, they are initialized
     * for the user. Returns appropriate error responses for invalid tokens or issues
     * with database interaction.
     *
     * @param token The Authorization token provided in the request header to authenticate the user.
     * @return A ResponseEntity containing:
     *         - A list of missions associated with the authenticated user if successful.
     *         - A null body and HTTP status 401 if the token is invalid or the user is not authenticated.
     *         - A null body and HTTP status 503 if a database error occurs while retrieving missions.
     */
    @GetMapping("/missions")
    public ResponseEntity<List<Mission>> getMissions(@RequestHeader("Authorization") String token) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(null);

            // Load missions
            List<Mission> missions = loadMissions(user.getUsername());
            if (missions.isEmpty()) {
                missions = initializeMissions(user.getUsername());
                for (Mission mission : missions) {
                    missionTable.putItem(mission);
                }
            }
            return ResponseEntity.ok(missions);
        } catch (DynamoDbException e) {
            System.err.println("Error loading missions: " + e.getMessage());
            return ResponseEntity.status(503).build();
        }
    }

    /**
     * Loads the list of missions associated with the specified username from the database.
     * This method queries the mission table using the provided username as a key.
     *
     * @param username The username used to retrieve the missions from the database.
     * @return A list of Mission objects associated with the specified username.
     */
    private List<Mission> loadMissions(String username) {
        Key key = Key.builder().partitionValue(username).build();
        QueryConditional queryConditional = QueryConditional.keyEqualTo(key);
        return missionTable.query(queryConditional)
                .stream()
                .flatMap(page -> page.items().stream())
                .toList();
    }

    /**
     * Initializes a list of missions for a specified user based on predefined mission templates.
     * The method creates Mission objects for each template, sets their initial values, and associates them with the user.
     *
     * @param username The username of the user for whom the missions are being initialized.
     * @return A list of Mission objects initialized for the specified user.
     */
    private List<Mission> initializeMissions(String username) {
        List<Mission> missions = new ArrayList<>();
        String today = LocalDate.now().toString();
        for (MissionConfig.MissionTemplate template : MISSION_TEMPLATES) {
            Mission mission = new Mission();
            mission.setUsername(username);
            mission.setMissionId(template.missionId);
            mission.setType(template.type);
            mission.setDescription(template.type.equals("Permanent") ?
                    String.format(template.descriptionFormat, template.targetProgress[0]) :
                    template.descriptionFormat);
            mission.setCurrentProgress(0);
            mission.setTargetProgress(template.targetProgress[0]);
            mission.setPointsReward(template.pointsReward);
            mission.setCompleted(false);
            mission.setResetDate(template.type.equals("Daily") ? today : null);
            missions.add(mission);
        }
        return missions;
    }

    /**
     * Loads the current game state for the user identified by the provided authorization token.
     * The method retrieves game progress, plants, and missions associated with the user and
     * constructs the complete game state. If no game progress is found, an empty game state
     * is returned. If no missions are found, new missions are initialized and saved.
     *
     * @param token the authorization token identifying the user whose game state is to be loaded
     * @return a ResponseEntity containing the complete GameState object for the user, or an empty
     *         GameState if no progress exists. Returns a 401 status if the user cannot be authenticated
     *         and a 503 status if a DynamoDB error occurs.
     */
    @GetMapping("/load")
    public ResponseEntity<GameState> loadGame(@RequestHeader("Authorization") String token) {
        try {
            // Authenticate user using token
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(null);

            // Load GameProgress
            Key progressKey = Key.builder().partitionValue(user.getUsername()).sortValue("default").build();
            GameProgress progress = gameProgressTable.getItem(progressKey);
            if (progress == null)
                return ResponseEntity.ok(new GameState()); // Return an empty GameState if no progress exists

            // Load Plants
            Key plantKey = Key.builder().partitionValue(user.getUsername()).build();
            QueryConditional queryConditional = QueryConditional.keyEqualTo(plantKey);
            List<Plant> plants = plantTable.query(queryConditional)
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .toList();

            List<Mission> missions = loadMissions(user.getUsername());
            if (missions.isEmpty()) {
                missions = initializeMissions(user.getUsername());
                for (Mission mission : missions) {
                    missionTable.putItem(mission);
                }
            }

            // Construct GameState
            GameState gameState = getGameState(progress, plants, missions);
            return ResponseEntity.ok(gameState);
        } catch (DynamoDbException e) {
            System.err.println("DynamoDB error while loading game state: " + e.getMessage());
            return ResponseEntity.status(503).build();
        }
    }

    /**
     * Constructs a GameState object representing the current state of the game based on the provided
     * game progress, plants data, and active missions.
     *
     * @param progress The current GameProgress object containing details of the player's progress
     *                 and game state variables such as tools, inventory, and settings.
     * @param plants   A list of Plant objects representing the plants in the game.
     * @param missions A list of Mission objects representing the missions currently available or
     *                 being undertaken in the game.
     * @return A GameState object consolidating the provided game progress, plants, and missions into
     *         a single representation of the game's current state.
     */
    private static GameState getGameState(GameProgress progress, List<Plant> plants, List<Mission> missions) {
        GameState gameState = new GameState();
        Map<String, Object> progressMap = new HashMap<>();
        progressMap.put("username", progress.getUsername());
        progressMap.put("progressId", progress.getProgressId());
        progressMap.put("lastWeatherUpdate", progress.getLastWeatherUpdate() != null ? progress.getLastWeatherUpdate() : "");
        progressMap.put("houseLightsOn", progress.getHouseLightsOn());
        progressMap.put("houseAirConditionersOn", progress.getHouseAirConditionersOn());
        progressMap.put("greenHouseLightsOn", progress.getGreenHouseLightsOn());
        progressMap.put("greenHouseFansOn", progress.getGreenHouseFansOn());
        progressMap.put("greenHouseIrrigationOn", progress.getGreenHouseIrrigationOn());
        progressMap.put("groundSprinklersOn", progress.getGroundSprinklersOn());
        progressMap.put("groundLightsOn", progress.getGroundLightsOn());
        progressMap.put("points", progress.getPoints());
        progressMap.put("playerAvailableTools", progress.getPlayerAvailableTools() != null ? progress.getPlayerAvailableTools() : new ArrayList<>());
        progressMap.put("playerPlantsInventory", progress.getPlayerPlantsInventory() != null ? progress.getPlayerPlantsInventory() : new HashMap<>());
        progressMap.put("playerFertilizersInventory", progress.getPlayerFertilizersInventory() != null ? progress.getPlayerFertilizersInventory() : new HashMap<>());
        progressMap.put("missions", missions != null ? missions : new ArrayList<>());
        gameState.setGameProgress(progressMap);
        gameState.setPlants(plants);
        return gameState;
    }
}