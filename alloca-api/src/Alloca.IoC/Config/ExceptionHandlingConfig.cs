using Alloca.IoC.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Alloca.IoC.Config;

public static class ExceptionHandlingConfig
{
    public static IApplicationBuilder UseExceptionHandlingConfig(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
