namespace RPG.Core.Services.EquipmentService;

public enum EquipmentError
{
    None,
    ItemCannotBeEquip,
    InvalidOperation
}

public record EquipmentResult(bool Success, EquipmentError Result, string? Message = null)
{
    public static EquipmentResult Ok() => new(true, EquipmentError.None);
    public static EquipmentResult Fail(EquipmentError result, string? message = null) => new(false, result, message);
}