using Hookline.Api.ErrorHandling;
using Hookline.Modules.Delivery;
using Hookline.Modules.Ingestion;
using Hookline.Modules.Management;

using Microsoft.AspNetCore.Authorization;
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

// Every route requires an authenticated caller, unless it is explicitly marked as public.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddProblemDetails();
// Order matters: handlers are asked in registration order and the first one that handles wins.
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

var app = builder.Build();

// The host would check the settings only inside app.Run(). Checking them right away stops a
// misconfigured API before anything else runs, so it never changes the database.
app.Services.GetRequiredService<IStartupValidator>().Validate();

// On in development and in Docker Compose. In production, migrations run as a deploy step instead.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyManagementMigrationsAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// Added explicitly after the error handling, so a failure while checking a key, such as the
// database being down, still becomes a Problem Details response.
app.UseAuthentication();
app.UseAuthorization();

// API description and interactive reference only in development:
// production does not expose the full map of the API.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();

app.MapManagementEndpoints()
    .MapIngestionEndpoints()
    .MapDeliveryEndpoints();

app.Run();
