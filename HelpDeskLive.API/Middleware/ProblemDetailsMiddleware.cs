using HelpDeskLive.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
namespace HelpDeskLive.API.Middleware;
public sealed class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;
    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    { _next = next; _logger = logger; }
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception exception)
        {
            if (context.Response.HasStarted) throw;
            var status = exception switch
            {
                HelpDeskLive.Application.Exceptions.ValidationException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                ForbiddenException => StatusCodes.Status403Forbidden,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };
            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled API exception");
            else
                _logger.LogWarning(exception, "API request rejected with status {StatusCode}", status);
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    400 => "Validation failed", 401 => "Unauthorized", 403 => "Forbidden",
                    404 => "Resource not found", 409 => "Conflict",
                    _ => "An unexpected error occurred"
                },
                Detail = status == 500 ? "The server could not complete the request." : exception.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (exception is HelpDeskLive.Application.Exceptions.ValidationException validation)
                problem.Extensions["errors"] = validation.Errors;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}