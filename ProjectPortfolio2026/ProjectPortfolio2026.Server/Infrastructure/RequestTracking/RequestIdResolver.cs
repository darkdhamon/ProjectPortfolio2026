using Microsoft.AspNetCore.Http;
using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Infrastructure.RequestTracking;

public static class RequestIdResolver
{
    public static string? Resolve(HttpContext httpContext, IDictionary<string, object?> actionArguments)
    {
        var bodyRequestId = actionArguments.Values
            .OfType<ApiRequestDto>()
            .Select(request => request.RequestId)
            .FirstOrDefault(requestId => !string.IsNullOrWhiteSpace(requestId));

        if (!string.IsNullOrWhiteSpace(bodyRequestId))
        {
            return bodyRequestId.Trim();
        }

        var queryRequestId = httpContext.Request.Query["requestId"].FirstOrDefault()
            ?? httpContext.Request.Query["RequestId"].FirstOrDefault()
            ?? httpContext.Request.Query["x-request-id"].FirstOrDefault()
            ?? httpContext.Request.Query["X-Request-Id"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(queryRequestId))
        {
            return queryRequestId.Trim();
        }

        var headerRequestId = httpContext.Request.Headers["X-Request-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerRequestId) ? null : headerRequestId.Trim();
    }
}
