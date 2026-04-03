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

        var queryRequestId = FindValueCaseInsensitive(httpContext.Request.Query, "requestId")
            ?? FindValueCaseInsensitive(httpContext.Request.Query, "x-request-id");

        if (!string.IsNullOrWhiteSpace(queryRequestId))
        {
            return queryRequestId.Trim();
        }

        var headerRequestId = FindValueCaseInsensitive(httpContext.Request.Headers, "x-request-id");
        return string.IsNullOrWhiteSpace(headerRequestId) ? null : headerRequestId.Trim();
    }

    private static string? FindValueCaseInsensitive(IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> values, string key)
    {
        return values.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            .Value
            .FirstOrDefault();
    }
}
