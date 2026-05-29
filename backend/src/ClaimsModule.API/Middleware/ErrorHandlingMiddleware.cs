using System.Net;
using System.Text.Json;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Domain.Common;

namespace ClaimsModule.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            var errorCodes = vex.ErrorDetails
                .SelectMany(kvp => kvp.Value)
                .Select(d => d.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToArray();
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "validation_failed", vex.Message,
                new
                {
                    errors = vex.Errors,
                    errorDetails = vex.ErrorDetails,
                    errorCodes
                });
        }
        catch (NotFoundException nex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "not_found", nex.Message);
        }
        catch (ForbiddenException fex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "forbidden", fex.Message);
        }
        catch (DomainException dex)
        {
            await WriteProblemAsync(context, HttpStatusCode.UnprocessableEntity, dex.Code, dex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "internal_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode status, string code,
        string message, object? extras = null)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        var payload = new
        {
            type = "about:blank",
            title = code,
            status = (int)status,
            detail = message,
            extras
        };
        await JsonSerializer.SerializeAsync(context.Response.Body, payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
