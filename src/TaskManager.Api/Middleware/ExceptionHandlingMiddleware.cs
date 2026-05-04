using System.Net;
using System.Text.Json;
using TaskManager.Application;

namespace TaskManager.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed");
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message).ConfigureAwait(false);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message).ConfigureAwait(false);
        }
        catch (ConflictException ex)
        {
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.").ConfigureAwait(false);
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode status, string detail)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("The response has already started.");
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            title = status.ToString(),
            status = (int)status,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions)).ConfigureAwait(false);
    }
}
