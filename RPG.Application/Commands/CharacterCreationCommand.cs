using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;
using RPG.Domain.Models;

namespace RPG.Application.Commands;

public record CreateCharacterCommand(Character Character) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}
