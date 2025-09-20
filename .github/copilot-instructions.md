# AI Coding Agent Instructions for DevHobby

## Overview
DevHobby is a multi-service application with several distinct components, each serving a specific purpose. The project is organized into multiple directories, each representing a service or module. The architecture follows a microservices pattern, with services communicating through well-defined interfaces.

### Key Components
- **CricuitBraker**: Implements a worker service for managing circuit breaker patterns.
- **PersistenceService**: Handles data persistence and storage.
- **RedisWormUp**: Manages Redis-related operations.
- **RPG.Core**: Contains core domain entities, interfaces, and services for the RPG application.
- **RPG.GameServer**: Implements the game server logic, including domain, application, and infrastructure layers.
- **RPG.UI**: Provides the user interface for the RPG application.

## Developer Workflows

### Building the Project
Use the solution file `DevHobby.sln` to build all projects at once. Run the following command in the root directory:
```bash
# Build the solution
msbuild DevHobby.sln
```

### Running Services
Each service has its own `Program.cs` file and can be run independently. For example, to run the `RPG.GameServer`:
```bash
# Navigate to the service directory
cd RPG.GameServer

# Run the service
dotnet run
```

### Testing
Tests are not explicitly defined in the current structure. If tests are added, ensure they follow the naming convention `*Tests.cs` and are placed in a `Tests` directory within the respective service.

## Project-Specific Conventions

### Configuration
- Each service has its own `appsettings.json` and `appsettings.Development.json` for configuration.
- Use `launchSettings.json` in the `Properties` directory for debugging configurations.

### Dependency Injection
- Services are registered in `Program.cs` using the built-in .NET Dependency Injection framework.
- Follow the pattern of registering interfaces and their implementations in the `ConfigureServices` method.

### Logging
- Logging is configured in `appsettings.json`.
- Use the `ILogger` interface for structured logging.

## Integration Points

### External Dependencies
- **Redis**: Used by the `RedisWormUp` service.
- **Docker**: Each service has a `Dockerfile` for containerization.

### Cross-Service Communication
- Services communicate through shared interfaces and domain entities defined in `RPG.Core`.
- Ensure that changes to `RPG.Core` are backward-compatible to avoid breaking other services.

## Examples

### Adding a New Service
1. Create a new directory with the service name.
2. Add a `.csproj` file and include it in `DevHobby.sln`.
3. Follow the structure of existing services (e.g., `Program.cs`, `Worker.cs`, `appsettings.json`).

### Modifying a Shared Entity
1. Update the entity in `RPG.Core/Entity`.
2. Ensure all dependent services are updated and tested.

---

This document is a starting point. Update it as the project evolves to ensure AI agents remain effective contributors.