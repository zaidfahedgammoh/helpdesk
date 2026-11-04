using FluentValidation;
using HelpDeskLive.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, extensions) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error processing {Path}: {Message}",
                httpContext.Request.Path, exception.Message);
        }
        else
        {
            _logger.LogWarning("Client error {StatusCode} processing {Path}: {Message}",
                statusCode, httpContext.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, IDictionary<string, object?>? Extensions) MapException(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException fluentValEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                new Dictionary<string, object?>
                {
                    ["errors"] = fluentValEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }),

            HelpDeskLive.Application.Common.Exceptions.ValidationException appValEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                appValEx.Message,
                new Dictionary<string, object?>
                {
                    ["errors"] = appValEx.Errors
                }),

            UnauthorizedException unauthEx => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthEx.Message,
                null),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenEx.Message,
                null),

            UnauthorizedAccessException accessEx => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                accessEx.Message,
                null),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundEx.Message,
                null),

            KeyNotFoundException keyNotFoundEx => (
                StatusCodes.Status404NotFound,
                "Not Found",
                keyNotFoundEx.Message,
                null),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictEx.Message,
                null),

            InvalidOperationException invalidOpEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                invalidOpEx.Message,
                null),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                argEx.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing your request.",
                null)
        };
    }
}
