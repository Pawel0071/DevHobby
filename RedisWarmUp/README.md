# RedisWarmUp

The **RedisWarmUp** project is a one-shot console host that preloads Redis with every document available in MongoDB. It ensures the cache mirrors the persistent store before latency-sensitive services (like the GameServer) start serving traffic.

## What It Does

- Boots the full DevHobby infrastructure stack (MongoDB, Redis, RabbitMQ logging) via `InfrastructureRegistration`.
- Discovers every document type declared in `DocumentMappingRegistry` and creates a warm-up strategy for it.
- Streams all documents for each collection from MongoDB and writes them to Redis using `IRedisDocumentRepository`.
- Emits structured progress logs so orchestration tools can monitor bootstrap status.

## Execution Flow

1. Configuration is loaded from `appsettings.json` plus shared infrastructure settings.
2. `AddInfrastructure` registers MongoDB, Redis, logging, and repository abstractions.
3. For each mapping, a `DocumentWarmUpStrategy<TDocument>` is registered.
4. `RedisWarmUpService.ExecuteAsync()` iterates strategies:
   - Reads every document through `IMongoDocumentRepository`.
   - Writes each document to Redis and reports counts.
5. Service exits with exit code `0` when warm-up succeeds, or `1` when an error is thrown.

Run locally with:

```bash
cd RedisWarmUp
dotnet run
```

The service finishes after the cache has been hydrated.

## Configuration

Key settings live in `appsettings.json` and defer to shared files in `RPG.Infrastructure`:

- `ConnectionStrings:Mongo` – MongoDB endpoint.
- `ConnectionStrings:Redis` – Redis instance used as the cache target.
- Logging configuration is inherited from `appsettings.infrastructure.json` (Serilog sinks, levels).

Override these values via environment-specific settings or environment variables when running in containers.

## Testing

| Scope | Command | Notes |
| --- | --- | --- |
| Unit tests (warm-up strategy) | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter DocumentWarmUpStrategyTests` | Verifies batching, cancellation, and Redis writes for `DocumentWarmUpStrategy`. |
| Integration (Mongo ↔ Redis bootstrap) | `dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj --filter RedisWarmUpIntegrationTests` | Runs end-to-end warm-up against Testcontainers (requires Docker). |

Always run the integration suite after changing repository contracts or warm-up logic to confirm compatibility with real dependencies.

## Related Components

- `RPG.Infrastructure` supplies `DocumentMappingRegistry`, repositories, logging, and health checks used here.
- `RPG.PersistenceService` publishes RabbitMQ events that keep MongoDB in sync, allowing RedisWarmUp to work with current data.
- GameServer and UI nodes read from Redis to serve low-latency responses once warm-up completes.
