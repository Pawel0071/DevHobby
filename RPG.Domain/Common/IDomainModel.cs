namespace RPG.Domain.Common;

/// <summary>
///     Base interface for all domain entities
/// </summary>
public interface IDomainModel
{
    /// <summary>
    ///     Unique identifier of the entity
    /// </summary>
    Guid Id { get; }
}
