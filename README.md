docker compose up --build
# DevHobby

DevHobby is a multi-service RPG platform built with .NET 8. The solution contains a gRPC game server, domain/application layers, background workers, infrastructure plumbing, a CLI toolkit, automated tests, and a lightweight semi-graphical desktop client that talks to the live server.

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
- `RPG.Application/` – application services, command handlers, diagnostics
- `RPG.Infrastructure/` – MongoDB/Redis/RabbitMQ integration, DI setup, outbox, health checks
- `RPG.GameServer/` – ASP.NET Core gRPC host exposing the RPG services
- `RPG.PersistenceService/` – background worker for persistence/outbox dispatching
- `RedisWormUp/`, `CricuitBraker/` – auxiliary workers (cache warm-up, circuit breaker)
- `RPG.CLI/` – command-line entry point (functional scenarios, gRPC helpers)
- `RPG.IntegrationTests/`, `RPG.UnitTest/` – automated test suites
- `RPG.DesktopClient/` – semi-graphical console client for quick gameplay smoke tests
- `observability/` – Grafana/Tempo/Prometheus/Loki provisioning

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
- Unit tests live in `RPG.UnitTest/` (organised by module).
- Run all unit tests: `dotnet test RPG.UnitTest/RPG.UnitTest.csproj`
- Integration tests (`RPG.IntegrationTests/`) use Testcontainers for Mongo, Redis, RabbitMQ.
- CLI document scenarios (full CRUD path through Mongo/Redis): `dotnet run --project RPG.CLI -- document-tests`

## Observability Stack
- Compose provisions Prometheus, Loki, Tempo, Grafana.
- Dashboard provisioning file: `observability/grafana/provisioning/dashboards/devhobby-observability-overview.json`.
- Key panels:
  - Recent gRPC activities: `RPG.GameServer`
  - New panels for `RPG.Application` and `RPG.Core` ActivitySource traces
  - MongoDB, Redis, RabbitMQ health/metrics
- Activity sources are emitted from both Application and Core layers (movement handlers, services).

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
- Missing project references → verify `DevHobby.sln` entries and `ProjectReference` nodes.
- Docker compose issues → ensure Dockerfile paths exist, ports 27017/6379/5672/15672 are free.
- gRPC connection failures from clients → confirm `RPG.GameServer` is running and reachable (`http://localhost:5124`).
- Observability dashboards empty → check Tempo service logs; ensure ActivitySources are registered via OpenTelemetry setup.
- Unit tests failing with external dependency errors → run `docker compose up -d` or use Testcontainers fixtures (integration tests handle setup automatically).

---

Questions or ideas for additional documentation (environment variables, seed data, advanced observability) are welcome—feel free to open an issue or PR.
📝 **Szczegółowa dokumentacja zmian w Infrastructure:** zobacz `INFRASTRUCTURE_CHANGES.md`

