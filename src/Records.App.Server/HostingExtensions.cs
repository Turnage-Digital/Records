using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Records.App.Server.Services;
using Records.Clocks.Application.Commands.ClockDefinitions.Create;
using Records.Clocks.Application.Commands.Clocks.Start;
using Records.Clocks.Infrastructure.Sql;
using Records.Clocks.Presentation.Controllers;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;
using Records.Notifications.Application.Commands;
using Records.Notifications.Application.Commands.NotificationRules.Create;
using Records.Notifications.Application.Commands.NotificationRules.Delete;
using Records.Notifications.Application.Commands.NotificationRules.Update;
using Records.Notifications.Infrastructure.Sql;
using Records.Notifications.Presentation.Controllers;
using Records.Recordsets.Application.Commands.CreateRecord;
using Records.Recordsets.Application.Commands.CreateRecordset;
using Records.Recordsets.Application.Commands.Migrations;
using Records.Recordsets.Application.Commands.UpdateRecord;
using Records.Recordsets.Application.Commands.UpdateRecordsetSchema;
using Records.Recordsets.Application.Migrations.Services;
using Records.Recordsets.Application.Queries.Migrations;
using Records.Recordsets.Domain.Services;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.Presentation.Controllers;
using Records.Tenants.Application.Commands.CreateTenant;
using Records.Tenants.Infrastructure.Sql;
using Records.Tenants.Presentation.Controllers;
using Records.Users.Application.Commands.GrantUserRole;
using Records.Users.Application.Commands.InviteUser;
using Records.Users.Application.Commands.RevokeUserRole;
using Records.Users.Application.Commands.SuspendUser;
using Records.Users.Domain.Entities;
using Records.Users.Infrastructure.Sql;
using Records.Users.Presentation.Controllers;

namespace Records.App.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
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
            .AddApplicationPart(typeof(NotificationsController).Assembly);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Missing DefaultConnection connection string.");
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        builder.Services.AddCoreInfrastructureSql(connectionString, serverVersion);
        builder.Services.AddUsersInfrastructureSql(connectionString);
        builder.Services.AddTenantsInfrastructureSql(connectionString);
        builder.Services.AddRecordsetsInfrastructureSql(connectionString);
        builder.Services.AddClocksInfrastructureSql(connectionString);
        builder.Services.AddNotificationsInfrastructureSql(connectionString);
        builder.Services.AddScoped<IRecordBagValidator, RecordBagValidator>();
        builder.Services.AddScoped<IMigrationValidator, MigrationValidator>();
        builder.Services.AddScoped<RecordsetMigrationJobRunner>();

        builder.Services
            .AddIdentityApiEndpoints<User>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<ChangeFeed>();
        builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(ChangeFeedNotificationHandler<>));

        builder.Services.AddAuthentication().AddIdentityCookies();
        builder.Services.AddAuthorization();

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
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen();
        }

        builder.Services.AddHostedService<RecordsetMigrationDispatcherService>();
        builder.Services.AddHostedService<ClockWatchdogService>();
        builder.Services.AddHostedService<DeferredDispatchProcessor>();
        builder.Services.AddHostedService<NotificationProcessingService>();

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
            app.UseExceptionHandler();
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        var identityGroup = app
            .MapGroup("/identity")
            .WithTags("Identity");

        identityGroup.MapIdentityApi<User>();

        identityGroup.MapPost("logout",
            async (SignInManager<User> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Ok();
            }
        );

        return app;
    }
}
