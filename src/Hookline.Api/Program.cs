using Hookline.Api.ErrorHandling;
using Hookline.Modules.Delivery;
using Hookline.Modules.Ingestion;
using Hookline.Modules.Management;

using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json lists the setting with an empty value on purpose: each environment must provide it.
var connectionString = builder.Configuration.GetConnectionString("Hookline");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Hookline' is not configured.");
}

builder.Services
    .AddManagementModule(connectionString)
    .AddIngestionModule(connectionString)
    .AddDeliveryModule(connectionString);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
// Order matters: handlers are asked in registration order and the first one that handles wins.
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

var app = builder.Build();

// On in development and in Docker Compose. In production, migrations run as a deploy step instead.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    // The host checks the settings only when it starts, which comes after this point.
    // Checking them first means a misconfigured API never changes the database.
    app.Services.GetRequiredService<IStartupValidator>().Validate();

    await app.Services.ApplyManagementMigrationsAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// API description and interactive reference only in development:
// production does not expose the full map of the API.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

app.MapManagementEndpoints()
    .MapIngestionEndpoints()
    .MapDeliveryEndpoints();

app.Run();
