using System.Net;
using System.Text.Json;
using Alloca.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Alloca.IoC.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ValidationException ex) { await WriteAsync(context, 400, "validation_error", ex.Errors.Select(e => e.ErrorMessage)); }
        catch (AppException ex) { await WriteAsync(context, ex.StatusCode, ex.GetType().Name, new[] { ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, 500, "server_error", new[] { "Unexpected error." });
        }
    }

    private static Task WriteAsync(HttpContext ctx, int status, string code, IEnumerable<string> messages)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { error = code, messages });
        return ctx.Response.WriteAsync(payload);
    }
}
