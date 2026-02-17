# Performance Smoke Harness

Use `scripts/perf-smoke.ps1` for quick endpoint latency/error baselines in local or CI-like environments.

## Basic usage

- Run against local host defaults:
  - `./scripts/perf-smoke.ps1`
- Run against multiple endpoints:
  - `./scripts/perf-smoke.ps1 -BaseUrl "http://localhost:8080" -Endpoints "/health","/metrics"`
- Run authenticated endpoints:
  - `./scripts/perf-smoke.ps1 -Endpoints "/api/recordsets?page=0&pageSize=10" -BearerToken "<token>"`
- Fail build/run on non-2xx responses:
  - `./scripts/perf-smoke.ps1 -Endpoints "/health","/metrics" -FailOnErrors`

## Output

For each endpoint, the script reports:
- request count
- success rate
- average/min/max latency
- p50 and p95 latency
- failure count

This is a smoke-level baseline tool, not a full load-testing system.
