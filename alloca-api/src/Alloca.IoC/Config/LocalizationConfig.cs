using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Alloca.IoC.Config;

/// <summary>
/// Configura cultura de requisição (pt-BR padrão, en-US suportado) via Accept-Language.
/// Reflete em <see cref="CultureInfo.CurrentCulture"/> para FluentValidation, formatação de
/// datas/números e qualquer consumo de cultura no pipeline.
/// </summary>
public static class LocalizationConfig
{
    public static readonly string[] SupportedCultures = ["pt-BR", "en-US"];

    public static IServiceCollection AddLocalizationConfig(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supported = SupportedCultures.Select(c => new CultureInfo(c)).ToList();
            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supported;
            options.SupportedUICultures = supported;
            options.ApplyCurrentCultureToResponseHeaders = true;
        });
        return services;
    }

    public static IApplicationBuilder UseLocalizationConfig(this IApplicationBuilder app)
        => app.UseRequestLocalization();
}
