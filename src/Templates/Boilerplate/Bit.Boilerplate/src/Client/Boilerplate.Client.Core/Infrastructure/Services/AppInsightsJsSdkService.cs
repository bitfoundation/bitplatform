using BlazorApplicationInsights;
using BlazorApplicationInsights.Models;
using BlazorApplicationInsights.Interfaces;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// A Blazor Hybrid / Blazor Server compatible version of <see cref="ApplicationInsights"/>
/// </summary>
public class AppInsightsJsSdkService : IApplicationInsights
{
    /// <summary>
    /// The Application Insights JS SDK is fetched from a CDN, so it can be missing for a while - or for the whole
    /// of a launch that started offline. That is transient, and it must not be remembered as a permanent failure:
    /// a failed attempt is discarded so the next telemetry call retries, rather than every later call rethrowing
    /// the first timeout for the rest of the app session.
    /// </summary>
    private static readonly TimeSpan appInsightsJsFilesLoadTimeout = TimeSpan.FromSeconds(15);

    private Task? applicationInsightsIsReady;
    private readonly SemaphoreSlim applicationInsightsIsReadyLock = new(1, 1);

    /// <summary>
    /// Initializers that have not reached the SDK yet. They are queued rather than applied directly because
    /// <see cref="AddTelemetryInitializer"/> is called fire and forget at startup, before the SDK is guaranteed to be
    /// loaded - and they are kept until the SDK acknowledges them, so neither ordering loses one: an initializer added
    /// before readiness is applied by the attempt that establishes it, and one added after readiness is applied on the
    /// spot.
    /// </summary>
    private readonly ConcurrentQueue<TelemetryItem> pendingTelemetryInitializers = new();

    /// <summary>Serializes the consent read with the write it feeds, so a decision taken mid update cannot lose to an older snapshot.</summary>
    private readonly SemaphoreSlim updateCfgLock = new(1, 1);

    private IJSRuntime jsRuntime = default!;
    private readonly TimeProvider timeProvider;
    private readonly ConsentService consentService;
    private readonly ApplicationInsights applicationInsights = new();

    /// <summary><see cref="ConsentService"/> is scoped while this is a singleton on the browser: still one instance, because client side Blazor has no DI scopes.</summary>
    public AppInsightsJsSdkService(IJSRuntime jsRuntime, TimeProvider timeProvider, ConsentService consentService)
    {
        this.timeProvider = timeProvider;
        this.consentService = consentService;
        InitJSRuntime(jsRuntime);
    }

    public CookieMgr GetCookieMgr()
    {
        return applicationInsights.GetCookieMgr();
    }

    public void InitJSRuntime(IJSRuntime jSRuntime)
    {
        this.jsRuntime = jSRuntime;
        applicationInsights.InitJSRuntime(jSRuntime);
    }

