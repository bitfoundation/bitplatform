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
    /// What became of a restart attempt. The two failures need different things of the caller, so they are told
    /// apart: an app that was never stopped still holds whatever it held, and waiting is the way back; one that
    /// stopped and did not answer again is simply down, and no amount of waiting helps.
    /// </summary>
    /// <param name="Stopped">Whether the old process actually went away.</param>
    /// <param name="Refusal">Why the app is not serving from a fresh process, or null when it is.</param>
    public readonly record struct RestartResult(bool Stopped, string? Refusal)
    {
        public bool Restarted => Refusal is null;
    }

    /// <summary>
    /// Stops the app serving <paramref name="healthAddress"/> and returns once a new process answers there again.
    /// A refusal says why that did not happen: the site folder belongs to the deployment, so a run that cannot write
    /// into it has to cope without a restart.
    /// </summary>
    public static async Task<RestartResult> TryRestart(string sitePath, Uri healthAddress, CancellationToken cancellationToken)
    {
        var appOffline = Path.Combine(sitePath, appOfflineFileName);

        try
        {
            await File.WriteAllTextAsync(appOffline, $"Stopped by {nameof(DeployedSite)} at {DateTimeOffset.Now:O}.", cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(Stopped: false, $"could not write {appOffline}: {exception.Message}");
        }

        bool stopped;
        try
        {
            // The old process has to be gone before the file is removed, or the next request reaches it and nothing
            // was reset.
            stopped = await PollUntil(healthAddress, status => status is HttpStatusCode.ServiceUnavailable, stopDeadline, cancellationToken);
        }
        finally
        {
            // Whatever the poll above decided, the site must never be left answering 503.
            File.Delete(appOffline);
        }

        // Never observing the 503 means the file did not take - a wrong site path, or an IIS that ignored it. The old
        // process is then still answering, so the poll below succeeds at once and would report a restart that never
        // happened, leaving the caller to trust an in-memory state that was never dropped.
        if (stopped is false)
            return new(Stopped: false, $"{healthAddress} never answered {(int)HttpStatusCode.ServiceUnavailable} within {stopDeadline}, so {appOfflineFileName} did not stop the app and nothing was reset.");

        return await PollUntil(healthAddress, status => status is HttpStatusCode.OK, startDeadline, cancellationToken)
            ? new(Stopped: true, null)
            : new(Stopped: true, $"{healthAddress} never answered again within {startDeadline}.");
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
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested is false)
            {
                // The probe's own timeout: a hung site is not an answer either, and throwing here would replace the
                // caller's failure from its finally.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
        while (DateTimeOffset.UtcNow < giveUpAt);

        return false;
    }
}
