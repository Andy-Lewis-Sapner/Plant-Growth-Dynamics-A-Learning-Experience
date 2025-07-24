package com.plantgame.server.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;

import java.util.concurrent.Executor;

/**
 * Configuration class for enabling asynchronous processing and defining
 * a custom thread pool executor for handling specific asynchronous tasks.
 * <p>
 * This class leverages Spring's @EnableAsync annotation to enable asynchronous
 * method execution and configures a thread pool with a predefined core size,
 * maximum thread count, queue capacity, and thread name prefix.
 */
@Configuration
@EnableAsync
public class AsyncConfig {

    /**
     * Creates and returns a thread pool executor designed for executing tasks
     * related to plant updates. The executor is configured with a core pool size
     * of 2 threads, a maximum pool size of 5 threads, and a task queue capacity
     * of 20. All threads are prefixed with "PlantUpdate-" for easier identification
     * in logs or debugging scenarios.
     *
     * @return an instance of Executor configured as a thread pool for managing plant update tasks
     */
    @Bean(name = "plantUpdateExecutor")
    public Executor plantUpdateExecutor() {
        ThreadPoolTaskExecutor executor = new ThreadPoolTaskExecutor();
        executor.setCorePoolSize(2); // Number of threads to keep in the pool
        executor.setMaxPoolSize(5); // Maximum number of threads
        executor.setQueueCapacity(20); // Queue capacity for tasks
        executor.setThreadNamePrefix("PlantUpdate-");
        executor.initialize();
        return executor;
    }

}