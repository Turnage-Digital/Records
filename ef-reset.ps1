#!/usr/bin/env pwsh
# Rebuilds EF migrations and databases for Records modules.
#
# Usage:
#   ./ef-reset.ps1              # Full reset: drops DB, removes migrations, regenerates migrations, applies them
#   ./ef-reset.ps1 -QuickReset  # Quick reset: drops DB, applies existing migrations (for seed data testing)
#
param(
    [switch]$QuickReset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$configuration = "Debug"
$startupProject = "src/Records.App.Server/Records.App.Server.csproj"
$migrationOutputDir = "Migrations"

$modules = @(
    @{
        Name = "Core"
        Project = "src/Modules/Core/Records.Core.Infrastructure.Sql/Records.Core.Infrastructure.Sql.csproj"
        Context = "Records.Core.Infrastructure.Sql.CoreDbContext"
        MigrationName = "InitialEventStore"
    },
    @{
        Name = "Users"
        Project = "src/Modules/Users/Records.Users.Infrastructure.Sql/Records.Users.Infrastructure.Sql.csproj"
        Context = "Records.Users.Infrastructure.Sql.UsersDbContext"
        MigrationName = "InitialUsers"
    },
    @{
        Name = "Tenants"
        Project = "src/Modules/Tenants/Records.Tenants.Infrastructure.Sql/Records.Tenants.Infrastructure.Sql.csproj"
        Context = "Records.Tenants.Infrastructure.Sql.TenantsDbContext"
        MigrationName = "InitialTenants"
    },
    @{
        Name = "Recordsets"
        Project = "src/Modules/Recordsets/Records.Recordsets.Infrastructure.Sql/Records.Recordsets.Infrastructure.Sql.csproj"
        Context = "Records.Recordsets.Infrastructure.Sql.RecordsetsDbContext"
        MigrationName = "InitialRecordsets"
    },
    @{
        Name = "Notifications"
        Project = "src/Modules/Notifications/Records.Notifications.Infrastructure.Sql/Records.Notifications.Infrastructure.Sql.csproj"
        Context = "Records.Notifications.Infrastructure.Sql.NotificationsDbContext"
        MigrationName = "InitialNotifications"
    },
    @{
        Name = "Clocks"
        Project = "src/Modules/Clocks/Records.Clocks.Infrastructure.Sql/Records.Clocks.Infrastructure.Sql.csproj"
        Context = "Records.Clocks.Infrastructure.Sql.ClocksDbContext"
        MigrationName = "InitialClocks"
    }
)

function Invoke-DotNetEf
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host "dotnet ef $( $Arguments -join ' ' )" -ForegroundColor Cyan
    & dotnet ef @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet ef exited with code $LASTEXITCODE."
    }
}

function Get-EfCommonArgs
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    $startup = if ($Module.ContainsKey("StartupProject") -and $Module.StartupProject)
    {
        $Module.StartupProject
    }
    else
    {
        $startupProject
    }

    return @(
        "--project", $Module.Project,
        "--startup-project", $startup,
        "--context", $Module.Context,
        "--configuration", $configuration,
        "--verbose"
    )
}

function Ensure-ModuleMigrations
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module
    $name = if ($Module.ContainsKey("MigrationName") -and $Module.MigrationName)
    {
        $Module.MigrationName
    }
    else
    {
        "InitialMigration"
    }
    $outputDir = if ($Module.ContainsKey("MigrationOutputDir") -and $Module.MigrationOutputDir)
    {
        $Module.MigrationOutputDir
    }
    else
    {
        $migrationOutputDir
    }

    $addArgs = @("migrations", "add", $name) + $commonArgs + @("--output-dir", $outputDir)
    Invoke-DotNetEf -Arguments $addArgs
}

function Update-ModuleDatabase
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module
    $updateArgs = @("database", "update") + $commonArgs
    Invoke-DotNetEf -Arguments $updateArgs
}

function Remove-ModuleDatabase
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $commonArgs = Get-EfCommonArgs -Module $Module
    $dropArgs = @("database", "drop") + $commonArgs + @("--force")
    Invoke-DotNetEf -Arguments $dropArgs
}

function Reset-ModuleMigrations
{
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Module
    )

    if (-not (Test-Path -Path $Module.Project))
    {
        Write-Warning "Skipping $( $Module.Name ) – project '$( $Module.Project )' not found."
        return
    }

    $moduleOutputDir = if ($Module.ContainsKey("MigrationOutputDir") -and $Module.MigrationOutputDir)
    {
        $Module.MigrationOutputDir
    }
    else
    {
        $migrationOutputDir
    }

    $migrationDir = Join-Path (Split-Path $Module.Project) $moduleOutputDir
    if (Test-Path -Path $migrationDir)
    {
        Write-Host "Removing existing migrations for $( $Module.Name )..." -ForegroundColor Yellow
        Remove-Item -Path $migrationDir -Recurse -Force
    }
}

$databaseDropped = $false
$processedModules = @()

if ($QuickReset)
{
    Write-Host "Quick reset mode: Dropping database and applying existing migrations (no migration regeneration)." -ForegroundColor Green
}
else
{
    Write-Host "Full reset mode: Dropping database, removing migrations, and regenerating from scratch." -ForegroundColor Green
}

foreach ($module in $modules)
{
    if (-not (Test-Path -Path $module.Project))
    {
        Write-Warning "Skipping $( $module.Name ) – project '$( $module.Project )' not found."
        continue
    }

    if (-not $databaseDropped)
    {
        Remove-ModuleDatabase -Module $module
        $databaseDropped = $true
    }
    else
    {
        Write-Host "Database already dropped – skipping drop for $( $module.Name )." -ForegroundColor Yellow
    }

    if ($QuickReset)
    {
        # Quick reset: Just apply existing migrations without regenerating them
        Update-ModuleDatabase -Module $module
    }
    else
    {
        # Full reset: Remove old migrations, create new ones, then apply
        Reset-ModuleMigrations -Module $module
        Ensure-ModuleMigrations -Module $module
        Update-ModuleDatabase -Module $module
    }

    $processedModules += $module.Name
}

if (-not $processedModules)
{
    Write-Warning "No modules were processed. Ensure module project paths are correct."
}
