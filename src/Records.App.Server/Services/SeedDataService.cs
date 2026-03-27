using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;
using Records.Notifications.Infrastructure.Sql;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;
using Records.Recordsets.Infrastructure.Sql;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql;
using Records.Tenants.Infrastructure.Sql.Entities;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.App.Server.Services;

public sealed class SeedDataService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    IOptions<SeedOptions> seedOptions,
    ILogger<SeedDataService> logger
) : IHostedService
{
    private static readonly string[] FirstNames =
    [
        "Avery", "Blake", "Cameron", "Dakota", "Elliot", "Finley", "Harper", "Jordan",
        "Kai", "Logan", "Morgan", "Noah", "Parker", "Quinn", "Riley", "Sawyer",
        "Sydney", "Taylor", "Wesley", "Zion"
    ];

    private static readonly string[] LastNames =
    [
        "Anderson", "Bishop", "Carter", "Davis", "Ellis", "Foster", "Gray", "Hayes",
        "Jenkins", "Kelly", "Mitchell", "Owens", "Perry", "Price", "Reed", "Rivera",
        "Stewart", "Turner", "Walker", "Young"
    ];

    private static readonly (string City, string State)[] CityStates =
    [
        ("Atlanta", "GA"),
        ("Austin", "TX"),
        ("Boston", "MA"),
        ("Charlotte", "NC"),
        ("Chicago", "IL"),
        ("Columbus", "OH"),
        ("Denver", "CO"),
        ("Madison", "WI"),
        ("Nashville", "TN"),
        ("Portland", "OR"),
        ("Raleigh", "NC"),
        ("Salt Lake City", "UT"),
        ("Seattle", "WA"),
        ("Tampa", "FL")
    ];

    private static readonly string[] StreetNames =
    [
        "Maple", "Oak", "Pine", "Elm", "Cedar", "Willow", "Hillcrest", "River", "Lake",
        "Sunset", "Main", "Park", "Franklin", "Jefferson", "Magnolia", "Walnut"
    ];

    private static readonly string[] StreetSuffixes =
    [
        "Ave", "Blvd", "Cir", "Ct", "Dr", "Ln", "Rd", "St", "Ter", "Way"
    ];

    private static readonly string[] TeacherPrefixes = ["Ms.", "Mr.", "Mx.", "Dr."];

    private static readonly string[] ProjectThemes =
    [
        "Atlas", "Beacon", "Catalyst", "Drift", "Elevate", "Forge", "Harbor",
        "Meridian", "Northstar", "Orbit", "Pulse", "Summit", "Voyager"
    ];

    private static readonly string[] ProjectTargets =
    [
        "Analytics", "Automation", "Billing", "Compliance", "Fulfillment", "Growth",
        "Insights", "Inventory", "Lifecycle", "Onboarding", "Platform", "Reporting"
    ];

    private static readonly string[] ProjectOutcomes =
    [
        "reduce manual work for partner teams",
        "speed up monthly close reporting",
        "improve handoffs between operations and support",
        "make backlog health visible to leadership",
        "stabilize API throughput during peak load",
        "support new enterprise onboarding workflows",
        "tighten audit readiness for critical records",
        "unblock field teams waiting on approvals"
    ];

    private static readonly string[] ProjectTagsPool =
    [
        "api", "backend", "compliance", "data", "infrastructure", "mobile", "ops", "research", "ux", "web"
    ];

    private static readonly string[] ProjectOwners =
    [
        "Heath Turnage", "Erika Turnage", "Sam Price", "Lydia Hart", "Noah Gray", "Avery Bishop"
    ];

    private static readonly string[] InventoryCategories =
    [
        "Audio", "Computing", "Facilities", "Networking", "Office", "Safety", "Vehicles", "Video"
    ];

    private static readonly string[] InventoryLocations =
    [
        "HQ - Floor 1", "HQ - Floor 2", "HQ - Warehouse", "Field Office - East", "Field Office - West",
        "Remote Storage A", "Remote Storage B", "Studio"
    ];

    private static readonly string[] InventoryVendors =
    [
        "Acme Industrial", "Blue Ridge Supply", "Evergreen Systems", "Northline Equipment",
        "Peak Office Co.", "Precision Fleet", "SignalWorks", "Summit Safety"
    ];

    private static readonly string[] InventoryNotes =
    [
        "Pending inspection before reassignment.",
        "Recently serviced and ready for use.",
        "Reserved for upcoming training event.",
        "Needs accessory kit before deployment.",
        "Usage spikes every quarter-end."
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var options = seedOptions.Value;
        if (!options.Enabled)
        {
            logger.LogDebug("SeedData is disabled");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.GlobalAdminEmail) ||
            string.IsNullOrWhiteSpace(options.GlobalAdminPassword))
        {
            throw new InvalidOperationException(
                "DevelopmentSeed settings require GlobalAdminEmail and GlobalAdminPassword.");
        }

        using var scope = scopeFactory.CreateScope();
        var context = CreateContext(scope.ServiceProvider);
        var seededAt = DateTimeOffset.UtcNow;
        var random = new Random(58231);

        logger.LogInformation("Seeding development demo data");

        var admin = await EnsureSeedUserAsync(
            context,
            new UserSeed(options.GlobalAdminEmail.Trim(), options.GlobalAdminDisplayName.Trim()),
            options.GlobalAdminPassword,
            cancellationToken);

        var heath = await EnsureSeedUserAsync(
            context,
            new UserSeed("heath@records.local", "Heath Turnage"),
            options.GlobalAdminPassword,
            cancellationToken);

        var erika = await EnsureSeedUserAsync(
            context,
            new UserSeed("erika@records.local", "Erika Turnage"),
            options.GlobalAdminPassword,
            cancellationToken);

        var sam = await EnsureSeedUserAsync(
            context,
            new UserSeed("sam@records.local", "Sam Price"),
            options.GlobalAdminPassword,
            cancellationToken);

        await EnsureRoleMembershipAsync(
            context.UsersDbContext,
            admin,
            UserRole.GlobalAdmin,
            null,
            admin.Id,
            seededAt,
            cancellationToken);

        TenantDb? tenant = null;
        if (!string.IsNullOrWhiteSpace(options.TenantName))
        {
            tenant = await EnsureTenantAsync(
                options.TenantName.Trim(),
                context.TenantsDbContext,
                seededAt.AddDays(-90),
                cancellationToken);

            var tenantId = RequireUlid(tenant.Id, "seed tenant");

            await EnsureRoleMembershipAsync(
                context.UsersDbContext,
                heath,
                UserRole.TenantAdmin,
                tenantId,
                admin.Id,
                seededAt,
                cancellationToken);

            await EnsureRoleMembershipAsync(
                context.UsersDbContext,
                erika,
                UserRole.Operations,
                tenantId,
                admin.Id,
                seededAt,
                cancellationToken);

            await EnsureRoleMembershipAsync(
                context.UsersDbContext,
                sam,
                UserRole.TenantAdmin,
                tenantId,
                admin.Id,
                seededAt,
                cancellationToken);

            await EnsureRoleMembershipAsync(
                context.UsersDbContext,
                sam,
                UserRole.Operations,
                tenantId,
                admin.Id,
                seededAt,
                cancellationToken);
        }
        else
        {
            logger.LogDebug("TenantName not configured. Tenant-specific demo roles and notifications will be skipped.");
        }

        var ownerId = RequireUlid(heath.Id, "seed record owner");

        var students = await EnsureStudentsRecordsetAsync(context, ownerId, random, seededAt, cancellationToken);
        var projects = await EnsureProjectsRecordsetAsync(context, ownerId, random, seededAt, cancellationToken);
        var inventory = await EnsureInventoryRecordsetAsync(context, ownerId, random, seededAt, cancellationToken);

        if (tenant is not null)
        {
            var tenantId = RequireUlid(tenant.Id, "seed tenant");

            await EnsureStudentsNotificationsAsync(
                context,
                tenantId,
                students,
                heath,
                erika,
                seededAt,
                cancellationToken);

            await EnsureProjectsNotificationsAsync(
                context,
                tenantId,
                projects,
                heath,
                erika,
                seededAt,
                cancellationToken);

            await EnsureInventoryNotificationsAsync(
                context,
                tenantId,
                inventory,
                heath,
                sam,
                seededAt,
                cancellationToken);
        }

        logger.LogInformation(
            "Development seed bootstrap complete. Demo users: {AdminEmail}, heath@records.local, erika@records.local, sam@records.local",
            admin.Email);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static SeedContext CreateContext(IServiceProvider services)
    {
        return new SeedContext(
            services.GetRequiredService<UserManager<User>>(),
            services.GetRequiredService<UsersDbContext>(),
            services.GetRequiredService<TenantsDbContext>(),
            services.GetRequiredService<IRecordsetsUnitOfWork>(),
            services.GetRequiredService<RecordsetsDbContext>(),
            services.GetRequiredService<IRecordBagValidator>(),
            services.GetRequiredService<IRecordsetProjectionWriter>(),
            services.GetRequiredService<INotificationsUnitOfWork>(),
            services.GetRequiredService<NotificationsDbContext>());
    }

    private async Task<User> EnsureSeedUserAsync(
        SeedContext context,
        UserSeed seed,
        string password,
        CancellationToken cancellationToken
    )
    {
        var email = seed.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var displayName = string.IsNullOrWhiteSpace(seed.DisplayName) ? null : seed.DisplayName.Trim();

        var user = await context.UserManager.FindByEmailAsync(email);
        var created = false;

        if (user is null)
        {
            user = new User
            {
                Id = UlidId.NewUlid().ToString(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                UserName = email,
                NormalizedUserName = normalizedEmail,
                DisplayName = displayName,
                EmailConfirmed = true,
                Status = UserStatus.Active
            };

            var createResult = await context.UserManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed creating seed user '{email}': {FormatErrors(createResult.Errors)}");
            }

            created = true;
            logger.LogInformation("Created seed user {Email}", email);
        }
        else
        {
            var updated = false;

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                updated = true;
            }

            if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = email;
                updated = true;
            }

            if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            {
                user.NormalizedEmail = normalizedEmail;
                updated = true;
            }

            if (!string.Equals(user.NormalizedUserName, normalizedEmail, StringComparison.Ordinal))
            {
                user.NormalizedUserName = normalizedEmail;
                updated = true;
            }

            if (!string.Equals(user.DisplayName, displayName, StringComparison.Ordinal))
            {
                user.DisplayName = displayName;
                updated = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                updated = true;
            }

            if (user.Status != UserStatus.Active)
            {
                user.Status = UserStatus.Active;
                updated = true;
            }

            if (updated)
            {
                var updateResult = await context.UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed updating seed user '{email}': {FormatErrors(updateResult.Errors)}");
                }
            }

            if (!await context.UserManager.HasPasswordAsync(user))
            {
                var addPasswordResult = await context.UserManager.AddPasswordAsync(user, password);
                if (!addPasswordResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed setting password for seed user '{email}': {FormatErrors(addPasswordResult.Errors)}");
                }
            }
        }

        _ = RequireUlid(user.Id, $"seed user '{email}'");
        await UpsertUserProjectionAsync(context.UsersDbContext, user, cancellationToken);

        if (!created)
        {
            logger.LogDebug("Seed user {Email} already exists", email);
        }

        return user;
    }

    private static async Task UpsertUserProjectionAsync(
        UsersDbContext usersDbContext,
        User user,
        CancellationToken cancellationToken
    )
    {
        var projection = await usersDbContext.UserProjections
            .SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);

        if (projection is null)
        {
            usersDbContext.UserProjections.Add(new UserProjectionDb
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Status = user.Status,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            projection.Email = user.Email ?? string.Empty;
            projection.DisplayName = user.DisplayName;
            projection.Status = user.Status;
            projection.LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        await usersDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoleMembershipAsync(
        UsersDbContext usersDbContext,
        User user,
        UserRole role,
        UlidId? tenantId,
        string grantedBy,
        DateTimeOffset grantedAt,
        CancellationToken cancellationToken
    )
    {
        var tenantKey = tenantId?.ToString();
        var exists = await usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == user.Id &&
                     x.Role == role &&
                     x.TenantId == tenantKey,
                cancellationToken);

        if (exists)
        {
            return;
        }

        usersDbContext.UserRoleMemberships.Add(new UserRoleMembershipDb
        {
            UserId = user.Id,
            Role = role,
            TenantId = tenantKey,
            GrantedBy = grantedBy,
            GrantedAt = grantedAt
        });

        await usersDbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Granted {Role} role to seed user {UserId} for tenant {TenantId}",
            role,
            user.Id,
            tenantKey ?? "<global>");
    }

    private async Task<TenantDb> EnsureTenantAsync(
        string tenantName,
        TenantsDbContext tenantsDbContext,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken
    )
    {
        var tenant = await tenantsDbContext.Tenants
            .SingleOrDefaultAsync(x => x.Name == tenantName, cancellationToken);

        if (tenant is null)
        {
            tenant = new TenantDb
            {
                Id = UlidId.NewUlid().ToString(),
                Name = tenantName,
                Status = TenantStatus.Active
            };

            tenantsDbContext.Tenants.Add(tenant);
            await tenantsDbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created seed tenant {TenantId}", tenant.Id);
        }

        var projection = await tenantsDbContext.TenantProjections
            .SingleOrDefaultAsync(x => x.TenantId == tenant.Id, cancellationToken);

        if (projection is null)
        {
            tenantsDbContext.TenantProjections.Add(new TenantProjectionDb
            {
                TenantId = tenant.Id,
                Name = tenant.Name,
                Status = tenant.Status,
                CreatedAt = createdAt
            });
        }
        else
        {
            projection.Name = tenant.Name;
            projection.Status = tenant.Status;
        }

        await tenantsDbContext.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    private Task<SeededRecordset> EnsureStudentsRecordsetAsync(
        SeedContext context,
        UlidId ownerId,
        Random random,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        Status[] statuses =
        [
            CreateStatus("Active", "#FFCA28"),
            CreateStatus("Probation", "#FF7043"),
            CreateStatus("Inactive", "#607D8B"),
            CreateStatus("Graduated", "#66BB6A")
        ];

        Column[] columns =
        [
            TextColumn("Name", true, "name"),
            TextColumn("Address", storageKey: "address"),
            TextColumn("City", storageKey: "city"),
            TextColumn("State", storageKey: "state"),
            TextColumn("Zip Code", storageKey: "zipCode", regex: "^\\d{5}$"),
            DateColumn("Date Of Birth", "dateOfBirth"),
            TextColumn("Guardian Name", storageKey: "guardianName"),
            TextColumn("Homeroom Teacher", storageKey: "homeroomTeacher"),
            NumberColumn("GPA", "gpa", 0m, 4m)
        ];

        StatusTransition[] transitions =
        [
            new() { From = "Active", AllowedNext = ["Probation", "Inactive", "Graduated"] },
            new() { From = "Probation", AllowedNext = ["Active", "Inactive"] },
            new() { From = "Inactive", AllowedNext = ["Active"] },
            new() { From = "Graduated", AllowedNext = [] }
        ];

        return EnsureRecordsetAsync(
            context,
            "Students",
            ownerId,
            seededAt.AddDays(-75),
            columns,
            statuses,
            transitions,
            recordset => BuildStudentRecords(recordset, random, seededAt, 180),
            cancellationToken);
    }

    private Task<SeededRecordset> EnsureProjectsRecordsetAsync(
        SeedContext context,
        UlidId ownerId,
        Random random,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        Status[] statuses =
        [
            CreateStatus("Backlog", "#90A4AE"),
            CreateStatus("In Progress", "#42A5F5"),
            CreateStatus("Blocked", "#EF5350"),
            CreateStatus("Done", "#66BB6A")
        ];

        Column[] columns =
        [
            TextColumn("Title", true, "title"),
            TextColumn("Summary", storageKey: "summary"),
            TextColumn("Owner", storageKey: "owner"),
            DateColumn("Due Date", "dueDate"),
            NumberColumn("Priority", "priority", 1, 5),
            NumberColumn("Budget", "budget", 10000m, 250000m),
            TextColumn("Tags", storageKey: "tags")
        ];

        StatusTransition[] transitions =
        [
            new() { From = "Backlog", AllowedNext = ["In Progress", "Blocked"] },
            new() { From = "In Progress", AllowedNext = ["Blocked", "Done"] },
            new() { From = "Blocked", AllowedNext = ["In Progress"] },
            new() { From = "Done", AllowedNext = [] }
        ];

        return EnsureRecordsetAsync(
            context,
            "Projects",
            ownerId,
            seededAt.AddDays(-60),
            columns,
            statuses,
            transitions,
            recordset => BuildProjectRecords(recordset, random, seededAt, 96),
            cancellationToken);
    }

    private Task<SeededRecordset> EnsureInventoryRecordsetAsync(
        SeedContext context,
        UlidId ownerId,
        Random random,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        Status[] statuses =
        [
            CreateStatus("Available", "#8BC34A"),
            CreateStatus("In Use", "#29B6F6"),
            CreateStatus("Maintenance", "#FFC107"),
            CreateStatus("Retired", "#BDBDBD")
        ];

        Column[] columns =
        [
            TextColumn("Asset Tag", true, "assetTag", regex: "^AST-\\d{4}$"),
            TextColumn("Category", true, "category", InventoryCategories),
            TextColumn("Location", storageKey: "location"),
            TextColumn("Assigned To", storageKey: "assignedTo"),
            DateColumn("Purchase Date", "purchaseDate"),
            DateColumn("Warranty Expiration", "warrantyExpiration"),
            NumberColumn("Quantity", "quantity", 1, 50),
            NumberColumn("Cost", "cost", 50m, 5000m),
            TextColumn("Vendor", storageKey: "vendor"),
            TextColumn("Notes", storageKey: "notes")
        ];

        StatusTransition[] transitions =
        [
            new() { From = "Available", AllowedNext = ["In Use", "Maintenance", "Retired"] },
            new() { From = "In Use", AllowedNext = ["Maintenance", "Retired"] },
            new() { From = "Maintenance", AllowedNext = ["Available", "Retired"] },
            new() { From = "Retired", AllowedNext = [] }
        ];

        return EnsureRecordsetAsync(
            context,
            "Inventory",
            ownerId,
            seededAt.AddDays(-45),
            columns,
            statuses,
            transitions,
            recordset => BuildInventoryRecords(recordset, random, seededAt, 120),
            cancellationToken);
    }

    private async Task<SeededRecordset> EnsureRecordsetAsync(
        SeedContext context,
        string name,
        UlidId ownerId,
        DateTimeOffset createdAt,
        IReadOnlyList<Column> columns,
        IReadOnlyList<Status> statuses,
        IReadOnlyList<StatusTransition> transitions,
        Func<Recordset, List<SeedRecord>> buildRecords,
        CancellationToken cancellationToken
    )
    {
        var existing = await context.RecordsetsUnitOfWork.GetRecordsetByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug("Seed recordset {Recordset} already exists", name);
            return new SeededRecordset(
                existing.Id,
                name,
                await GetSampleRecordIdsAsync(context.RecordsetsDbContext, existing.Id, cancellationToken));
        }

        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            name,
            columns,
            statuses,
            transitions,
            createdAt);

        await context.RecordsetsUnitOfWork.AddRecordsetAsync(recordset, cancellationToken);
        await context.RecordsetsUnitOfWork.SaveChangesAsync(cancellationToken);

        var records = buildRecords(recordset);
        foreach (var seedRecord in records)
        {
            context.RecordBagValidator.Validate(recordset, seedRecord.Record.Bag);
            await context.RecordsetsUnitOfWork.AddRecordAsync(
                seedRecord.Record,
                ownerId,
                seedRecord.OccurredAt,
                cancellationToken);
        }

        if (records.Count > 0)
        {
            await context.RecordsetsUnitOfWork.SaveChangesAsync(cancellationToken);
            await context.RecordsetProjectionWriter.UpdateItemCountAsync(recordset.Id, records.Count,
                cancellationToken);
            await context.RecordsetProjectionWriter.UpdateLastChangedAsync(
                recordset.Id,
                records.Max(x => x.OccurredAt),
                cancellationToken);
        }

        logger.LogInformation("Created seed recordset {Recordset} with {Count} records", name, records.Count);

        return new SeededRecordset(
            recordset.Id,
            name,
            records.Take(4).Select(x => x.Record.Id).ToArray());
    }

    private async Task EnsureStudentsNotificationsAsync(
        SeedContext context,
        UlidId tenantId,
        SeededRecordset recordset,
        User heath,
        User erika,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        var recordCreatedRule = await EnsureNotificationRuleAsync(
            context,
            "seed.students.record-created",
            tenantId,
            recordset.Id,
            heath.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.RecordCreated,
                TenantId = tenantId,
                RecordsetId = recordset.Id
            },
            [
                InAppChannel(),
                EmailChannel(RequireEmail(heath, "Heath"))
            ],
            NotificationSchedule.Immediate(),
            cancellationToken);

        var digestRule = await EnsureNotificationRuleAsync(
            context,
            "seed.students.status-digest",
            tenantId,
            recordset.Id,
            erika.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.StatusChanged,
                TenantId = tenantId,
                RecordsetId = recordset.Id
            },
            [
                EmailChannel(RequireEmail(erika, "Erika")),
                InAppChannel()
            ],
            NotificationSchedule.Weekly(new TimeOnly(9, 0), DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday),
            cancellationToken);

        var recordId = recordset.SampleRecordIds.FirstOrDefault();
        if (recordId <= 0)
        {
            logger.LogDebug("Skipping Students sample notifications because no sample records were found.");
            return;
        }

        var secondRecordId = recordset.SampleRecordIds.Skip(1).FirstOrDefault();
        if (secondRecordId <= 0)
        {
            secondRecordId = recordId;
        }

        var thirdRecordId = recordset.SampleRecordIds.Skip(2).FirstOrDefault();
        if (thirdRecordId <= 0)
        {
            thirdRecordId = recordId;
        }

        await EnsureNotificationAsync(
            context,
            "seed-students-email-delivered",
            () =>
            {
                var createdAt = seededAt.AddHours(-7);
                var notification = CreateSeedNotification(
                    NotificationTrigger.RecordCreated(tenantId, recordset.Id, recordId),
                    NotificationRecipient.Email(RequireEmail(heath, "Heath"), heath.DisplayName, heath.Id),
                    new NotificationContent
                    {
                        Subject = "New student imported",
                        Body = $"Student record #{recordId} was added to {recordset.Name}.",
                        TemplateId = "seed.students.delivered",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordset"] = recordset.Name,
                            ["recordId"] = recordId
                        }
                    },
                    NotificationSchedule.Immediate(),
                    NotificationPriority.Normal,
                    createdAt,
                    "seed-students-email-delivered",
                    recordCreatedRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(2));
                notification.RecordDeliverySuccess(createdAt.AddMinutes(4), "seed-msg-students-001");
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-students-digest-read",
            () =>
            {
                var createdAt = seededAt.AddDays(-2).AddHours(-1);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, secondRecordId, "Probation", "Active"),
                    NotificationRecipient.InApp(erika.Id),
                    new NotificationContent
                    {
                        Subject = "Student status digest",
                        Body = "Three student records changed status and are ready for review.",
                        TemplateId = "seed.students.digest",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordset"] = recordset.Name,
                            ["changes"] = 3
                        }
                    },
                    digestRule.Schedule,
                    NotificationPriority.Low,
                    createdAt,
                    "seed-students-digest-read",
                    digestRule.Id);

                notification.MarkQueued(createdAt.AddHours(1));
                notification.RecordDeliverySuccess(createdAt.AddHours(1).AddMinutes(1), "seed-inapp-students-002");
                notification.MarkRead(createdAt.AddHours(9));
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-students-digest-unread",
            () =>
            {
                var createdAt = seededAt.AddHours(-3);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, thirdRecordId, "Active", "Probation"),
                    NotificationRecipient.InApp(erika.Id),
                    new NotificationContent
                    {
                        Subject = "Advisor follow-up needed",
                        Body = "A student moved to probation and needs a follow-up note.",
                        TemplateId = "seed.students.unread",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = thirdRecordId,
                            ["fromStatus"] = "Active",
                            ["toStatus"] = "Probation"
                        }
                    },
                    NotificationSchedule.Immediate(),
                    NotificationPriority.High,
                    createdAt,
                    "seed-students-digest-unread",
                    digestRule.Id);

                notification.RecordDeliverySuccess(createdAt.AddMinutes(1), "seed-inapp-students-003");
                return notification;
            },
            cancellationToken);
    }

    private async Task EnsureProjectsNotificationsAsync(
        SeedContext context,
        UlidId tenantId,
        SeededRecordset recordset,
        User heath,
        User erika,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        var completionRule = await EnsureNotificationRuleAsync(
            context,
            "seed.projects.completion",
            tenantId,
            recordset.Id,
            erika.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.StatusChanged,
                TenantId = tenantId,
                RecordsetId = recordset.Id,
                FromValue = "In Progress",
                ToValue = "Done"
            },
            [
                EmailChannel(RequireEmail(erika, "Erika")),
                InAppChannel()
            ],
            NotificationSchedule.Delayed(TimeSpan.FromMinutes(10)),
            cancellationToken);

        var deletionRule = await EnsureNotificationRuleAsync(
            context,
            "seed.projects.deletion",
            tenantId,
            recordset.Id,
            heath.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.RecordDeleted,
                TenantId = tenantId,
                RecordsetId = recordset.Id
            },
            [
                WebhookChannel(
                    "https://hooks.records.local/projects",
                    new Dictionary<string, string> { ["Authorization"] = "Bearer seed-projects-token" }),
                InAppChannel()
            ],
            NotificationSchedule.Immediate(),
            cancellationToken);

        var recordId = recordset.SampleRecordIds.FirstOrDefault();
        if (recordId <= 0)
        {
            logger.LogDebug("Skipping Projects sample notifications because no sample records were found.");
            return;
        }

        var secondRecordId = recordset.SampleRecordIds.Skip(1).FirstOrDefault();
        if (secondRecordId <= 0)
        {
            secondRecordId = recordId;
        }

        var thirdRecordId = recordset.SampleRecordIds.Skip(2).FirstOrDefault();
        if (thirdRecordId <= 0)
        {
            thirdRecordId = recordId;
        }

        await EnsureNotificationAsync(
            context,
            "seed-projects-completion-email",
            () =>
            {
                var createdAt = seededAt.AddHours(-12);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, recordId, "In Progress", "Done"),
                    NotificationRecipient.Email(RequireEmail(erika, "Erika"), erika.DisplayName, erika.Id),
                    new NotificationContent
                    {
                        Subject = "Project shipped",
                        Body = $"Project record #{recordId} moved from In Progress to Done.",
                        TemplateId = "seed.projects.email",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = recordId,
                            ["recordset"] = recordset.Name
                        }
                    },
                    completionRule.Schedule,
                    NotificationPriority.High,
                    createdAt,
                    "seed-projects-completion-email",
                    completionRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(12));
                notification.RecordDeliverySuccess(createdAt.AddMinutes(13), "seed-msg-projects-001");
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-projects-completion-read",
            () =>
            {
                var createdAt = seededAt.AddHours(-8);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, secondRecordId, "In Progress", "Done"),
                    NotificationRecipient.InApp(erika.Id),
                    new NotificationContent
                    {
                        Subject = "Launch checklist complete",
                        Body = "A project closeout notification was delivered to Erika.",
                        TemplateId = "seed.projects.read",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = secondRecordId
                        }
                    },
                    completionRule.Schedule,
                    NotificationPriority.Normal,
                    createdAt,
                    "seed-projects-completion-read",
                    completionRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(11));
                notification.RecordDeliverySuccess(createdAt.AddMinutes(12), "seed-inapp-projects-002");
                notification.MarkRead(createdAt.AddHours(2));
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-projects-webhook-failed",
            () =>
            {
                var createdAt = seededAt.AddHours(-2);
                var notification = CreateSeedNotification(
                    NotificationTrigger.RecordDeleted(tenantId, recordset.Id, thirdRecordId),
                    NotificationRecipient.Webhook(
                        "https://hooks.records.local/projects",
                        new Dictionary<string, string> { ["Authorization"] = "Bearer seed-projects-token" }),
                    new NotificationContent
                    {
                        Subject = "Webhook retry required",
                        Body = $"Deletion payload for project record #{thirdRecordId} failed to deliver.",
                        TemplateId = "seed.projects.failed",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = thirdRecordId,
                            ["action"] = "RecordDeleted"
                        }
                    },
                    NotificationSchedule.Immediate(),
                    NotificationPriority.High,
                    createdAt,
                    "seed-projects-webhook-failed",
                    deletionRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(1));
                notification.RecordDeliveryFailure(
                    createdAt.AddMinutes(3),
                    "Simulated 503 from project webhook endpoint",
                    TimeSpan.FromMinutes(15));
                return notification;
            },
            cancellationToken);
    }

    private async Task EnsureInventoryNotificationsAsync(
        SeedContext context,
        UlidId tenantId,
        SeededRecordset recordset,
        User heath,
        User sam,
        DateTimeOffset seededAt,
        CancellationToken cancellationToken
    )
    {
        var maintenanceRule = await EnsureNotificationRuleAsync(
            context,
            "seed.inventory.maintenance",
            tenantId,
            recordset.Id,
            heath.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.StatusChanged,
                TenantId = tenantId,
                RecordsetId = recordset.Id,
                FromValue = "In Use",
                ToValue = "Maintenance"
            },
            [
                EmailChannel(RequireEmail(heath, "Heath")),
                SmsChannel("+15555551515")
            ],
            NotificationSchedule.Immediate(),
            cancellationToken);

        var lowStockRule = await EnsureNotificationRuleAsync(
            context,
            "seed.inventory.low-stock",
            tenantId,
            recordset.Id,
            sam.Id,
            new NotificationTrigger
            {
                Type = NotificationTriggerType.CustomCondition,
                TenantId = tenantId,
                RecordsetId = recordset.Id,
                ColumnName = "quantity",
                Operator = "<=",
                Value = "3"
            },
            [
                EmailChannel(RequireEmail(sam, "Sam")),
                InAppChannel()
            ],
            NotificationSchedule.Batched(TimeSpan.FromHours(4)),
            cancellationToken);

        var recordId = recordset.SampleRecordIds.FirstOrDefault();
        if (recordId <= 0)
        {
            logger.LogDebug("Skipping Inventory sample notifications because no sample records were found.");
            return;
        }

        var secondRecordId = recordset.SampleRecordIds.Skip(1).FirstOrDefault();
        if (secondRecordId <= 0)
        {
            secondRecordId = recordId;
        }

        var thirdRecordId = recordset.SampleRecordIds.Skip(2).FirstOrDefault();
        if (thirdRecordId <= 0)
        {
            thirdRecordId = recordId;
        }

        await EnsureNotificationAsync(
            context,
            "seed-inventory-sms-queued",
            () =>
            {
                var createdAt = seededAt.AddMinutes(-45);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, recordId, "In Use", "Maintenance"),
                    NotificationRecipient.Sms("+15555551515", userId: heath.Id),
                    new NotificationContent
                    {
                        Subject = "Maintenance dispatch queued",
                        Body = $"Asset record #{recordId} has a pending maintenance text alert.",
                        TemplateId = "seed.inventory.queued",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = recordId
                        }
                    },
                    NotificationSchedule.Immediate(),
                    NotificationPriority.Critical,
                    createdAt,
                    "seed-inventory-sms-queued",
                    maintenanceRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(1));
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-inventory-email-bounced",
            () =>
            {
                var createdAt = seededAt.AddHours(-18);
                var notification = CreateSeedNotification(
                    NotificationTrigger.StatusChanged(tenantId, recordset.Id, secondRecordId, "In Use", "Maintenance"),
                    NotificationRecipient.Email(RequireEmail(heath, "Heath"), heath.DisplayName, heath.Id),
                    new NotificationContent
                    {
                        Subject = "Warranty reminder bounced",
                        Body = $"Inventory record #{secondRecordId} generated a bounced email example.",
                        TemplateId = "seed.inventory.bounced",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = secondRecordId
                        }
                    },
                    NotificationSchedule.Immediate(),
                    NotificationPriority.High,
                    createdAt,
                    "seed-inventory-email-bounced",
                    maintenanceRule.Id);

                notification.MarkQueued(createdAt.AddMinutes(2));
                notification.RecordBounce(createdAt.AddMinutes(4), "Simulated mailbox unavailable");
                return notification;
            },
            cancellationToken);

        await EnsureNotificationAsync(
            context,
            "seed-inventory-low-stock-unread",
            () =>
            {
                var createdAt = seededAt.AddHours(-4);
                var notification = CreateSeedNotification(
                    new NotificationTrigger
                    {
                        Type = NotificationTriggerType.CustomCondition,
                        TenantId = tenantId,
                        RecordsetId = recordset.Id,
                        RecordId = thirdRecordId,
                        ColumnName = "quantity",
                        Operator = "<=",
                        Value = "3"
                    },
                    NotificationRecipient.InApp(sam.Id),
                    new NotificationContent
                    {
                        Subject = "Low stock review",
                        Body = "An in-app low stock alert is waiting in the queue for Sam.",
                        TemplateId = "seed.inventory.unread",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["seed"] = true,
                            ["recordId"] = thirdRecordId,
                            ["threshold"] = 3
                        }
                    },
                    lowStockRule.Schedule,
                    NotificationPriority.Normal,
                    createdAt,
                    "seed-inventory-low-stock-unread",
                    lowStockRule.Id);

                notification.MarkQueued(createdAt.AddHours(4));
                notification.RecordDeliverySuccess(createdAt.AddHours(4).AddMinutes(2), "seed-inapp-inventory-003");
                return notification;
            },
            cancellationToken);
    }

    private async Task<NotificationRule> EnsureNotificationRuleAsync(
        SeedContext context,
        string templateId,
        UlidId tenantId,
        UlidId recordsetId,
        string userId,
        NotificationTrigger trigger,
        NotificationChannelConfig[] channels,
        NotificationSchedule schedule,
        CancellationToken cancellationToken
    )
    {
        var existingId = await context.NotificationsDbContext.NotificationRules
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetId.ToString() &&
                        x.TemplateId == templateId &&
                        !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(existingId))
        {
            var existing = await context.NotificationsUnitOfWork.NotificationRules.GetByIdAsync(
                UlidId.Parse(existingId),
                cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        var rule = NotificationRule.Create(
            tenantId,
            recordsetId,
            userId,
            trigger,
            channels,
            schedule,
            templateId,
            true);

        await context.NotificationsUnitOfWork.NotificationRules.AddAsync(rule, cancellationToken);
        await context.NotificationsUnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created seed notification rule {TemplateId}", templateId);
        return rule;
    }

    private async Task EnsureNotificationAsync(
        SeedContext context,
        string correlationId,
        Func<Notification> factory,
        CancellationToken cancellationToken
    )
    {
        var exists = await context.NotificationsDbContext.Notifications
            .AsNoTracking()
            .AnyAsync(x => x.CorrelationId == correlationId, cancellationToken);

        if (exists)
        {
            return;
        }

        var notification = factory();
        await context.NotificationsUnitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await context.NotificationsUnitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created seed notification {CorrelationId}", correlationId);
    }

    private static Notification CreateSeedNotification(
        NotificationTrigger trigger,
        NotificationRecipient recipient,
        NotificationContent content,
        NotificationSchedule schedule,
        NotificationPriority priority,
        DateTimeOffset createdAt,
        string correlationId,
        UlidId notificationRuleId
    )
    {
        return Notification.Create(
            trigger,
            recipient,
            content,
            schedule,
            priority,
            createdAt,
            correlationId,
            notificationRuleId);
    }

    private static List<SeedRecord> BuildStudentRecords(
        Recordset recordset,
        Random random,
        DateTimeOffset seededAt,
        int count
    )
    {
        var records = new List<SeedRecord>(count);

        for (var i = 0; i < count; i++)
        {
            var (city, state) = Pick(random, CityStates);
            var bag = new Dictionary<string, object?>
            {
                ["status"] = PickStudentStatus(random),
                ["name"] = RandomFullName(random),
                ["address"] = RandomStreetAddress(random),
                ["city"] = city,
                ["state"] = state,
                ["zipCode"] = $"{random.Next(10000, 99999)}",
                ["dateOfBirth"] = seededAt.AddYears(-random.Next(16, 23))
                    .AddDays(-random.Next(0, 365))
                    .ToString("O"),
                ["guardianName"] = RandomFullName(random),
                ["homeroomTeacher"] = $"{Pick(random, TeacherPrefixes)} {Pick(random, LastNames)}",
                ["gpa"] = RandomDecimal(random, 2.0m, 4.0m)
            };

            records.Add(new SeedRecord(
                new Record(
                    0,
                    recordset.Id,
                    bag),
                RandomPastDate(random, seededAt, 10, 240)));
        }

        return records;
    }

    private static List<SeedRecord> BuildProjectRecords(
        Recordset recordset,
        Random random,
        DateTimeOffset seededAt,
        int count
    )
    {
        var records = new List<SeedRecord>(count);

        for (var i = 0; i < count; i++)
        {
            var bag = new Dictionary<string, object?>
            {
                ["status"] = PickProjectStatus(random),
                ["title"] = $"{Pick(random, ProjectThemes)} {Pick(random, ProjectTargets)}",
                ["summary"] = $"{Pick(random, ProjectThemes)} work will {Pick(random, ProjectOutcomes)}.",
                ["owner"] = Pick(random, ProjectOwners),
                ["dueDate"] = RandomFutureDate(random, seededAt, 14, 180).ToString("O"),
                ["priority"] = random.Next(1, 6),
                ["budget"] = RandomDecimal(random, 10000m, 250000m),
                ["tags"] = string.Join(", ", PickDistinct(random, ProjectTagsPool, random.Next(1, 4)))
            };

            records.Add(new SeedRecord(
                new Record(
                    0,
                    recordset.Id,
                    bag),
                RandomPastDate(random, seededAt, 5, 180)));
        }

        return records;
    }

    private static List<SeedRecord> BuildInventoryRecords(
        Recordset recordset,
        Random random,
        DateTimeOffset seededAt,
        int count
    )
    {
        var records = new List<SeedRecord>(count);

        for (var i = 0; i < count; i++)
        {
            var assignedTo = Chance(random, 0.65)
                ? RandomFullName(random)
                : "Unassigned";

            var bag = new Dictionary<string, object?>
            {
                ["status"] = PickInventoryStatus(random),
                ["assetTag"] = $"AST-{1000 + i:D4}",
                ["category"] = Pick(random, InventoryCategories),
                ["location"] = Pick(random, InventoryLocations),
                ["assignedTo"] = assignedTo,
                ["purchaseDate"] = RandomPastDate(random, seededAt, 120, 1600).ToString("O"),
                ["warrantyExpiration"] = RandomFutureDate(random, seededAt, 30, 1080).ToString("O"),
                ["quantity"] = random.Next(1, 51),
                ["cost"] = RandomDecimal(random, 50m, 5000m),
                ["vendor"] = Pick(random, InventoryVendors),
                ["notes"] = Chance(random, 0.45) ? Pick(random, InventoryNotes) : string.Empty
            };

            records.Add(new SeedRecord(
                new Record(
                    0,
                    recordset.Id,
                    bag),
                RandomPastDate(random, seededAt, 3, 365)));
        }

        return records;
    }

    private static async Task<int[]> GetSampleRecordIdsAsync(
        RecordsetsDbContext recordsetsDbContext,
        UlidId recordsetId,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();

        return await recordsetsDbContext.RecordsetItems
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .OrderBy(x => x.Id)
            .Take(4)
            .Select(x => (int)x.Id)
            .ToArrayAsync(cancellationToken);
    }

    private static Status CreateStatus(string name, string color)
    {
        return new Status
        {
            Name = name,
            Color = color
        };
    }

    private static Column TextColumn(
        string name,
        bool required = false,
        string? storageKey = null,
        string[]? allowedValues = null,
        string? regex = null
    )
    {
        return new Column
        {
            StorageKey = storageKey,
            Name = name,
            Type = ColumnType.Text,
            Required = required,
            AllowedValues = allowedValues,
            Regex = regex
        };
    }

    private static Column DateColumn(string name, string? storageKey = null)
    {
        return new Column
        {
            StorageKey = storageKey,
            Name = name,
            Type = ColumnType.Date
        };
    }

    private static Column NumberColumn(
        string name,
        string? storageKey = null,
        decimal? minNumber = null,
        decimal? maxNumber = null
    )
    {
        return new Column
        {
            StorageKey = storageKey,
            Name = name,
            Type = ColumnType.Number,
            MinNumber = minNumber,
            MaxNumber = maxNumber
        };
    }

    private static NotificationChannelConfig InAppChannel()
    {
        return new NotificationChannelConfig { Type = NotificationChannel.InApp };
    }

    private static NotificationChannelConfig EmailChannel(string email)
    {
        return new NotificationChannelConfig
        {
            Type = NotificationChannel.Email,
            Address = email
        };
    }

    private static NotificationChannelConfig SmsChannel(string phoneNumber)
    {
        return new NotificationChannelConfig
        {
            Type = NotificationChannel.Sms,
            Address = phoneNumber
        };
    }

    private static NotificationChannelConfig WebhookChannel(string url, Dictionary<string, string>? settings = null)
    {
        return new NotificationChannelConfig
        {
            Type = NotificationChannel.Webhook,
            Address = url,
            Settings = settings ?? new Dictionary<string, string>()
        };
    }

    private static string RequireEmail(User user, string label)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException($"{label} must have an email address for seed notifications.");
        }

        return user.Email;
    }

    private static UlidId RequireUlid(string value, string label)
    {
        if (!UlidId.TryParse(value, out var id))
        {
            throw new InvalidOperationException($"{label} id '{value}' is not a ULID.");
        }

        return id;
    }

    private static string FormatErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join("; ", errors.Select(x => $"{x.Code}: {x.Description}"));
    }

    private static T Pick<T>(Random random, IReadOnlyList<T> values)
    {
        return values[random.Next(values.Count)];
    }

    private static IReadOnlyList<string> PickDistinct(Random random, IReadOnlyList<string> values, int count)
    {
        return values
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToArray();
    }

    private static bool Chance(Random random, double probability)
    {
        return random.NextDouble() <= probability;
    }

    private static string RandomFullName(Random random)
    {
        return $"{Pick(random, FirstNames)} {Pick(random, LastNames)}";
    }

    private static string RandomStreetAddress(Random random)
    {
        return $"{random.Next(100, 9999)} {Pick(random, StreetNames)} {Pick(random, StreetSuffixes)}";
    }

    private static string PickStudentStatus(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 64 => "Active",
            < 76 => "Probation",
            < 86 => "Inactive",
            _ => "Graduated"
        };
    }

    private static string PickProjectStatus(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 28 => "Backlog",
            < 68 => "In Progress",
            < 82 => "Blocked",
            _ => "Done"
        };
    }

    private static string PickInventoryStatus(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 35 => "Available",
            < 72 => "In Use",
            < 88 => "Maintenance",
            _ => "Retired"
        };
    }

    private static decimal RandomDecimal(Random random, decimal min, decimal max)
    {
        var value = min + (decimal)random.NextDouble() * (max - min);
        return Math.Round(value, 2);
    }

    private static DateTimeOffset RandomPastDate(
        Random random,
        DateTimeOffset seededAt,
        int minDaysAgo,
        int maxDaysAgo
    )
    {
        var daysAgo = random.Next(minDaysAgo, maxDaysAgo + 1);
        var hours = random.Next(0, 24);
        var minutes = random.Next(0, 60);
        return seededAt.AddDays(-daysAgo).AddHours(-hours).AddMinutes(-minutes);
    }

    private static DateTimeOffset RandomFutureDate(
        Random random,
        DateTimeOffset seededAt,
        int minDaysAhead,
        int maxDaysAhead
    )
    {
        var daysAhead = random.Next(minDaysAhead, maxDaysAhead + 1);
        var hours = random.Next(0, 24);
        var minutes = random.Next(0, 60);
        return seededAt.AddDays(daysAhead).AddHours(hours).AddMinutes(minutes);
    }

    private sealed record SeedContext(
        UserManager<User> UserManager,
        UsersDbContext UsersDbContext,
        TenantsDbContext TenantsDbContext,
        IRecordsetsUnitOfWork RecordsetsUnitOfWork,
        RecordsetsDbContext RecordsetsDbContext,
        IRecordBagValidator RecordBagValidator,
        IRecordsetProjectionWriter RecordsetProjectionWriter,
        INotificationsUnitOfWork NotificationsUnitOfWork,
        NotificationsDbContext NotificationsDbContext
    );

    private sealed record UserSeed(string Email, string DisplayName);

    private sealed record SeedRecord(Record Record, DateTimeOffset OccurredAt);

    private sealed record SeededRecordset(UlidId Id, string Name, int[] SampleRecordIds);
}

public sealed class SeedOptions
{
    public bool Enabled { get; set; }
    public string GlobalAdminEmail { get; set; } = "admin@records.local";
    public string GlobalAdminPassword { get; set; } = "ChangeMe123!";
    public string GlobalAdminDisplayName { get; set; } = "Records Admin";
    public string? TenantName { get; set; } = "Demo Tenant";
}