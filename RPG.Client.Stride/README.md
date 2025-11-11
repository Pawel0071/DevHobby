# RPG.Client.Stride

This project experiments with rendering the DevHobby RPG world using [MonoGame](https://www.monogame.net/). It currently opens a desktop window and draws a rotating, lit cube rendered with `BasicEffect`. The goal is to provide a bridge toward a fully featured 3D client while keeping the stack pure C#.

## Prerequisites

- .NET 8 SDK
- MonoGame Content Builder (optional for future asset compilation). Install via `dotnet tool install -g dotnet-mgcb`.
- A desktop GPU with the latest drivers; MonoGame DesktopGL uses OpenGL by default.

## Running

```bash
# Restore packages and run the MonoGame sample client
cd RPG.Client.Stride
dotnet run
```

The window should display a lit, rotating cube.

## Next Steps

1. Export shared gRPC contracts (e.g. `RPG.GameServer/Protos/*.proto`) into this project and regenerate Unity-compatible C# stubs using `Grpc.Tools`.
2. Replace the temporary cube with meshes loaded from the MonoGame content pipeline and driven by snapshot data from the game server.
3. Implement an input system that issues `StartMovement`/`StopMovement` commands and applies client-side prediction.
4. Build a UI layer (HUD, chat, inventory) using MonoGame's SpriteBatch or a UI framework such as Myra.
5. Integrate the MonoGame content pipeline to manage textures, models, animations, and particle effects.

Document any additional setup steps in this file as the client evolves.