    public async Task ClearAuthenticatedUserContext()
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.ClearAuthenticatedUserContext();
    }

    public async Task<TelemetryContext> Context()
    {
        await EnsureApplicationInsightsIsReady();
        return await applicationInsights.Context();
    }

    public async Task Flush()
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.Flush();
    }

    public async Task SetAuthenticatedUserContext(string authenticatedUserId, string? accountId = null, bool? storeInCookie = null)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.SetAuthenticatedUserContext(authenticatedUserId, accountId, storeInCookie);
    }

    public async Task StartTrackEvent(string name)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.StartTrackEvent(name);
    }

    public async Task StartTrackPage(string? name = null)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.StartTrackPage(name);
    }

    public async Task StopTrackEvent(string name, Dictionary<string, object?>? properties = null, Dictionary<string, decimal>? measurements = null)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.StopTrackEvent(name, properties, measurements);
    }

    public async Task StopTrackPage(string? name = null, string? url = null, Dictionary<string, object?>? customProperties = null, Dictionary<string, decimal>? measurements = null)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.StopTrackPage(name, url, customProperties, measurements);
    }

    public async Task TrackDependencyData(DependencyTelemetry dependency)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackDependencyData(dependency);
    }

    public async Task TrackEvent(EventTelemetry @event)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackEvent(@event);
    }

    public async Task TrackException(ExceptionTelemetry exception)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackException(exception);
    }

    public async Task TrackMetric(MetricTelemetry metric)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackMetric(metric);
    }

    public async Task TrackPageView(PageViewTelemetry? pageView = null)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackPageView(pageView);
    }

    public async Task TrackPageViewPerformance(PageViewPerformanceTelemetry pageViewPerformance)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackPageViewPerformance(pageViewPerformance);
    }

    public async Task TrackTrace(TraceTelemetry trace)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.TrackTrace(trace);
    }

    /// <summary>
    /// The single writer of the switches Analytics consent controls: stamped onto every update, so ApplicationInsightsInit's
    /// <c>mergeExisting: false</c> replace cannot reset what consent granted. A caller's values for those four are overridden.
    /// </summary>
    public async Task UpdateCfg(Config newConfig, bool mergeExisting = true)
    {
        await EnsureApplicationInsightsIsReady();

        // After readiness rather than around it, which would queue every caller behind that method's own 15 second poll.
        await updateCfgLock.WaitAsync();

        try
        {
            var granted = await consentService.IsGranted(ConsentCategory.Analytics);

            // Mutating the caller's instance is safe: nothing else reads it and all four are always assigned.
            newConfig.DisableCookiesUsage = granted is false;
            newConfig.IsStorageUseDisabled = granted is false;
            newConfig.AutoTrackPageVisitTime = granted;
            newConfig.EnableAutoRouteTracking = granted;

            await applicationInsights.UpdateCfg(newConfig, mergeExisting);
        }
        finally
        {
            updateCfgLock.Release();
        }
    }

    public async Task AddTelemetryInitializer(TelemetryItem telemetryItem)
    {
        pendingTelemetryInitializers.Enqueue(telemetryItem);

        try
        {
            await EnsureApplicationInsightsIsReady();
        }
        catch
        {
            // This is called fire and forget during startup (AppClientCoordinator), so a failure here must not
            // surface as an unobserved task exception. The initializer stays queued and is applied by the retry that
            // the next Track* call drives.
        }
    }

    private async Task EnsureApplicationInsightsIsReady()
    {
        if (applicationInsightsIsReady is not { IsCompletedSuccessfully: true })
        {
            await applicationInsightsIsReadyLock.WaitAsync();

            try
            {
                // Only a *successful* attempt is kept. A failed one is replaced by a fresh attempt below, which is
                // the whole point: the previous shape stored the first failure in a one-shot TaskCompletionSource,
                // so a CDN that was merely slow disabled telemetry for the rest of the app session.
                if (applicationInsightsIsReady is not { IsCompletedSuccessfully: true })
                {
                    await (applicationInsightsIsReady = WaitForAppInsightsJsFiles());
                }
            }
            finally
            {
                applicationInsightsIsReadyLock.Release();
            }
        }

        // Deliberately outside the readiness check rather than inside the wait: readiness is established by whichever
        // call gets there first, which is routinely a Track* call that carries no initializer. Applying only there
        // would silently drop an initializer added afterwards.
        await ApplyPendingTelemetryInitializers();
    }

    private async Task ApplyPendingTelemetryInitializers()
    {
        if (pendingTelemetryInitializers.IsEmpty) return;

        await applicationInsightsIsReadyLock.WaitAsync();

        try
        {
            // Dequeued only once the SDK has taken it, so a failure leaves it queued for the next attempt rather
            // than losing it.
            while (pendingTelemetryInitializers.TryPeek(out var telemetryItem))
            {
                await applicationInsights.AddTelemetryInitializer(telemetryItem);

                pendingTelemetryInitializers.TryDequeue(out _);
            }
        }
        finally
        {
            applicationInsightsIsReadyLock.Release();
        }
    }

    private async Task WaitForAppInsightsJsFiles()
    {
        using var cts = new CancellationTokenSource(appInsightsJsFilesLoadTimeout, timeProvider);

        while (await AppInsightsJsFilesAreLoaded() is false)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), timeProvider, cts.Token);
        }
    }

    private async Task<bool> AppInsightsJsFilesAreLoaded()
    {
        return await jsRuntime.InvokeAsync<bool>("window.hasOwnProperty", "appInsights") &&
               await jsRuntime.InvokeAsync<bool>("appInsights.hasOwnProperty", "updateCfg") &&
               await jsRuntime.InvokeAsync<bool>("window.hasOwnProperty", "blazorApplicationInsights");
    }
}
