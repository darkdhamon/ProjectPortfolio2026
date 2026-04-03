using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

        if (executedContext.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode == StatusCodes.Status404NotFound)
        {
            executedContext.Result = CreateNotFoundObjectResult(requestId, context.HttpContext.Request.Path);
            return;
        }

        if (executedContext.Result is ObjectResult objectResult)
        {
            if (objectResult.Value is ApiResponseDto apiResponse)
            {
                apiResponse.RequestId = requestId;
            }
            else if (objectResult.StatusCode == StatusCodes.Status404NotFound)
            {
                executedContext.Result = CreateNotFoundObjectResult(requestId, context.HttpContext.Request.Path);
            }
        }
    }

    private static NotFoundObjectResult CreateNotFoundObjectResult(string? requestId, PathString requestPath)
    {
        return new NotFoundObjectResult(new ApiErrorResponse
        {
            RequestId = requestId,
            ErrorCode = "resource_not_found",
            Message = $"Resource '{requestPath}' was not found."
        });
    }
}
