using Records.App.Server;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, config) =>
    {
        config.MinimumLevel.Information()
            .MinimumLevel.Override("Azure.Core", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level} {CorrelationId}] {SourceContext} {Message:lj} {Exception}{NewLine}")
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationIdHeader("X-Correlation-ID");
    });

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

namespace Records.App.Server
{
    public class Program;
}