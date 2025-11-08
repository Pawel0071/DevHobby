using System.CommandLine;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer.Protos;
using CharacterServiceClient = RPG.GameServer.Protos.CharacterService.CharacterServiceClient;
using ProtoCharacterClass = RPG.GameServer.Protos.CharacterClass;
using ProtoLocation = RPG.GameServer.Protos.Location;

namespace RPG.CLI.Commands;

public class CharacterGrpcCommand
{
    private readonly IServiceProvider _provider;

    public CharacterGrpcCommand(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Command Build()
    {
        var root = new Command("character", "Komendy gRPC dla postaci");

        root.AddCommand(BuildCreate());
        root.AddCommand(BuildStartMovement());
        root.AddCommand(BuildStopMovement());
        root.AddCommand(BuildStartRotation());
        root.AddCommand(BuildStopRotation());

        return root;
    }

    private Command BuildCreate()
    {
        var nameOption = new Option<string>("--name", "Nazwa postaci") { IsRequired = true };
    var classOption = new Option<ProtoCharacterClass>("--class", () => ProtoCharacterClass.Warrior, "Klasa postaci");
        var levelOption = new Option<int>("--level", () => 1, "Poziom postaci");

        var cmd = new Command("create", "Tworzy postać poprzez gRPC")
        {
            nameOption,
            classOption,
            levelOption
        };

    cmd.SetHandler(async (string name, ProtoCharacterClass clazz, int level) =>
        {
            using var scope = _provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<CharacterServiceClient>();

            var characterId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();

            var request = new CharacterRequest
            {
                Character = new PlayerCharacter
                {
                    SessionId = sessionId.ToString(),
                    CharacterClass = clazz,
                    BaseCharacter = new BaseCharacter
                    {
                        Id = characterId.ToString(),
                        Name = name,
                        Level = level,
                        MaxHealth = 100,
                        CurrentHealth = 100,
                        MaxMana = 50,
                        CurrentMana = 50,
                        Stats = new Stats
                        {
                            MoveSpeed = 5,
                            Strength = 10,
                            Vitality = 10
                        },
                        Position = new ProtoLocation
                        {
                            X = 0,
                            Y = 0,
                            Z = 0
                        },
                        IsMoving = false,
                        IsRotating = false,
                        Rotation = 0
                    }
                }
            };

            request.Character.BaseCharacter.Position ??= new ProtoLocation();

            try
            {
                Console.WriteLine($"[CLI] Calling CharacterService.CreateCharacter for '{name}' at {DateTime.UtcNow:O}");
                var reply = await client.CreateCharacterAsync(request);
                Console.WriteLine($"[CLI] CharacterService.CreateCharacter responded with id {reply.CharacterId}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"[CLI] gRPC error from CreateCharacter: {ex.Status}");
            }
    }, nameOption, classOption, levelOption);

        return cmd;
    }

    private Command BuildStartMovement()
    {
        var characterOption = new Option<Guid>("--character", "Identyfikator postaci") { IsRequired = true };
        var directionOption = new Option<int>("--direction", "Kierunek ruchu (1-8)") { IsRequired = true };

        var cmd = new Command("move-start", "Rozpoczyna ruch postaci")
        {
            characterOption,
            directionOption
        };

        cmd.SetHandler(async (Guid characterId, int direction) =>
        {
            using var scope = _provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<CharacterServiceClient>();

            try
            {
                Console.WriteLine($"[CLI] Calling CharacterService.StartMovement for {characterId} direction {direction} at {DateTime.UtcNow:O}");
                var reply = await client.StartMovementAsync(new MovementCommandRequest
                {
                    CharacterId = characterId.ToString(),
                    Direction = direction
                });

                Console.WriteLine(reply.Success
                    ? "[CLI] Movement started."
                    : $"[CLI] Failed to start movement: {reply.ErrorCode} {reply.Message}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"[CLI] gRPC error from StartMovement: {ex.Status}");
            }
        }, characterOption, directionOption);

        return cmd;
    }

    private Command BuildStopMovement()
    {
        var characterOption = new Option<Guid>("--character", "Identyfikator postaci") { IsRequired = true };

        var cmd = new Command("move-stop", "Zatrzymuje ruch postaci")
        {
            characterOption
        };

        cmd.SetHandler(async (Guid characterId) =>
        {
            using var scope = _provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<CharacterServiceClient>();

            try
            {
                Console.WriteLine($"[CLI] Calling CharacterService.StopMovement for {characterId} at {DateTime.UtcNow:O}");
                var reply = await client.StopMovementAsync(new CharacterIdRequest { CharacterId = characterId.ToString() });
                Console.WriteLine(reply.Success
                    ? "[CLI] Movement stopped."
                    : $"[CLI] Failed to stop movement: {reply.ErrorCode} {reply.Message}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"[CLI] gRPC error from StopMovement: {ex.Status}");
            }
        }, characterOption);

        return cmd;
    }

    private Command BuildStartRotation()
    {
        var characterOption = new Option<Guid>("--character", "Identyfikator postaci") { IsRequired = true };
        var directionOption = new Option<int>("--direction", "Kierunek rotacji (1-8)") { IsRequired = true };

        var cmd = new Command("rotate-start", "Rozpoczyna rotację postaci")
        {
            characterOption,
            directionOption
        };

        cmd.SetHandler(async (Guid characterId, int direction) =>
        {
            using var scope = _provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<CharacterServiceClient>();

            try
            {
                Console.WriteLine($"[CLI] Calling CharacterService.StartRotation for {characterId} direction {direction} at {DateTime.UtcNow:O}");
                var reply = await client.StartRotationAsync(new MovementCommandRequest
                {
                    CharacterId = characterId.ToString(),
                    Direction = direction
                });

                Console.WriteLine(reply.Success
                    ? "[CLI] Rotation started."
                    : $"[CLI] Failed to start rotation: {reply.ErrorCode} {reply.Message}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"[CLI] gRPC error from StartRotation: {ex.Status}");
            }
        }, characterOption, directionOption);

        return cmd;
    }

    private Command BuildStopRotation()
    {
        var characterOption = new Option<Guid>("--character", "Identyfikator postaci") { IsRequired = true };

        var cmd = new Command("rotate-stop", "Zatrzymuje rotację postaci")
        {
            characterOption
        };

        cmd.SetHandler(async (Guid characterId) =>
        {
            using var scope = _provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<CharacterServiceClient>();

            try
            {
                Console.WriteLine($"[CLI] Calling CharacterService.StopRotation for {characterId} at {DateTime.UtcNow:O}");
                var reply = await client.StopRotationAsync(new CharacterIdRequest { CharacterId = characterId.ToString() });
                Console.WriteLine(reply.Success
                    ? "[CLI] Rotation stopped."
                    : $"[CLI] Failed to stop rotation: {reply.ErrorCode} {reply.Message}");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"[CLI] gRPC error from StopRotation: {ex.Status}");
            }
        }, characterOption);

        return cmd;
    }
}
