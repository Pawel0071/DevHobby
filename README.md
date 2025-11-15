docker compose up --build
# DevHobby

DevHobby is a multi-service RPG platform built with .NET 8. The solution contains a gRPC game server, domain/application layers, background workers, infrastructure plumbing, a CLI toolkit, automated tests, and a lightweight semi-graphical desktop client that talks to the live server.

> TL;DR to boot everything locally:
>
> ```bash
> docker compose up --build
> dotnet build DevHobby.sln
> dotnet run --project RPG.GameServer/RPG.GameServer.csproj
> ```

## Table of Contents
- Architecture
- Prerequisites
- Quick Start
- Local Development Workflows
- Testing
- Observability Stack
- gRPC & Protobuf
- Configuration & Logging
- External Integrations
- Continuous Integration
- Extending the Solution
- Troubleshooting

## Architecture

- `RPG.Core/` – shared domain entities, interfaces, diagnostics, and cross-cutting services
- `RPG.Domain/` – domain logic (aggregates, value objects, enums)
  - `Models/Npcs/NpcComponents/AiComponent.cs` – AI behavior profile configuration (patrol, aggro, detection ranges)
- `RPG.Application/` – application services, command handlers, diagnostics
  - `Events/NpcEvents.cs` – NPC AI requested events (move, skill, engage, idle, return to spawn)
  - `Events/Handlers/NpcRequestedHandlers.cs` – handlers for NPC AI events that update repository & broadcast deltas
- `RPG.Application/Infrastructure/RequestedEventOrchestrator.cs` – single dispatcher that matches each `*RequestedEvent` to a dedicated `IRequestedEventHandler`
- `RPG.Application/Managers/SessionManager.cs` – authoritative session store used by the GameServer for Create/Heartbeat/End validation
- `RPG.AI/` – Utility AI system for NPCs
  - `Core/AiContext.cs` – runtime snapshot for AI decisions (health, threat, blackboard, directives)
  - `Utility/Actions/UtilityActionCatalog.cs` – pre-built AI behaviors (Patrol, Attack, Flee, etc.)
  - `Utility/UtilityAgent.cs` – decision engine that evaluates actions based on context
- `RPG.Core/Services/NpcServices/NpcAiService.cs` – NPC AI tick loop that:
  1. Builds AiContext from NPC state + nearby players
  2. Runs UtilityAgent.Decide() to get AI directives
  3. Converts directives to *RequestedEvents (NpcMoveRequested, NpcIdleRequested, etc.)
  4. Enqueues events to RequestedEventQueue for chronological processing
- `RPG.Infrastructure/` – MongoDB/Redis/RabbitMQ integration, DI setup, outbox, health checks, GameDelta persistence helpers
- `RPG.GameServer/` – ASP.NET Core gRPC host exposing the RPG services
  - `Controllers/*ServiceImpl.cs` map Proto → Domain → Command/Query buses (no business logic inside controllers)
  - `Mappers/*ProtoMapper.cs` mirror `RPG.Infrastructure.Mappers` to include every domain component/tag in proto responses
  - `Services/GameStateBroadcastAdapter` + `GameDeltaBuffer` push world-state deltas to connected sessions
- `RPG.PersistenceService/` – background worker for persistence/outbox dispatching
- `RedisWormUp/`, `CricuitBraker/` – auxiliary workers (cache warm-up, circuit breaker)
- `RPG.CLI/` – command-line entry point (functional scenarios, gRPC helpers)
- `RPG.IntegrationTests/`, `RPG.UnitTest/` – automated test suites
- `RPG.DesktopClient/` – semi-graphical console client for quick gameplay smoke tests
- `observability/` – Grafana/Tempo/Prometheus/Loki provisioning

### NPC AI → Event Flow

```
NpcAiService.TickAsync()
  ↓
1. Build AiContext (from Npc + nearby players)
  ↓
2. UtilityAgent.Decide(context) → AiDirective[]
  ↓
3. Convert directives to *RequestedEvents:
   - AiDirectiveType.MoveToLocation → NpcMoveRequestedEvent
   - AiDirectiveType.Idle → NpcIdleRequestedEvent
   - AiDirectiveType.UseSkill → NpcSkillUseRequestedEvent
  ↓
4. Enqueue to RequestedEventQueue
  ↓
5. GameEventDispatcher processes events chronologically:
   - NpcMovementRequestedHandler updates Npc.CurrentLocation
   - Persists to MongoDB
   - Broadcasts NpcDelta to all clients via GameStateBroadcaster
```

This ensures AI decisions are processed in the same event pipeline as player commands, maintaining consistent game state.

Root-level files of note:
- `DevHobby.sln` – Visual Studio / dotnet solution
- `compose.yaml` – docker-compose stack for MongoDB, Redis, RabbitMQ, observability, and workers
- `qodana.yaml`, `coverlet.runsettings` – tooling configuration

## Prerequisites
- .NET SDK 8.0 or newer
- Docker & Docker Compose (required for the full infrastructure stack)
- For observability dashboards: Grafana/Tempo/Prometheus/Loki (auto-provisioned by `compose.yaml`)

## Quick Start

```bash
# Restore & build everything
dotnet build DevHobby.sln

# Start infra & background services
docker compose up -d

# In a new shell start the game server (uses Mongo/Redis from compose)
ConnectionStrings__Mongo="mongodb://localhost:27017" \
ConnectionStrings__Redis="localhost:6379" \
dotnet run --project RPG.GameServer/RPG.GameServer.csproj

# Run the semi-graphical desktop client (creates a character and lets you move it)
dotnet run --project RPG.DesktopClient/RPG.DesktopClient.csproj

# Run document repository end-to-end scenarios via CLI (optional)
dotnet run --project RPG.CLI/RPG.CLI.csproj -- document-tests

# When finished
kill <gameserver-pid>

```

