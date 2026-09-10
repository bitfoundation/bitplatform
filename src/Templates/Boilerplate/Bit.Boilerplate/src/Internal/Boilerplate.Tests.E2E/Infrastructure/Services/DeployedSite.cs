namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// The demos are IIS sites on this machine (See .github/actions/deploy-to-server), published out of process, so
/// stopping one drops whatever it held in memory - an exhausted rate limit window, a warm cache.
/// </summary>
public static class DeployedSite
{
    /// <summary>
    /// While this file is in the site folder IIS answers 503 and ANCM stops the app's process. Unlike appcmd, it
    /// needs no elevation.
    /// </summary>
    private const string appOfflineFileName = "app_offline.htm";

    private static readonly TimeSpan stopDeadline = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan startDeadline = TimeSpan.FromMinutes(2);
    private static readonly HttpClient probe = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>
    /// Stops the app serving <paramref name="healthAddress"/> and returns once a new process answers there again.
    /// Null when that happened, otherwise why it did not: the site folder belongs to the deployment, so a run that
    /// cannot write into it has to cope without a restart.
    /// </summary>
    public static async Task<string?> TryRestart(string sitePath, Uri healthAddress, CancellationToken cancellationToken)
    {
        var appOffline = Path.Combine(sitePath, appOfflineFileName);

        try
        {
            await File.WriteAllTextAsync(appOffline, $"Stopped by {nameof(DeployedSite)} at {DateTimeOffset.Now:O}.", cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"could not write {appOffline}: {exception.Message}";
        }

        try
        {
            // The old process has to be gone before the file is removed, or the next request reaches it and nothing
            // was reset.
            await PollUntil(healthAddress, status => status is HttpStatusCode.ServiceUnavailable, stopDeadline, cancellationToken);
        }
        finally
        {
            // Whatever the poll above decided, the site must never be left answering 503.
            File.Delete(appOffline);
        }

        return await PollUntil(healthAddress, status => status is HttpStatusCode.OK, startDeadline, cancellationToken)
            ? null
            : $"{healthAddress} never answered again within {startDeadline}.";
    }

    private static async Task<bool> PollUntil(Uri address, Func<HttpStatusCode, bool> isExpected, TimeSpan deadline, CancellationToken cancellationToken)
    {
        var giveUpAt = DateTimeOffset.UtcNow + deadline;

        do
        {
            try
            {
                using var response = await probe.GetAsync(address, cancellationToken);

                if (isExpected(response.StatusCode))
                    return true;
            }
            catch (HttpRequestException)
            {
                // A site in the middle of a restart refuses the connection, which is neither answer.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
        while (DateTimeOffset.UtcNow < giveUpAt);

        return false;
    }
}
