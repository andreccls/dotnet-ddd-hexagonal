using DddHexagonal.Api;
using DddHexagonal.Api.Endpoints;
using DddHexagonal.Infrastructure;

// Composition root: the ONLY place where ports are wired to adapters.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddUseCases();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured."));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "dotnet-ddd-hexagonal v1"));
app.MapHealthChecks("/health");

app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

await app.RunAsync();

/// <summary>Makes the entry point visible to WebApplicationFactory in Api.Tests.</summary>
public partial class Program;
