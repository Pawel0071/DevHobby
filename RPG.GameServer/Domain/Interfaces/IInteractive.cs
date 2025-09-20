namespace RPG.GameServer.Interfaces;

public interface IInteractive
{
    string InteractionType { get; }
    string Interact(string initiatorId);
}