namespace RPG.Application.Queries;

public sealed class LocationReadDto
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public string? WorldId { get; init; }
    public string MapId { get; init; } = string.Empty;
    public string ZoneName { get; init; } = string.Empty;
    public float Rotation { get; init; }

    public static LocationReadDto FromDomain(RPG.Domain.Models.Location loc) => new()
    {
        X = loc?.Position.X ?? 0f,
        Y = loc?.Position.Y ?? 0f,
        Z = loc?.Position.Z ?? 0f,
        WorldId = loc?.WorldId.ToString(),
        MapId = loc?.MapId ?? string.Empty,
        ZoneName = loc?.MapName ?? string.Empty,
        Rotation = loc?.Direction ?? 0f
    };
}

