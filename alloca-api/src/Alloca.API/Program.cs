using Alloca.IoC.Config;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

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
