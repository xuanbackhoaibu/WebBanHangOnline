using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebBanHangOnline.Services;

public sealed class HangfireStorageHealthCheck : IHealthCheck
{
    private readonly JobStorage _jobStorage;

    public HangfireStorageHealthCheck(JobStorage jobStorage)
    {
        _jobStorage = jobStorage;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var monitoringApi = _jobStorage.GetMonitoringApi();
            var servers = monitoringApi.Servers();

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Hangfire storage reachable. Servers: {servers.Count}."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Hangfire storage is not reachable.",
                exception));
        }
    }
}
