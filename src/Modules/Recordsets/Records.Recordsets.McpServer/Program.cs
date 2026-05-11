using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Records.Core.Contracts;
using Records.Core.Infrastructure.Sql;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.McpServer;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Missing DefaultConnection connection string.");

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
}

builder.Services.AddCoreInfrastructureSql(connectionString, serverVersion);
builder.Services.AddRecordsetsInfrastructureSql(connectionString, serverVersion);
builder.Services.AddScoped<IRecordBagValidator, RecordBagValidator>();
builder.Services.AddScoped<IMigrationValidator, MigrationValidator>();
builder.Services.AddSingleton<McpTenantContextAccessor>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<McpTenantContextAccessor>());
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<CreateRecordCommandHandler>();
    config.RegisterServicesFromAssemblyContaining<UpdateRecordCommandHandler>();
});
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RecordsetsMcpTools>();

await builder.Build().RunAsync();
