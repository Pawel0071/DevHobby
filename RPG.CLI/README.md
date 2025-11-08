# RPG CLI

Command-line companion for DevHobby services. It can execute domain commands and end-to-end functional checks that exercise the live infrastructure stack.

## Prerequisites

- .NET SDK 8.0 or newer
- Docker and Docker Compose (for the backing services)

## Start the infrastructure stack

```bash
docker compose up -d
```

This brings up MongoDB, Redis, RabbitMQ, and the worker services defined in `compose.yaml`. Leave the stack running while you use the CLI.

## Run the CLI

From the repository root:

```bash
dotnet run --project RPG.CLI -- <command> [options]
```

Alternatively, change directory into `RPG.CLI` and run:

```bash
dotnet run -- <command> [options]
```

## Available commands

### functional-tests

Executes the end-to-end pipeline using the sample payload in `Samples/item.json`. Requires the infrastructure stack to be running.

```bash
dotnet run --project RPG.CLI -- functional-tests --sample Samples/item.json
```

Successful execution verifies:

- Item upsert through `DocumentRepository`
- Persistence to MongoDB via the `MessageHandler`
- Redis cache warm-up using the real infrastructure repositories

### equip

Sends an "equip" command to the application layer. Use `--help` on the command for usage details.

```bash
dotnet run --project RPG.CLI -- equip --help
```

## Stopping services

When finished, tear down the infrastructure stack to free resources:

```bash
docker compose down
```
