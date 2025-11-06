# DevHobby

A multi-project .NET solution containing an RPG server, core/domain libraries, workers and utilities.

## Repository layout

- `RPG.Core/` - Core domain entities and shared types used across the solution.
- `RPG.Domain/` - Domain-specific models and logic (domain layer).
- `RPG.Application/` - Application services and use-cases.
- `RPG.Infrastructure/` - Infrastructure implementations (DB, external integrations).
- `RPG.GameServer/` - ASP.NET / gRPC game server and controllers (main server).
- `RPG.PersistenceService/` - Background worker for persistence tasks.
- `RPG.UI/` - UI project (client / front-end pieces).
- `RedisWormUp/`, `CricuitBraker/` - Worker utilities used by the solution.
- `RPG.CLI/` - Command-line utilities.
- `RPG.UnitTest/` - Unit and integration tests.
- `RPG.PersistenceService/` - (alternate persistence worker; check naming if duplicates exist)

Files you will commonly use:
- `DevHobby.sln` — Visual Studio / dotnet solution file.
- `compose.yaml` — docker-compose for local multi-service runs (if present/used).

## Requirements

- .NET SDK 8.0+ (the projects target `net8.0`).
- Optional: Docker and Docker Compose for running containers.

## Quick build & run

Build the whole solution:

```bash
dotnet build DevHobby.sln
```

Run a single service (example: GameServer):

```bash
cd RPG.GameServer
dotnet run
```

If you prefer to run multiple services using Docker/compose (if `compose.yaml` is configured):

```bash
docker compose up --build
```

## Protobuf / gRPC notes

- Protobuf files are under `RPG.GameServer/Protos/` and compiled by the project when building.
- If you update `.proto` files, rebuild the solution to regenerate the C# classes used by gRPC services.

## MongoDB / Data seeding

- For seeding sample items (example `Item` documents), use `mongoimport` with a JSON array file:

```bash
# create items.json with an array of item documents
mongoimport --uri "mongodb+srv://<username>:<password>@<cluster>/<db>" --collection items --file items.json --jsonArray
```

## Running tests

```bash
dotnet test RPG.UnitTest/RPG.UnitTest.csproj
```

## Common troubleshooting

- If `dotnet build` fails with missing project file errors, confirm that each `.csproj` path in `DevHobby.sln` matches the actual folder structure.
- If you see type/namespace errors in `RPG.Core` or other projects, check `ProjectReference` entries in each `.csproj` and ensure referenced projects exist and build.
- When adding a new project, add its `.csproj` to the solution with `dotnet sln add <path-to-csproj>` or edit `DevHobby.sln` carefully.

## Suggested next steps

- Run `dotnet build DevHobby.sln` and fix any compile errors; they will point to missing files or incorrect references.
- If you want, I can validate all `ProjectReference` paths and list any missing .csproj files or broken references.

## Contact / notes

If you want this README expanded with environment variables, per-service run instructions, or a diagram of the architecture, tell me which parts to expand and I will update it.

