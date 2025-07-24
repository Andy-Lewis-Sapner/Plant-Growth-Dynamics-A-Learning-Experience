package com.plantgame.server.controllers;

import com.plantgame.server.models.User;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbIndex;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Map;

/**
 * A REST controller for handling heartbeat-related functionality.
 * Provides an endpoint to verify user sessions and update activity status in the system.
 */
@RestController
@RequestMapping("/heartbeat")
public class HeartbeatController {

    @Autowired
    private DynamoDbTable<User> userTable;

    /**
     * Retrieves a user associated with the specified token by querying the "token-index" in the DynamoDB table.
     *
     * @param token the unique token used to identify the user in the "token-index"
     * @return the User object associated with the token, or null if no matching user is found
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
     * Handles a POST request to verify a user's session and update their last activity timestamp.
     * If the provided token is valid, the method updates the user's last active time in the database
     * and returns a response indicating success. If the token is invalid or a database error occurs,
     * appropriate error responses are returned.
     *
     * @param token the authorization token of the user sent in the "Authorization" header
     * @return a ResponseEntity containing:
     *         - a success message with a timestamp if the operation succeeds
     *         - an error message if the token is invalid or if a database error occurs
     */
    @PostMapping
    public ResponseEntity<?> ping(@RequestHeader("Authorization") String token) {
        try {
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("error", "Invalid token"));

            String now = ZonedDateTime.now(ZoneOffset.UTC).format(DateTimeFormatter.ISO_OFFSET_DATE_TIME);
            user.setLastActiveTime(now);
            userTable.putItem(user);

            return ResponseEntity.ok(Map.of("status", "pong", "timestamp", now));
        } catch (DynamoDbException e) {
            return ResponseEntity.status(503).body(Map.of("error", "Database error"));
        }
    }

}
