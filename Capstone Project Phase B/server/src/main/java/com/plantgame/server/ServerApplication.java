package com.plantgame.server;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * ServerApplication serves as the entry point for the Spring Boot application.
 * It enables asynchronous processing and scheduling capabilities within the application
 * by using the @EnableAsync and @EnableScheduling annotations.
 * <p>
 * The @SpringBootApplication annotation marks this class as a Spring Boot configuration
 * class and automatically enables component scanning, autoconfiguration, and Spring
 * Boot's additional setup features.
 */
@SpringBootApplication
@EnableAsync
@EnableScheduling
public class ServerApplication {

	public static void main(String[] args) {
		SpringApplication.run(ServerApplication.class, args);
	}

}
