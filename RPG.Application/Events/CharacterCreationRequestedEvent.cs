using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;

namespace RPG.Application.Events;

public record CharacterCreateRequestedEvent(EventMetadata Meta, Character Character) : IGameEventWithMetadata
{ public object? Payload => new { Character = Character }; public string? PayloadType => "CharacterCreateRequested"; }

