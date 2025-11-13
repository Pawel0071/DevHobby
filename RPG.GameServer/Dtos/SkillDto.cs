// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Dtos/SkillDto.cs
namespace RPG.GameServer.Dtos;

public sealed class SkillDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

