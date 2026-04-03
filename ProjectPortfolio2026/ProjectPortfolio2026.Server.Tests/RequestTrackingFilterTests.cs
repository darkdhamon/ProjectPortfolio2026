using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class RequestTrackingFilterTests
{
    [Test]
    public async Task Filter_StoresBodyRequestId_AndAppliesItToSuccessResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "header-id";
        httpContext.Request.QueryString = new QueryString("?requestId=query-id");

        var actionContext = CreateActionContext(httpContext);
        var request = new ProjectRequest
        {
            RequestId = "body-id",
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary."
        };

        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = request },
            controller: new object());

        var filter = new RequestTrackingFilter();

        var executed = false;
        ActionExecutedContext? finalContext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                executed = true;
                var response = new ProjectResponse { Title = "Portfolio Platform" };
                finalContext = new ActionExecutedContext(actionContext, [], new object())
                {
                    Result = new OkObjectResult(response)
                };

                return Task.FromResult(finalContext);
            });

        Assert.That(executed, Is.True);
        var okResult = finalContext!.Result as OkObjectResult;
        var responseDto = okResult?.Value as ProjectResponse;
        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.RequestId, Is.EqualTo("body-id"));
    }

    [Test]
    public async Task Filter_WrapsNotFoundResult_InApiErrorResponse_WithResolvedRequestId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "header-id";
        httpContext.Request.QueryString = new QueryString("?requestId=query-id");
        httpContext.Request.Path = "/api/projects/101";

        var actionContext = CreateActionContext(httpContext);
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());

        var filter = new RequestTrackingFilter();

        ActionExecutedContext? finalContext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                finalContext = new ActionExecutedContext(actionContext, [], new object())
                {
                    Result = new NotFoundResult()
                };

                return Task.FromResult(finalContext);
            });

        Assert.That(finalContext, Is.Not.Null);
        var notFoundObjectResult = finalContext!.Result as ObjectResult;
        var response = notFoundObjectResult?.Value as ApiErrorResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(notFoundObjectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            Assert.That(response!.RequestId, Is.EqualTo("query-id"));
            Assert.That(response.ErrorCode, Is.EqualTo("resource_not_found"));
            Assert.That(response.Message, Does.Contain("/api/projects/101"));
        });
    }

    [Test]
    public async Task Filter_UsesHeaderRequestId_WhenBodyAndQueryAreMissing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-ReQuEsT-iD"] = "header-id";

        var actionContext = CreateActionContext(httpContext);
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());

        var filter = new RequestTrackingFilter();

        ActionExecutedContext? finalContext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                finalContext = new ActionExecutedContext(actionContext, [], new object())
                {
                    Result = new OkObjectResult(new ProjectListResponse())
                };

                return Task.FromResult(finalContext);
            });

        var okResult = finalContext!.Result as OkObjectResult;
        var response = okResult?.Value as ProjectListResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RequestId, Is.EqualTo("header-id"));
    }

    [Test]
    public async Task Filter_UsesCaseInsensitiveQueryKey_WhenResolvingRequestId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?ReQuEsTiD=query-id");

        var actionContext = CreateActionContext(httpContext);
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());

        var filter = new RequestTrackingFilter();
        ActionExecutedContext? finalContext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                finalContext = new ActionExecutedContext(actionContext, [], new object())
                {
                    Result = new OkObjectResult(new ProjectResponse())
                };

                return Task.FromResult(finalContext);
            });

        var okResult = finalContext!.Result as OkObjectResult;
        var response = okResult?.Value as ProjectResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RequestId, Is.EqualTo("query-id"));
    }

    [Test]
    public async Task Filter_WrapsValidationProblemDetails_InApiErrorResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "header-id";
        httpContext.Request.Path = "/api/projects";

        var actionContext = CreateActionContext(httpContext);
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());

        var filter = new RequestTrackingFilter();
        ActionExecutedContext? finalContext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                var validationProblem = new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["Title"] = ["The Title field is required."]
                    })
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };

                finalContext = new ActionExecutedContext(actionContext, [], new object())
                {
                    Result = new BadRequestObjectResult(validationProblem)
                };

                return Task.FromResult(finalContext);
            });

        var badRequestResult = finalContext!.Result as ObjectResult;
        var response = badRequestResult?.Value as ApiErrorResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(response!.RequestId, Is.EqualTo("header-id"));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(response.ErrorCode, Is.EqualTo("bad_request"));
            Assert.That(response.Message, Is.EqualTo("One or more validation errors occurred."));
            Assert.That(response.ValidationErrors, Contains.Key("Title"));
        });
    }

    private static ActionContext CreateActionContext(HttpContext httpContext)
    {
        return new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor
            {
                ActionName = "Test",
                ControllerName = "Projects"
            },
            new ModelStateDictionary());
    }
}
