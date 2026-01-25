using Records.App.Server;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

app.Run();

namespace Records.App.Server
{
    public class Program;
}