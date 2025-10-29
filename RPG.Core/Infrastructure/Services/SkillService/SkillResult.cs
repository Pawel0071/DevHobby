namespace RPG.Core.Interfaces;

public enum SkillError
{
    None,
    InvalidOperation
}

public record SkillResult(bool Success, SkillError Result, string? Message = null)
{
    public static SkillResult Ok() => new(true, SkillError.None);
    public static SkillResult Fail(SkillError result, string? message = null) => new(false, result, message);
}