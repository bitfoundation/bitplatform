using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// Checks underlying S3, Azure blob storage, or local file system storage is healthy.
/// </summary>
public partial class AppStorageHealthCheck : IHealthCheck
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private ServerApiSettings settings = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await blobStorage.ExistsAsync(settings.UserProfileImagesDir, cancellationToken);

            return HealthCheckResult.Healthy("Storage is healthy");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("Storage is unhealthy", exp);
        }
    }
}
