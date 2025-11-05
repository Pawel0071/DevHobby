namespace RPG.Core.Application.Handlers;


public enum CommandError
{
    None,
    InvalidOperation,
    ItemNotFound,
    InventoryFull,
    LevelToLow,
    ItemNotHaveStatsDef
}

public record CommandResult(bool Success, CommandError Result, string? Message = null, object? InnerResult = null)
{
    public static CommandResult Ok() => new(true, CommandError.None);
    public static CommandResult Fail(CommandError result, string? message = null, object? innerResult = null) => new(false, result, message, innerResult);
}