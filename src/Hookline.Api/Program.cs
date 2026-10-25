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

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
