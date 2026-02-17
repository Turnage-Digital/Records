#!/usr/bin/env pwsh
# Lightweight API performance smoke harness for Records.
#
# Example:
#   ./scripts/perf-smoke.ps1 -BaseUrl "http://localhost:8080" -Endpoints "/health","/metrics"
#
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string[]]$Endpoints = @("/health"),
    [int]$RequestsPerEndpoint = 30,
    [string]$BearerToken = "",
    [int]$TimeoutSeconds = 30,
    [switch]$FailOnErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($RequestsPerEndpoint -le 0)
{
    throw "RequestsPerEndpoint must be greater than 0."
}

if ($TimeoutSeconds -le 0)
{
    throw "TimeoutSeconds must be greater than 0."
}

$httpClientHandler = [System.Net.Http.HttpClientHandler]::new()
$httpClient = [System.Net.Http.HttpClient]::new($httpClientHandler)
$httpClient.Timeout = [TimeSpan]::FromSeconds($TimeoutSeconds)

if (-not [string]::IsNullOrWhiteSpace($BearerToken))
{
    $httpClient.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $BearerToken)
}

function Get-Percentile
{
    param(
        [Parameter(Mandatory = $true)]
        [double[]]$Values,
        [Parameter(Mandatory = $true)]
        [double]$Percentile
    )

    if ($Values.Count -eq 0)
    {
        return 0.0
    }

    $ordered = $Values | Sort-Object
    $index = [Math]::Ceiling(($Percentile / 100.0) * $ordered.Count) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $ordered.Count) { $index = $ordered.Count - 1 }
    return [double]$ordered[$index]
}

$results = @()

foreach ($endpoint in $Endpoints)
{
    if ([string]::IsNullOrWhiteSpace($endpoint))
    {
        continue
    }

    $uri = [System.Uri]::new($BaseUrl.TrimEnd('/') + "/" + $endpoint.TrimStart('/'))
    Write-Host "Running $RequestsPerEndpoint request(s) against $uri" -ForegroundColor Cyan

    for ($i = 1; $i -le $RequestsPerEndpoint; $i++)
    {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $statusCode = 0
        $success = $false
        $error = $null

        try
        {
            $response = $httpClient.GetAsync($uri).GetAwaiter().GetResult()
            $statusCode = [int]$response.StatusCode
            $success = $response.IsSuccessStatusCode
            $response.Dispose()
        }
        catch
        {
            $error = $_.Exception.Message
        }
        finally
        {
            $stopwatch.Stop()
        }

        $results += [pscustomobject]@{
            Endpoint = $endpoint
            DurationMs = [double]$stopwatch.Elapsed.TotalMilliseconds
            StatusCode = $statusCode
            Success = $success
            Error = $error
        }
    }
}

$summaries = @()
$failedEndpoints = @()

foreach ($endpoint in $Endpoints)
{
    $endpointResults = $results | Where-Object { $_.Endpoint -eq $endpoint }
    if ($endpointResults.Count -eq 0)
    {
        continue
    }

    $durations = @($endpointResults | ForEach-Object { [double]$_.DurationMs })
    $successCount = ($endpointResults | Where-Object { $_.Success }).Count
    $failureCount = $endpointResults.Count - $successCount
    $successRate = [Math]::Round(($successCount * 100.0) / $endpointResults.Count, 2)

    if ($failureCount -gt 0)
    {
        $failedEndpoints += $endpoint
    }

    $summaries += [pscustomobject]@{
        Endpoint = $endpoint
        Requests = $endpointResults.Count
        SuccessRate = "$successRate`%"
        AvgMs = [Math]::Round((($durations | Measure-Object -Average).Average), 2)
        MinMs = [Math]::Round((($durations | Measure-Object -Minimum).Minimum), 2)
        P50Ms = [Math]::Round((Get-Percentile -Values $durations -Percentile 50), 2)
        P95Ms = [Math]::Round((Get-Percentile -Values $durations -Percentile 95), 2)
        MaxMs = [Math]::Round((($durations | Measure-Object -Maximum).Maximum), 2)
        Failures = $failureCount
    }
}

Write-Host ""
Write-Host "Performance Smoke Summary" -ForegroundColor Green
$summaries | Format-Table -AutoSize

if ($failedEndpoints.Count -gt 0)
{
    Write-Host ""
    Write-Host "Endpoints with failures:" -ForegroundColor Yellow
    $failedEndpoints | Sort-Object -Unique | ForEach-Object { Write-Host " - $_" -ForegroundColor Yellow }

    if ($FailOnErrors)
    {
        throw "One or more endpoints returned failures."
    }
}
