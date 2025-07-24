package com.plantgame.server;


import com.plantgame.server.models.GameProgress;
import com.plantgame.server.models.Plant;
import com.plantgame.server.models.PlantType;
import com.plantgame.server.models.User;
import org.mockito.Mockito;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;

@Configuration
public class TestDynamoDBConfig {

    @Bean
    @Primary
    public DynamoDbClient dynamoDbClient() {
        return Mockito.mock(DynamoDbClient.class);
    }

    @Bean
    @Primary
    public DynamoDbEnhancedClient dynamoDbEnhancedClient() {
        return DynamoDbEnhancedClient.builder()
                .dynamoDbClient(dynamoDbClient())
                .build();
    }

    @Bean
    @Primary
    public DynamoDbTable<User> userTable() {
        return Mockito.mock(DynamoDbTable.class);
    }

    @Bean
    @Primary
    public DynamoDbTable<GameProgress> gameProgressTable() {
        return Mockito.mock(DynamoDbTable.class);
    }

    @Bean
    @Primary
    public DynamoDbTable<Plant> plantTable() {
        return Mockito.mock(DynamoDbTable.class);
    }

    @Bean
    @Primary
    public DynamoDbTable<PlantType> plantTypeTable() {
        return Mockito.mock(DynamoDbTable.class);
    }

}