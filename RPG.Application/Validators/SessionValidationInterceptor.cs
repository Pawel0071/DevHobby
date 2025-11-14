using Grpc.Core;
using Grpc.Core.Interceptors;
using RPG.Application.Managers;

namespace RPG.Application.Validators;

public class SessionValidationInterceptor : Interceptor
{
    private readonly ISessionManager _sessionManager;

    public SessionValidationInterceptor(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        // Bypass for session bootstrap/read endpoints
        if (IsBypassedMethod(context.Method))
        {
            return await continuation(request, context);
        }

        if (!TryResolveSessionIdFromHeaders(context.RequestHeaders, out var sessionId))
        {
            // Fallback: try read SessionId from request message (proto has string SessionId)
            if (!TryResolveSessionIdFromRequest(request, out sessionId))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid session id"));
            }
        }

        var session = await _sessionManager.GetAsync(sessionId, context.CancellationToken);
        if (session == null || !session.IsActive)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Inactive or missing session"));
        }

        return await continuation(request, context);
    }

    private static bool TryResolveSessionIdFromHeaders(Metadata headers, out Guid sessionId)
    {
        sessionId = default;
        var header = headers.FirstOrDefault(h => string.Equals(h.Key, "x-session-id", StringComparison.OrdinalIgnoreCase));
        return header != null && Guid.TryParse(header.Value, out sessionId);
    }

    private static bool TryResolveSessionIdFromRequest<TRequest>(TRequest request, out Guid sessionId)
    {
        sessionId = default;
        if (request is null) return false;

        try
        {
            var type = request.GetType();
            var prop = type.GetProperty("SessionId");
            if (prop != null)
            {
                var value = prop.GetValue(request) as string;
                if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out sessionId))
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignore and return false
        }

        return false;
    }

    private static bool IsBypassedMethod(string fullMethodName)
    {
        // fullMethodName format: "/{package}.{Service}/{Method}"
        if (string.IsNullOrWhiteSpace(fullMethodName)) return false;

        // Session bootstrap endpoints
        if (fullMethodName.EndsWith("/CreateSession", StringComparison.Ordinal) ||
            fullMethodName.EndsWith("/GetSession", StringComparison.Ordinal) ||
            fullMethodName.EndsWith("/EndSession", StringComparison.Ordinal))
        {
            return true;
        }

        // Allow character creation without existing session (first action in flow)
        if (fullMethodName.EndsWith("/CreateCharacter", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
