using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alloca.API.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Informe o token JWT (sem o prefixo 'Bearer ')."
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var op in path.Operations!.Values)
            {
                op.Security ??= new List<OpenApiSecurityRequirement>();
                op.Security.Add(requirement);
            }
        }

        document.Info.Title = "Alloca API";
        document.Info.Version = "v1";
        document.Info.Description = "API de reservas de espaços acadêmicos (salas e mesas).";
        return Task.CompletedTask;
    }
}
