using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Records.Agents.Application.Commands;
using Records.Agents.Infrastructure.Sql;
using Records.Agents.Presentation.Controllers;
using Records.App.ChangeFeed;
using Records.App.ChangeFeed.Controllers;
using Records.App.Infrastructure.Security;
using Records.App.Server.Services;
using Records.Clocks.Application.Commands;
using Records.Clocks.Infrastructure.Sql;
using Records.Clocks.Presentation.Controllers;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;
using Records.Notifications.Application.Commands;
using Records.Notifications.Infrastructure.Sql;
using Records.Notifications.Presentation.Controllers;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Application.Queries;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.Presentation.Controllers;
using Records.Tenants.Application.Commands;
using Records.Tenants.Infrastructure.Sql;
using Records.Tenants.Presentation.Controllers;
using Records.Users.Application.Commands;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql;
using Records.Users.Presentation.Controllers;

namespace Records.App.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var isTestingEnvironment = builder.Environment.IsEnvironment("Testing");

        builder.Services.AddHealthChecks();
        builder.Services.Configure<SeedOptions>(
            builder.Configuration.GetSection("Seed"));
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                "Records.App.Server",
                serviceVersion: typeof(HostingExtensions).Assembly.GetName().Version?.ToString()))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(UnitOfWorkTelemetry.MeterName)
                    .AddMeter(BackgroundServiceTelemetry.MeterName)
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(UnitOfWorkTelemetry.ActivitySourceName)
                    .AddSource(BackgroundServiceTelemetry.ActivitySourceName);

                var otlpEndpoint = builder.Configuration["OpenTelemetry:Exporter:Endpoint"];
                tracing.AddOtlpExporter(options =>
                {
                    if (!string.IsNullOrWhiteSpace(otlpEndpoint) &&
                        Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                    {
                        options.Endpoint = endpoint;
                    }
                });
            });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UlidIdJsonConverter());
            })
            .AddApplicationPart(typeof(TenantsController).Assembly)
            .AddApplicationPart(typeof(UsersController).Assembly)
            .AddApplicationPart(typeof(RecordsetsController).Assembly)
            .AddApplicationPart(typeof(ClockDefinitionsController).Assembly)
            .AddApplicationPart(typeof(NotificationsController).Assembly)
            .AddApplicationPart(typeof(AgentsController).Assembly)
            .AddApplicationPart(typeof(ChangeStreamController).Assembly);

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
        builder.Services.AddUsersInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddTenantsInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddRecordsetsInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddClocksInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddNotificationsInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddAgentsInfrastructureSql(connectionString, serverVersion);

        builder.Services.AddScoped<IRecordBagValidator, RecordBagValidator>();
        builder.Services.AddScoped<IMigrationValidator, MigrationValidator>();
        builder.Services.AddScoped<RecordsetMigrationJobRunner>();

        builder.Services
            .AddIdentityApiEndpoints<User>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddChangeFeed();

        builder.Services.AddSecurity();

        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<CreateTenantCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<InviteUserCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<GrantUserRoleCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<RevokeUserRoleCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<SuspendUserCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateRecordsetCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<UpdateRecordsetSchemaCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateRecordCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<UpdateRecordCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<RunMigrationCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<GetRecordsetMigrationJobStatusQueryHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateClockDefinitionCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<StartClockCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateNotificationCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateNotificationRuleCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<UpdateNotificationRuleCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<DeleteNotificationRuleCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateAgentThreadCommandHandler>();
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen();

            builder.Services.AddHostedService<SeedDataService>();
        }

        if (!isTestingEnvironment)
        {
            builder.Services.AddHostedService<RecordsetMigrationDispatcherService>();
            builder.Services.AddHostedService<ClockWatchdogService>();
            builder.Services.AddHostedService<DeferredDispatchService>();
            builder.Services.AddHostedService<NotificationProcessingService>();
        }

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        var identityGroup = app
            .MapGroup("/identity")
            .WithTags("Identity");

        identityGroup.MapIdentityApi<User>();

        identityGroup.MapGet(
                "session",
                async (HttpContext context, ICurrentUserAccess currentUserAccess, CancellationToken cancellationToken) =>
                {
                    context.Response.Headers.CacheControl = "no-store";

                    if (context.User.Identity?.IsAuthenticated != true)
                    {
                        return Results.Unauthorized();
                    }

                    var userId = FindClaimValue(context.User, ClaimTypes.NameIdentifier, "sub");
                    var email = FindClaimValue(context.User, ClaimTypes.Email, "email");
                    var displayName = FindClaimValue(context.User, ClaimTypes.Name, "name") ?? email;
                    var userName = FindClaimValue(context.User, ClaimTypes.Name, "preferred_username", "unique_name") ??
                                   email ??
                                   displayName;
                    var tenantId = FindClaimValue(context.User, "tenantId", "tenantid", "tenant_id", "tid", "tenant");
                    var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
                    var canAccessOps = await currentUserAccess.CanAccessOpsAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        user = new
                        {
                            id = userId,
                            userId,
                            sub = userId,
                            tenantId,
                            userName,
                            email,
                            name = displayName
                        },
                        access = new
                        {
                            isGlobalAdmin,
                            canAccessOps
                        }
                    });
                })
            .AllowAnonymous();

        identityGroup.MapPost("logout",
            async (SignInManager<User> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Ok();
            }
        );

        identityGroup.MapGet(
                "access",
                async (ICurrentUserAccess currentUserAccess, CancellationToken cancellationToken) =>
                {
                    var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
                    var canAccessOps = await currentUserAccess.CanAccessOpsAsync(cancellationToken);

                    return Results.Ok(new
                    {
                        isGlobalAdmin,
                        canAccessOps
                    });
                })
            .RequireAuthorization();

        app.MapHealthChecks("/health")
            .AllowAnonymous();

        app.MapPrometheusScrapingEndpoint()
            .AllowAnonymous();

        app.Map("/error", (HttpContext context, IHostEnvironment env) =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var detail = env.IsDevelopment() || env.IsEnvironment("Testing")
                    ? error?.ToString()
                    : null;
                return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: detail);
            })
            .AllowAnonymous();

        app.MapFallbackToFile("index.html");

        return app;
    }

    private static string? FindClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
