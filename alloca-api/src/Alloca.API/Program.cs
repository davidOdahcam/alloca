using Alloca.Infra.Persistence;
using Alloca.IoC;
using Alloca.IoC.Config;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AllocaDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandlingConfig();
app.UseLocalizationConfig();

if (app.Environment.IsDevelopment())
    app.UseOpenApiConfig();

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors(CorsConfig.DefaultPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
