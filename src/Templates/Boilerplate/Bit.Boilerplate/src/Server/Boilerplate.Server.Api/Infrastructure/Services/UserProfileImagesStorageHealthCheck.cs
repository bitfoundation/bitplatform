using FluentStorage.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// Checks underlying S3, Azure blob storage, or local file system storage is healthy.
/// </summary>
public partial class UserProfileImagesStorageHealthCheck : IHealthCheck
{
    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private ServerApiSettings settings = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await blobStorage.ListObjects(new()
            {
                FolderPath = settings.UserProfileImagesDir,
                MaxResults = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy("User profile images storage is healthy");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("User profile images storage is unhealthy", exp);
        }
    }
}
