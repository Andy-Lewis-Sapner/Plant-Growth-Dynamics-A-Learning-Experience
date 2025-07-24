package com.plantgame.server.config;

import com.plantgame.server.models.*;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.TableSchema;

/**
 * Configuration class for defining DynamoDB table beans used in the application.
 * This class provides methods to configure and retrieve tables for various entity types
 * managed in the application.
 * <p>
 * Each method defines a bean for a specific table and uses the DynamoDbEnhancedClient
 * to create and map the table to its corresponding entity's schema.
 * <p>
 * The tables configured include:
 * - User table: Stores user-related information.
 * - Plant table: Stores plant-related data.
 * - PlantType table: Stores information about different plant types.
 * - GameProgress table: Tracks game progress for users.
 * - FertilizerType table: Manages data about different fertilizer types.
 * - Mission table: Stores mission-related data.
 */
@Configuration
public class DynamoDbTableConfig {
    @Bean
    public DynamoDbTable<User> userTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("Users", TableSchema.fromBean(User.class));
    }

    @Bean
    public DynamoDbTable<Plant> plantTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("Plants", TableSchema.fromBean(Plant.class));
    }

    @Bean
    public DynamoDbTable<PlantType> plantTypeTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("PlantTypes", TableSchema.fromBean(PlantType.class));
    }

    @Bean
    public DynamoDbTable<GameProgress> gameProgressTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("GameProgress", TableSchema.fromBean(GameProgress.class));
    }

    @Bean
    public DynamoDbTable<FertilizerType> fertilizerTypeTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("FertilizerTypes", TableSchema.fromBean(FertilizerType.class));
    }

    @Bean
    public DynamoDbTable<Mission> missionTable(DynamoDbEnhancedClient enhancedClient) {
        return enhancedClient.table("Missions", TableSchema.fromBean(Mission.class));
    }
}
