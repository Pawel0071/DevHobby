namespace RPG.Core.Infrastructure.Services.LevelSerivice;

public enum LevelingError
{
    None,
    NotEnoughExperience,
    AlreadyMaxLevel
}

public record LevelingResult(bool Success, LevelingError Error, string? Message = null)
{
    public static LevelingResult Ok() => new(true, LevelingError.None);
    public static LevelingResult Fail(LevelingError error, string? message = null) => new(false, error, message);
}