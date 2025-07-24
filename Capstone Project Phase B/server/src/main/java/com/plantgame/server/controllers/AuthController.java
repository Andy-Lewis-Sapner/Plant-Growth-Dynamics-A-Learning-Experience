package com.plantgame.server.controllers;

import com.plantgame.server.models.GameProgress;
import com.plantgame.server.models.User;
import com.plantgame.server.utils.LoginResponse;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbIndex;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;

/**
 * Controller responsible for handling authentication-related operations such as
 * user registration, login, and logout.
 * <p>
 * This controller interacts with DynamoDB tables to store and manage user data
 * as well as game progress. It uses password encoding and token-based
 * authentication for user session management.
 */
@RestController
@RequestMapping("/api/auth")
public class AuthController {

    @Autowired
    private DynamoDbTable<User> userTable;

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    @Autowired
    private BCryptPasswordEncoder passwordEncoder;

    /**
     * Retrieves a User object based on the provided token.
     * Queries the DynamoDB "token-index" to find a user associated with the given token.
     *
     * @param token the token associated with the user to be retrieved
     * @return the User object associated with the given token, or null if no such user is found
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
     * Handles user registration by creating a new user record in the database and initializing the
     * user's game progress with default values. If the username already exists, the registration
     * request will be rejected.
     *
     * @param username the username provided by the user for registration
     * @param password the raw password provided by the user for registration, which will be encoded
     * @param email the email address provided by the user
     * @return a ResponseEntity containing a success message if the registration is successful,
     *         or an appropriate error message if the registration fails
     */
    @PostMapping("/register")
    public ResponseEntity<String> register(@RequestParam String username, @RequestParam String password, @RequestParam String email) {
        try {
            Key key = Key.builder().partitionValue(username).build();
            User existingUser = userTable.getItem(key);
            if (existingUser != null)
                return ResponseEntity.badRequest().body("Username already exists.");

            User user = new User();
            user.setUsername(username);
            user.setPassword(passwordEncoder.encode(password));
            user.setEmail(email);
            user.setIsPlaying(false);
            userTable.putItem(user);

            // Initialize GameProgress with 100 points
            GameProgress progress = getGameProgress(username);
            gameProgressTable.putItem(progress);

            return ResponseEntity.ok("User registered successfully.");
        } catch (DynamoDbException e) {
            System.err.println("DynamoDB error while registering user: " + e.getMessage());
            return ResponseEntity.status(503).body("Error registering user.");
        }
    }

    /**
     * Initializes and retrieves a default game progress object for the specified user.
     * The game progress is pre-configured with default values for all properties.
     *
     * @param username the username for which the game progress is being initialized
     * @return a GameProgress object containing the default progress state associated with the given username
     */
    private static GameProgress getGameProgress(String username) {
        GameProgress progress = new GameProgress();
        progress.setUsername(username);
        progress.setProgressId("default");
        progress.setPoints(100);
        progress.setHouseLightsOn(false);
        progress.setHouseAirConditionersOn(false);
        progress.setGreenHouseLightsOn(false);
        progress.setGreenHouseFansOn(false);
        progress.setGreenHouseIrrigationOn(false);
        progress.setGroundSprinklersOn(false);
        progress.setPlayerAvailableTools(new ArrayList<>());
        progress.setPlayerPlantsInventory(new HashMap<>());
        progress.setPlayerFertilizersInventory(new HashMap<>());
        return progress;
    }

    /**
     * Authenticates a user based on the provided username and password.
     * If the login credentials are valid and the user is not yet logged in,
     * a unique token is generated and returned for session management.
     *
     * @param username the username provided by the user for login
     * @param password the raw password provided by the user for authentication
     * @return a ResponseEntity containing an LoginResponse object. If the login is successful,
     *         the response includes a token and a success message. Otherwise, an error message
     *         is returned with an appropriate HTTP status code.
     */
    @PostMapping("/login")
    public ResponseEntity<LoginResponse> login(@RequestParam String username, @RequestParam String password) {
        try {
            Key key = Key.builder().partitionValue(username).build();
            User user = userTable.getItem(key);
            if (user == null || !passwordEncoder.matches(password, user.getPassword()))
                return ResponseEntity.status(401).body(new LoginResponse(null, "Invalid username or password."));

            if (user.getIsPlaying())
                return ResponseEntity.status(401).body(new LoginResponse(null, "User is already logged in."));

            String token = UUID.randomUUID().toString();
            user.setToken(token);
            user.setIsPlaying(true);
            user.setLastActiveTime(ZonedDateTime.now(ZoneOffset.UTC).format(DateTimeFormatter.ISO_OFFSET_DATE_TIME));
            userTable.putItem(user);

            return ResponseEntity.ok(new LoginResponse(token, "Login successful."));
        } catch (DynamoDbException e) {
            System.err.println("DynamoDB error while logging in: " + e.getMessage());
            return ResponseEntity.status(503).body(new LoginResponse(null, "Error logging in."));
        }
    }

    /**
     * Handles the logout process for a user by invalidating their session token.
     * Updates the user's record in the database to reflect the logout status.
     *
     * @param token the authorization token provided by the user for authentication
     * @return a ResponseEntity containing a success message if the logout is successful,
     *         or a relevant error message if the token is invalid or a database error occurs
     */
    @PostMapping("/logout")
    public ResponseEntity<Map<String, Object>> logout(@RequestHeader("Authorization") String token) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid Token"));

            user.setToken(null);
            user.setIsPlaying(false);
            userTable.putItem(user);

            return ResponseEntity.ok(Map.of("message", "Logged out successfully"));
        } catch (DynamoDbException e) {
            System.err.println("Error during logout: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("message", "Database error"));
        }
    }
}


