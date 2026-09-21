using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Application.Exceptions;

namespace PartnerIntegration.Api.ExceptionHandling;

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
        var problem = Map(httpContext, exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode} at {Path}", problem.Status, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails Map(HttpContext httpContext, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage).Distinct())),
            PartnerNotVerifiedException notVerified => (
                StatusCodes.Status422UnprocessableEntity,
                "Partner is not verified",
                notVerified.Message),
            PartnerVerificationUnavailableException unavailable => (
                StatusCodes.Status503ServiceUnavailable,
                "Partner verification unavailable",
                unavailable.Message),
            MessagingUnavailableException messaging => (
                StatusCodes.Status503ServiceUnavailable,
                "Message broker unavailable",
                messaging.Message),
            TimeoutException timeout => (
                StatusCodes.Status504GatewayTimeout,
                "Upstream timeout",
                timeout.Message),
            UnauthorizedAccessException unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorized.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The request could not be processed. Retry later or contact support.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => ToCamelCase(group.Key),
                    group => group.Select(error => error.ErrorMessage).ToArray());
        }

        return problem;
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
