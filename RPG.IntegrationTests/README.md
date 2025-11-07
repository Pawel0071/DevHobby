# RPG Integration Tests

## Overview
This project contains integration tests for MongoDB, Redis, and RabbitMQ services.

## Test Approaches

### Option 1: Using Testcontainers (Recommended for CI/CD)
The integration tests use Testcontainers to automatically spin up Docker containers for testing. This approach requires:
- Docker running on your machine
- Docker Hub authentication (if images need to be pulled)
- Images available locally or ability to pull from registry

**Current Issue**: Docker Hub authentication is required but email verification is pending.

**Solutions**:
1. **Verify your Docker Hub email** - Check your email for verification link
2. **Logout from Docker Hub** if you don't need authenticated pulls:
   ```bash
   docker logout
   ```
3. **Pull images manually first**:
   ```bash
   docker pull mongo:latest
   docker pull redis:latest
   docker pull rabbitmq:4-management
   docker pull testcontainers/ryuk:0.5.1
   ```

### Option 2: Using Docker Compose Environment (Current Setup)
Since you already have a working Docker Compose setup, you can test against those containers directly.

#### Prerequisites
1. Start the Docker Compose environment:
   ```bash
   cd /Volumes/Data/Repositories/DevHobby
   docker compose up -d
   ```

2. Verify all containers are running:
   ```bash
   docker ps
   ```

#### Test Configuration
Update test connection strings to point to Docker Compose services:
- MongoDB: `mongodb://localhost:27017`
- Redis: `localhost:6379`
- RabbitMQ: `amqp://guest:guest@localhost:5672`

#### Manual Testing
You can manually verify the infrastructure is working:

**MongoDB**:
```bash
docker exec -it devhobby-mongodb-1 mongosh --eval "db.adminCommand('ping')"
```

**Redis**:
```bash
docker exec -it devhobby-redis-1 redis-cli ping
```

**RabbitMQ**:
```bash
docker exec -it devhobby-rabbitmq-1 rabbitmqctl status
```

### Option 3: Using Local Services
Install MongoDB, Redis, and RabbitMQ locally and configure tests to use localhost connections.

## Running Tests

### With Testcontainers (after fixing Docker auth)
```bash
dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj
```

### Against Docker Compose Environment
1. Ensure Docker Compose is running
2. Run tests:
   ```bash
   dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj
   ```

## Test Structure

### TestContainersFixture.cs
- Base fixture that sets up all infrastructure containers
- Implements `IAsyncLifetime` for proper setup/teardown
- Shared across all test classes using `IClassFixture<TestContainersFixture>`

### MongoDbIntegrationTests.cs
Tests for MongoDB operations:
- Connection verification
- CRUD operations (Insert, Read, Update, Delete)
- Query filtering
- Collection management

### RedisIntegrationTests.cs
Tests for Redis operations:
- Connection verification
- String operations (Set/Get)
- TTL/Expiry management
- Hash operations
- Sets and Lists
- Counter increments

### RabbitMqIntegrationTests.cs
Tests for RabbitMQ operations:
- Connection verification
- Exchange declaration
- Queue management
- Message publishing and consuming
- Fanout exchanges
- Message counting

## Test Coverage

- **24 integration tests** total
- **6 MongoDB tests**
- **10 Redis tests**
- **8 RabbitMQ tests**

All tests use FluentAssertions for readable assertions and comprehensive error messages.

## Troubleshooting

### Docker Authentication Error
```
Docker API responded with status code=Unauthorized, response={"message":"authentication required - email must be verified before using account"}
```

**Solutions**:
1. Verify Docker Hub email
2. Run `docker logout` and retry
3. Pull images manually before running tests
4. Use existing Docker Compose containers

### Container Startup Timeout
If containers take too long to start, you may need to:
1. Allocate more resources to Docker Desktop
2. Pull images beforehand
3. Increase timeout in `TestContainersFixture.cs`

### Port Conflicts
Ensure no other services are using the required ports:
- MongoDB: 27017
- Redis: 6379
- RabbitMQ: 5672, 15672

## CI/CD Integration

For CI/CD pipelines, Testcontainers is recommended as it provides:
- Isolated test environments
- Automatic cleanup
- Parallel test execution support
- No manual infrastructure setup

Example GitHub Actions workflow:
```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Integration Tests
        run: dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj
```

## Future Enhancements

- [ ] Add E2E tests that verify complete message flow
- [ ] Add performance tests for database operations
- [ ] Add chaos testing (container failures, network issues)
- [ ] Add tests for Infrastructure health checks
- [ ] Add tests for retry mechanisms
- [ ] Add tests for cache invalidation strategies
