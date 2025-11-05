using RPG.Domain.Common;
using RPG.Infrastructure;

namespace RPG.Core.Common;

public readonly record struct ServiceResult<TResult>(
    bool Success,
    ErrorCodeDefinition Error,
    TResult? Result = default,
    string? Message = null)
{
    public static ServiceResult<TResult> Ok(TResult result)
        => new(true, ErrorCodeDefinition.None, result);

    public static ServiceResult<TResult> Fail(ErrorCodeDefinition error, string? message = null)
        => new(false, error, default, message ?? error.Message);

    public static ServiceResult<TResult> FromException(Exception ex, ErrorCodeDefinition? fallback = null)
        => new(false, fallback ?? ErrorCodeDefinition.Unknown, default, ex.Message);
}