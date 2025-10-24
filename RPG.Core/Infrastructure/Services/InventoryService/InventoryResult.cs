namespace RPG.Core.Infrastructure.Services.InventoryService;

public enum InventoryError
{
    None,
    NoFreeSlot,
    StackLimitReached,
    ItemNotFound,
    InvalidOperation
}

public record InventoryResult(bool Success, InventoryError Result, string? Message = null)
{
    public static InventoryResult Ok() => new(true, InventoryError.None);
    public static InventoryResult Fail(InventoryError result, string? message = null) => new(false, result, message);
}