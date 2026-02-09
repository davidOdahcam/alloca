using Alloca.Infra.Persistence;
using Alloca.Infra.Persistence.Seed;
using Alloca.IoC.Config;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AllocaDbContext>();
    db.Database.Migrate();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandlingConfig();
app.UseLocalizationConfig();

if (app.Environment.IsDevelopment())
    app.UseOpenApiConfig();

app.UseHttpsRedirection();
app.UseCors(CorsConfig.DefaultPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

app.Run();
