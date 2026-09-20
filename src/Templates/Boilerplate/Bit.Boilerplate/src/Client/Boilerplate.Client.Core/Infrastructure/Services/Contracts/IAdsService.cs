namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

public interface IAdsService
{
    /// <summary>
    /// Loads the ad script and defines the rewarded slot. It reports every outcome through its result instead of
    /// throwing, because the only failure it could throw - a cancellation - is one the client exception handlers
    /// ignore, which would leave the caller with no way to tell "still loading" from "will never load".
    /// </summary>
    Task<AdInitResult> Init(string adUnitPath);

    Task<AdWatchResult> Watch();
}

public enum AdInitResult
{
    /// <summary>
    /// An ad is loaded and <see cref="IAdsService.Watch"/> can be called.
    /// </summary>
    Ready,
    /// <summary>
    /// The user has not agreed to <see cref="ConsentCategory.Advertising"/>, so nothing was loaded. Reported rather
    /// than thrown like every other outcome here, so a caller can offer the choice instead of showing a failure.
    /// </summary>
    ConsentRequired,
    /// <summary>
    /// The ad script could not be loaded: offline, an ad blocker, or a proxy blocking the ad server.
    /// </summary>
    ScriptFailed,
    /// <summary>
    /// Rewarded ads are not supported here: the out of page slot could not be defined.
    /// </summary>
    NotSupported,
    /// <summary>
    /// The ad server had nothing to serve for this ad unit, or did not answer in time.
    /// </summary>
    NotAvailable
}

public enum AdWatchResult
{
    Rewarded,
    Failed
}