**Desktop client controls**
- Movement: `W/S/A/D` or arrow keys (hold to move; release to stop)
- Rotation: `Q` / `E` (hold to rotate; release to stop)
- Escape: quit the client (sends stop commands before exit)

## Local Development Workflows
- Build specific project: `dotnet build <path-to-csproj>`
- Run the gRPC server: `dotnet run --project RPG.GameServer`
- Run CLI commands:
  - Create a character via gRPC: `dotnet run --project RPG.CLI -- character create --name Hero`
  - Movement smoke test: `rpg.CLI -- character move-start --character <id> --direction 1`
- Launch desktop client to exercise movement/rotation visually.
- Observability dashboards become available once `docker compose up` completes (Grafana default credentials: `admin / 2019Venza`).

## Testing

| Scope | Command | Notes |
| --- | --- | --- |
| Solution build | `dotnet build DevHobby.sln` | Fails fast on API/contract breakages |
| Unit tests | `dotnet test RPG.UnitTest/RPG.UnitTest.csproj` | Covers CommandBus, requested handlers, GameDeltaBuffer, infrastructure adapters |
| Integration tests | `dotnet test RPG.IntegrationTests/RPG.IntegrationTests.csproj` | Spins Mongo/Redis/RabbitMQ/Testcontainers; includes gRPC session handshake helpers |
| CLI smoke | `dotnet run --project RPG.CLI -- document-tests` | Exercises persistence round-trips |

> **Session-aware tests** – Integration specs use shared helpers to call `SessionService.CreateSession` and attach the returned session id in gRPC metadata. When writing new tests that hit the GameServer, follow the same pattern to avoid `Unauthenticated` failures.

After code changes involving requested handlers or broadcasters, run unit + integration suites to confirm ordering guarantees and delta serialization.

## Observability Stack & Health Probes

- Compose provisions Prometheus, Loki, Tempo, Grafana (see `observability/` provisioning files).
- Activity sources:
  - `RPG.Application.Commands.CommandBus` emits spans for every command (`command.name`, `service`, `traceId`, `spanId`).
  - Requested handlers and core services add nested spans so Tempo can show end-to-end traces.
- Serilog enrichment automatically attaches `traceId/spanId` to console, file, and Loki sinks.
- Dashboards:
  - Per-service boards (GameServer, PersistenceService, CircuitBreaker) covering CPU/mem (cAdvisor), request rates, commands/events per minute, health history.
  - Latest Logs panels use the provisioned Loki datasource.
- Health endpoints exposed by `RPG.GameServer`:
  - `/health/live` – process is running
  - `/health/ready` – Mongo/Redis/RabbitMQ connectivity
  - `/metrics` – Prometheus scrape (ASP.NET + custom meters)
  - `/ping` – lightweight readiness probe for local smoke tests

Example checks:

```bash
curl http://localhost:5124/health/live
curl http://localhost:5124/health/ready
curl http://localhost:5124/ping
```

## gRPC & Protobuf
- Proto definitions: `RPG.GameServer/Protos/*.proto`
- Client stubs generated via `Grpc.Tools` (Desktop client and CLI reference them).
- After editing `.proto` files, rebuild to regenerate messages/services.

## Configuration & Logging
- Each project contains `appsettings.json` + `appsettings.Development.json`.
- DI bootstrapping is done in each `Program.cs` (see `AddInfrastructure`, `CoreRegistration`, etc.).
- Logging uses `ILogger<T>` with structured logging (Serilog-compatible sinks can be wired through configuration).

## External Integrations
- **MongoDB** – persistence repositories registered in `RPG.Infrastructure` (accessed by application/core layers).
- **Redis** – caching abstractions, warmed through `RedisWormUp` worker.
- **RabbitMQ** – messaging via outbox/publisher pattern (`OutboxDispatcher`, `NullRabbitPublisher`).
- Health checks for all three are exposed by infrastructure components; map `/health` endpoint in hosting projects if needed.

## Continuous Integration
- Workflow: `.github/workflows/ci.yml`
- Steps: checkout → setup .NET 8 → restore → build (Release) → test (Release)
- Add NuGet caching as needed (example snippet in workflow).

## Extending the Solution
1. Create a new project directory + `.csproj`.
2. Add to solution: `dotnet sln DevHobby.sln add <path>`
3. Follow existing structure (`Program.cs`, DI, appsettings, Dockerfile if needed).
4. Register interfaces in Core/Application/Infrastructure as appropriate.
5. Add unit/integration tests.
6. Update compose or tooling if the service requires infrastructure.

When modifying shared entities in `RPG.Core`:
1. Update the contracts/entities.
2. Rebuild the solution; run unit + integration tests.
3. Confirm downstream services compile (GameServer, CLI, workers).

## Troubleshooting
- Missing session metadata → call `SessionService.CreateSession`, store the GUID, and send it as `x-session-id` header for every gRPC call (CLI/Desktop already do this).
- Missing project references → verify `DevHobby.sln` entries and `ProjectReference` nodes.
- Docker compose issues → ensure Dockerfile paths exist, ports 27017/6379/5672/15672 are free.
- gRPC connection failures from clients → confirm `RPG.GameServer` is running and reachable (`http://localhost:5124`).
- Observability dashboards empty → check Tempo service logs; ensure ActivitySources are registered via OpenTelemetry setup.
- Unit tests failing with external dependency errors → run `docker compose up -d` or use Testcontainers fixtures (integration tests handle setup automatically).

---

Questions or ideas for additional documentation (environment variables, seed data, advanced observability) are welcome—feel free to open an issue or PR.
📝 **Szczegółowa dokumentacja zmian w Infrastructure:** zobacz `INFRASTRUCTURE_CHANGES.md`
