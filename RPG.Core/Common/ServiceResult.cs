using RPG.Domain.Common;

namespace RPG.Core.Common;

/// <summary>
/// Generic ServiceResult for operations that return data
/// </summary>
public readonly record struct ServiceResult<TResult>(
    bool Success,
    ErrorCodeDefinition Error,
    TResult? Result = default,
    string? Message = null)
{
    public static ServiceResult<TResult> Ok(TResult result)
    {
        return new ServiceResult<TResult>(true, ErrorCodeDefinition.None, result);
    }

    public static ServiceResult<TResult> Fail(ErrorCodeDefinition error, string? message = null)
    {
        return new ServiceResult<TResult>(false, error, default, message ?? error.Message);
    }

    public static ServiceResult<TResult> FromException(Exception ex, ErrorCodeDefinition? fallback = null)
    {
        return new ServiceResult<TResult>(false, fallback ?? ErrorCodeDefinition.Unknown, default, ex.Message);
    }
}
