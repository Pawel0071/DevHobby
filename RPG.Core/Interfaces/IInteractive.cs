namespace RPG.Core.Interfaces;

public interface IInteractive
{
    string InteractionType { get; }
    string Interact(string initiatorId);
}