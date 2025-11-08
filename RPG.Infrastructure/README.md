# RPG.Infrastructure

The **RPG.Infrastructure** project centralises integrations with the platform services that power persistence, caching, messaging, and warm-up workflows for the DevHobby solution. It wires external systems (MongoDB, Redis, RabbitMQ) into a unified document pipeline that other services can consume through domain-facing abstractions.

## What It Provides

- **Service wiring** via `InfrastructureRegistration` for MongoDB, Redis, RabbitMQ, logging, and health checks.
- **Document orchestration** with repositories that translate between domain entities and storage-specific documents.
- **Dictionary caching** that boots common lookups from MongoDB into in-memory registries at startup.
- **Cross-cutting helpers** such as document mapping, item tag registries, and activity scopes shared across services.
- **Operational visibility** through health checks and Serilog-based structured logging.

## Core Building Blocks

- **MongoDocumentRepository**: low-level CRUD access to MongoDB collections by document type.
- **RedisDocumentRepository**: high-speed cache layer used for reads and transient storage.
- **DocumentRepository & Handler**: orchestrate Mongo, Redis, and RabbitMQ for cohesive entity flows.
- **DocumentMappingRegistry & DocumentTypeResolver**: map domain entities to Mongo documents and mappers.
- **DictionaryRepository & Registry**: preload static dictionaries (item tags, error codes, item types) into memory.
- **SerilogWrapper**: project-wide ILogger adapter backed by Serilog.
- **Health Checks**: verify MongoDB, Redis, and RabbitMQ connectivity at runtime.

## Document Flow

1. **Upsert**
   - Domain entity arrives at `IDocumentRepository`.
   - `DocumentRepositoryHandler` converts it into a Mongo document.
   - Document is cached in Redis, then an `entity.upserted` event is sent to RabbitMQ.

2. **Read**
   - Handler first attempts a Redis fetch for the document.
   - On a miss, MongoDB is queried and the result is back-filled into Redis before returning.

3. **Delete**
   - Handler resolves the document (Redis, then MongoDB fallback).
   - Redis copy is removed and an `entity.deleted` event is published.

4. **Warm-up Dictionaries**
   - `DictionaryRepository` pulls reference data from MongoDB.
   - `DictionaryRegistry` stores results in-process for low-latency lookups.

## Configuration

Key configuration resides in `appsettings.infrastructure.json` (overridden per environment):

- `ConnectionStrings:Mongo` – MongoDB connection string (database name defaults to `rpg`).
- `ConnectionStrings:Redis` – Redis connection string.
- `RabbitMQ` section – host, credentials, and vhost. When absent, a null-publisher is registered.
- `Serilog` section – logging sinks and enrichers consumed by `SerilogWrapper`.

Health checks are automatically registered under the names `mongo`, `redis`, and `rabbitmq`.

## Testing

| Scope | Command | Notes |
| --- | --- | --- |
| Unit tests (Mongo repository) | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter MongoDocumentRepositoryTests` | Covers Mongo CRUD orchestration and logging paths using in-memory cursors. |
| Integration (Mongo/Redis/RabbitMQ) | `dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj` | Uses Testcontainers to exercise the full infrastructure stack (Mongo, Redis, RabbitMQ warm-up). |
| Redis warm-up unit tests | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj --filter DocumentWarmUpStrategyTests` | Exercises dictionary warm-up strategies that seed Redis via this infrastructure. |

Run tests from the repository root. Ensure Docker is available before executing integration tests because they launch containerised dependencies.

## Related Services

- **RedisWarmUp**: leverages `DictionaryWarmupService` to preload Redis using this infrastructure layer.
- **PersistenceService**: background service that executes warm-up and maintenance tasks with the same repositories.
- **GameServer/UI**: consume `IDocumentRepository` abstractions to stay decoupled from storage details.

Use `InfrastructureRegistration.AddInfrastructure` during host configuration to pull the entire wiring into any .NET application within the solution.
