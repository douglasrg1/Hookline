using Hookline.Api.ErrorHandling;
using Hookline.Modules.Delivery;
using Hookline.Modules.Ingestion;
using Hookline.Modules.Management;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Hookline")
    ?? throw new InvalidOperationException("Connection string is not configured");

builder.Services
    .AddManagementModule(connectionString)
    .AddIngestionModule(connectionString)
    .AddDeliveryModule(connectionString);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

builder.Services.AddProblemDetails();
// Order matters: handlers are asked in registration order and the first one that handles wins.
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/health");

app.MapManagementEndpoints()
    .MapIngestionEndpoints()
    .MapDeliveryEndpoints();

app.Run();
