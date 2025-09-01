using FluentStorage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Services;

public partial class FluentStorageHealthCheck : IHealthCheck
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private ServerApiSettings settings = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await blobStorage.ExistsAsync(settings.UserProfileImagesDir, cancellationToken) is false)
            {
                await blobStorage.CreateFolderAsync(settings.UserProfileImagesDir, cancellationToken: cancellationToken);
            }

            return HealthCheckResult.Healthy("Storage is healthy");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("Storage is unhealthy", exp);
        }
    }
}
