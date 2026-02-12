using System.Globalization;
using System.Text.Json;
using Alloca.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Alloca.IoC.Middleware;

/// <summary>
/// Captura exceções e devolve um payload no estilo ProblemDetails com <c>code</c> estável
/// para tradução no frontend (errors.codes.&lt;code&gt;). O campo <c>detail</c> serve apenas
/// como fallback de leitura humana.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ValidationException ex)
        {
            var mensagens = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            var fields = ex.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "_" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            var primaria = mensagens.FirstOrDefault() ?? "Os dados enviados são inválidos.";
            await WriteAsync(context, 400, ErrorCodes.ValidationError, primaria, mensagens, fields);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message, new[] { ex.Message }, args: ex.Args);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            const string msg = "Erro inesperado. Tente novamente em instantes.";
            await WriteAsync(context, 500, ErrorCodes.Unknown, msg, new[] { msg });
        }
    }

    private static Task WriteAsync(
        HttpContext ctx,
        int status,
        string code,
        string detail,
        IEnumerable<string> messages,
        IDictionary<string, string[]>? fields = null,
        object[]? args = null)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";

        var payload = new
        {
            code,
            detail,
            messages,
            args = args ?? Array.Empty<object>(),
            errors = fields,
            culture = CultureInfo.CurrentCulture.Name,
            traceId = ctx.TraceIdentifier
        };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
