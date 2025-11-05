using RPG.Domain.Common;

namespace RPG.Core.Common;

public static class ServiceResultExtensions
{
    public static ServiceResult<T> ToResult<T>(this T value)
        => ServiceResult<T>.Ok(value);

    public static ServiceResult<T> ToFail<T>(this ErrorCodeDefinition error, string? message = null)
        => ServiceResult<T>.Fail(error, message);

    public static ServiceResult<T> ToFail<T>(this Exception ex, ErrorCodeDefinition? fallback = null)
        => ServiceResult<T>.FromException(ex, fallback);
}