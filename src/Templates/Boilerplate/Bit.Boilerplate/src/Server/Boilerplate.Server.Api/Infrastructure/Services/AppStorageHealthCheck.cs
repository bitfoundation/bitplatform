using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.Services;

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
            var result = await blobStorage.ListAsync(new()
            {
                FolderPath = settings.UserProfileImagesDir,
                MaxResults = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy("Storage is healthy");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("Storage is unhealthy", exp);
        }
    }
}
