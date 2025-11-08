# RPG.PersistenceService

The **RPG.PersistenceService** bridges the event stream coming from RabbitMQ with MongoDB persistence. It guarantees that cache-first services and warm-up jobs always operate on the latest data written by gameplay components.

## Responsibilities

- **Infrastructure bootstrapping** via `AddInfrastructure`, giving the service access to MongoDB, Redis, RabbitMQ, and logging.
- **Strategy registration**: for every mapping in `DocumentMappingRegistry`, a `DocumentPersistenceStrategy<TDocument>` is created to perform collection-specific CRUD.
- **Message handling**: `RabbitMqToMongoService` subscribes to RabbitMQ, and `MessageHandler` deserialises payloads, routes them to the correct strategy, and executes upsert/delete operations.
- **Routing resolution** thanks to `DocumentTypeMapper`, which converts routing keys (e.g., `player.upserted`) to collection metadata.

## Runtime Flow

1. Host configuration loads layered settings (`appsettings.json`, `RPG.Infrastructure/appsettings.infrastructure.json`, `RPG.Core/appsettings.core.json`, `RPG.Application/appsettings.application.json`).
2. `AddInfrastructure` registers external connections and repository abstractions.
3. For each mapping, the service builds a persistence strategy that targets the mapped Mongo collection.
4. `RabbitMqToMongoService.StartListeningAsync()`:
   - Subscribes to RabbitMQ via `IRabbitMqConsumer`.
   - Sets `MessageHandler.HandleMessageAsync` as the callback.
5. `MessageHandler` receives messages:
   - Resolves collection + operation (`created/updated` → upsert, `deleted` → delete).
   - Deserialises to the mapped `IMongoDocument` type.
   - Executes the matching strategy (`UpsertAsync` or `DeleteAsync`).
6. Errors are logged and re-thrown to surface processing issues for alerting.

Run the service locally with:

```bash
cd RPG.PersistenceService
dotnet run
```

The process stays alive, continuously consuming messages until interrupted.

## Configuration

Important settings pulled from configuration files:

- `ConnectionStrings:Mongo` / `Redis` (infrastructure) – storage backends used by strategies and caches.
- `RabbitMQ` section – ensures the service can bind to exchanges for gameplay events.
- `Serilog` settings – propagate structured logging to console/files (inherited from infrastructure config).
- Application/domain configuration overlays (core + application) are available for additional feature-level toggles if required.

Override values with environment-specific `appsettings.{Environment}.json` files or environment variables when deploying.

## Testing

| Scope | Command | Notes |
| --- | --- | --- |
| Strategy unit tests | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter DocumentPersistenceStrategyTests` | Validates `DocumentPersistenceStrategy` upsert/delete behaviour. |
| Message handler unit tests | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter MessageHandlerTests` | Covers routing-key resolution, error handling, and strategy invocation. |
| Service wiring unit tests | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter RabbitMqToMongoServiceTests` | Ensures the background listener registers callbacks and starts consuming. |
| Integration (RabbitMQ connectivity) | `dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj --filter RabbitMqIntegrationTests` | Uses Testcontainers to validate broker availability and basic publish/consume flows. |

Re-run the integration tests whenever you change consumer configuration, routing keys, or document mappings to confirm compatibility with real infrastructure.

## Related Components

- **RedisWarmUp**: relies on MongoDB being up to date; this service keeps Mongo current by consuming RabbitMQ events.
- **RPG.Infrastructure**: supplies repository abstractions, logging, and health checks used here.
- **GameServer / UI**: emit events that this service consumes to keep persistence synchronised.
