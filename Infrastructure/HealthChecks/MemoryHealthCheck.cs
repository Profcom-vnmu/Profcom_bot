using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace StudentUnionBot.Infrastructure.HealthChecks;

/// <summary>
/// Health check для моніторингу використання пам'яті
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private const long WarningThresholdBytes = 1024L * 1024 * 1024 * 2; // 2 GB
    private const long CriticalThresholdBytes = 1024L * 1024 * 1024 * 3; // 3 GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var gcTotalMemory = GC.GetTotalMemory(false);

            var workingSetMb = workingSet / (1024.0 * 1024.0);
            var privateMemoryMb = privateMemory / (1024.0 * 1024.0);
            var gcMemoryMb = gcTotalMemory / (1024.0 * 1024.0);

            var data = new Dictionary<string, object>
            {
                { "working_set_mb", Math.Round(workingSetMb, 2) },
                { "private_memory_mb", Math.Round(privateMemoryMb, 2) },
                { "gc_memory_mb", Math.Round(gcMemoryMb, 2) },
                { "gc_gen0_collections", GC.CollectionCount(0) },
                { "gc_gen1_collections", GC.CollectionCount(1) },
                { "gc_gen2_collections", GC.CollectionCount(2) }
            };

            // Критичний рівень
            if (workingSet > CriticalThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Memory usage is critical: {workingSetMb:F2} MB",
                    data: data));
            }

            // Попередження
            if (workingSet > WarningThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Memory usage is high: {workingSetMb:F2} MB",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage is normal: {workingSetMb:F2} MB",
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Memory check failed",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }));
        }
    }
}
