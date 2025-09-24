using System.Text.Json;
using System.Text.Json.Serialization;
using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Interfaces;

namespace RPG.Core.Domain.Factories;

public class CharacterFactory : ICharacterFactory
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public T CreateCharacter<T>(string characterJson) where T : BaseCharacter
    {
        var character = JsonSerializer.Deserialize<T>(characterJson, _jsonOptions);
        if (character == null)
        {
            throw new InvalidOperationException("Failed to deserialize character JSON.");
        }

        // Populate required properties if not set by JSON
        character.Id ??= Guid.NewGuid().ToString();
        character.Stats ??= new Stats();
        character.Position ??= new Location();

        return character;
    }
}