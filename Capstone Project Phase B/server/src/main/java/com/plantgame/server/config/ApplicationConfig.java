package com.plantgame.server.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;

/**
 * Configuration class for defining application-specific beans.
 * This class is responsible for setting up and exposing beans
 * related to application functionality.
 */
@Configuration
public class ApplicationConfig {

    /**
     * Creates and returns a bean of BCryptPasswordEncoder for password encoding.
     * This encoder is used to hash passwords securely using the BCrypt hashing algorithm.
     *
     * @return an instance of BCryptPasswordEncoder
     */
    @Bean
    public BCryptPasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

}
