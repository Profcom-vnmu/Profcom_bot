using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.HealthChecks;

/// <summary>
/// Health check для перевірки файлового сховища
/// </summary>
public class FileStorageHealthCheck : IHealthCheck
{
    private readonly IFileStorageService _fileStorage;

    public FileStorageHealthCheck(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Перевіряємо доступність storage
            var storageInfo = await _fileStorage.GetStorageInfoAsync(cancellationToken);

            var totalSpaceGb = storageInfo.TotalSpace / (1024.0 * 1024.0 * 1024.0);
            var freeSpaceGb = storageInfo.FreeSpace / (1024.0 * 1024.0 * 1024.0);
            var usedSpaceGb = totalSpaceGb - freeSpaceGb;
            var usagePercentage = (usedSpaceGb / totalSpaceGb) * 100;

            var data = new Dictionary<string, object>
            {
                { "total_space_gb", Math.Round(totalSpaceGb, 2) },
                { "free_space_gb", Math.Round(freeSpaceGb, 2) },
                { "used_space_gb", Math.Round(usedSpaceGb, 2) },
                { "usage_percentage", Math.Round(usagePercentage, 2) },
                { "file_count", storageInfo.FileCount }
            };

            // Degraded якщо залишилось менше 10% вільного місця
            if (usagePercentage > 90)
            {
                return HealthCheckResult.Degraded(
                    $"File storage usage is high: {usagePercentage:F2}%",
                    data: data);
            }

            // Unhealthy якщо залишилось менше 5% вільного місця
            if (usagePercentage > 95)
            {
                return HealthCheckResult.Unhealthy(
                    $"File storage is critically low: {usagePercentage:F2}%",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "File storage is healthy",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "File storage check failed",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}
