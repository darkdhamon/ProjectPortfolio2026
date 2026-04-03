using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Infrastructure.RequestTracking;

public sealed class RequestTrackingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var requestId = RequestIdResolver.Resolve(context.HttpContext, context.ActionArguments);
        context.HttpContext.Items[RequestIdContext.ItemKey] = requestId;

        var executedContext = await next();
        if (executedContext.Exception is not null || executedContext.Result is null)
        {
            return;
        }

        if (executedContext.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= StatusCodes.Status400BadRequest)
        {
            executedContext.Result = CreateErrorObjectResult(
                requestId,
                statusCodeResult.StatusCode,
                null,
                context.HttpContext.Request.Path);
            return;
        }

        if (executedContext.Result is ObjectResult objectResult)
        {
            if (objectResult.Value is ApiResponseDto apiResponse)
            {
                apiResponse.RequestId = requestId;
            }
            else if (GetStatusCode(objectResult) >= StatusCodes.Status400BadRequest)
            {
                executedContext.Result = CreateErrorObjectResult(
                    requestId,
                    GetStatusCode(objectResult),
                    objectResult.Value,
                    context.HttpContext.Request.Path);
            }
        }
    }

    private static ObjectResult CreateErrorObjectResult(string? requestId, int statusCode, object? value, PathString requestPath)
    {
        var errorResponse = new ApiErrorResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            ErrorCode = GetErrorCode(statusCode),
            Message = GetMessage(statusCode, value, requestPath),
            ValidationErrors = GetValidationErrors(value)
        };

        return new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };
    }

    private static int GetStatusCode(ObjectResult objectResult)
    {
        return objectResult.StatusCode
            ?? (objectResult.Value as ProblemDetails)?.Status
            ?? StatusCodes.Status500InternalServerError;
    }

    private static string GetErrorCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "resource_not_found",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status422UnprocessableEntity => "validation_failed",
            _ => "request_failed"
        };
    }

    private static string GetMessage(int statusCode, object? value, PathString requestPath)
    {
        if (value is ValidationProblemDetails validationProblem)
        {
            return string.IsNullOrWhiteSpace(validationProblem.Title)
                ? "One or more validation errors occurred."
                : validationProblem.Title;
        }

        if (value is ProblemDetails problemDetails)
        {
            if (!string.IsNullOrWhiteSpace(problemDetails.Detail))
            {
                return problemDetails.Detail;
            }

            if (!string.IsNullOrWhiteSpace(problemDetails.Title))
            {
                return problemDetails.Title;
            }
        }

        if (value is string message && !string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        return statusCode == StatusCodes.Status404NotFound
            ? $"Resource '{requestPath}' was not found."
            : ReasonPhrases.GetReasonPhrase(statusCode);
    }

    private static IDictionary<string, string[]>? GetValidationErrors(object? value)
    {
        return value is ValidationProblemDetails validationProblem
            ? validationProblem.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)
            : null;
    }
}
