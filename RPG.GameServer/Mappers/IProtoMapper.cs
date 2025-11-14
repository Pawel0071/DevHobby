namespace RPG.GameServer.Mappers;

/// <summary>
/// Interface for mapping between domain models and proto messages
/// </summary>
/// <typeparam name="TDomain">Domain model type</typeparam>
/// <typeparam name="TProto">Proto message type</typeparam>
public interface IProtoMapper<TDomain, TProto>
{
    /// <summary>
    /// Converts domain model to proto message
    /// </summary>
    TProto ToProto(TDomain domain);

    /// <summary>
    /// Converts proto message to domain model
    /// </summary>
    TDomain ToDomain(TProto proto);
}

