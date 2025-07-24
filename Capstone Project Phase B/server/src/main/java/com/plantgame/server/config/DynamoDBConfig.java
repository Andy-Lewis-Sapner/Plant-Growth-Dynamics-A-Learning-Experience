package com.plantgame.server.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import software.amazon.awssdk.auth.credentials.AwsBasicCredentials;
import software.amazon.awssdk.auth.credentials.DefaultCredentialsProvider;
import software.amazon.awssdk.auth.credentials.StaticCredentialsProvider;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.services.dynamodb.DynamoDbClientBuilder;

import java.net.URI;

/**
 * Configuration class for setting up AWS DynamoDB client.
 * This class is responsible for configuring and creating beans
 * for the DynamoDB client and enhanced client with appropriate properties
 * such as endpoint, access key, secret key, and region.
 * It supports both static and default credential providers.
 */
@Configuration
public class DynamoDBConfig {
	@Value("${amazon.dynamodb.endpoint}")
	private String amazonDynamoDBEndpoint;

	@Value("${amazon.aws.accessKey:}")
	private String amazonAWSAccessKey;

	@Value("${amazon.aws.secretKey:}")
	private String amazonAWSSecretKey;

	@Value("${amazon.aws.region}")
	private String amazonAWSRegion;

	/**
	 * Configures and provides a DynamoDbClient bean.
	 * The method sets up the AWS DynamoDB client with the specified region,
	 * endpoint, and credentials. If an access key and secret key are provided,
	 * it uses static credentials, otherwise it defaults to the default
	 * credentials provider.
	 *
	 * @return a configured instance of DynamoDbClient
	 */
	@Bean
	public DynamoDbClient dynamoDbClient() {
		DynamoDbClientBuilder builder = DynamoDbClient.builder()
				.region(Region.of(amazonAWSRegion))
				.endpointOverride(URI.create(amazonDynamoDBEndpoint));

		if (!amazonAWSAccessKey.isEmpty() && !amazonAWSSecretKey.isEmpty()) {
			builder.credentialsProvider(
					StaticCredentialsProvider.create(
							AwsBasicCredentials.create(amazonAWSAccessKey, amazonAWSSecretKey)
					)
			);
		} else {
			builder.credentialsProvider(DefaultCredentialsProvider.create());
		}

		return builder.build();
	}

	/**
	 * Configures and provides a DynamoDbEnhancedClient bean.
	 * This method sets up an enhanced DynamoDB client, which provides
	 * a high-level abstraction for working with DynamoDB tables and items.
	 * The client uses the provided DynamoDbClient for low-level communication.
	 *
	 * @param dynamoDbClient the DynamoDB client that will be used by the enhanced client
	 * @return a configured instance of DynamoDbEnhancedClient
	 */
	@Bean
	public DynamoDbEnhancedClient enhancedClient(DynamoDbClient dynamoDbClient) {
		return DynamoDbEnhancedClient.builder()
				.dynamoDbClient(dynamoDbClient)
				.build();
	}
}
