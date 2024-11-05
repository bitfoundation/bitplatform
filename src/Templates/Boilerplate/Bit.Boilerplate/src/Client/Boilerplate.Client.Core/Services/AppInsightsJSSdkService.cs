using BlazorApplicationInsights;
using BlazorApplicationInsights.Models;
using BlazorApplicationInsights.Interfaces;

namespace Boilerplate.Client.Core.Services;

public partial class AppInsightsJsSdkService : IApplicationInsights
{
    private readonly ApplicationInsights applicationInsights = new();
    private TaskCompletionSource? appInsightsReady;

    [AutoInject] private IJSRuntime jsRuntime = default!;

    public async Task AddTelemetryInitializer(TelemetryItem telemetryItem)
    {
        await EnsureReady();
        await applicationInsights.AddTelemetryInitializer(telemetryItem);
    }

    public async Task ClearAuthenticatedUserContext()
    {
        await EnsureReady();
        await applicationInsights.ClearAuthenticatedUserContext();
    }

    public async Task<TelemetryContext> Context()
    {
        await EnsureReady();
        return await applicationInsights.Context();
    }

    public async Task Flush()
    {
        await EnsureReady();
        await applicationInsights.Flush();
    }

    public CookieMgr GetCookieMgr()
    {
        if (appInsightsReady?.Task.IsCompleted is false)
            throw new InvalidOperationException("app insights is not ready");

        return applicationInsights.GetCookieMgr();
    }

    public void InitJSRuntime(IJSRuntime jSRuntime)
    {
        applicationInsights.InitJSRuntime(jSRuntime);
    }

    public async Task SetAuthenticatedUserContext(string authenticatedUserId, string? accountId = null, bool? storeInCookie = null)
    {
        await EnsureReady();
        await applicationInsights.SetAuthenticatedUserContext(authenticatedUserId, accountId, storeInCookie);
    }

    public async Task StartTrackEvent(string name)
    {
        await EnsureReady();
        await applicationInsights.StartTrackEvent(name);
    }

    public async Task StartTrackPage(string? name = null)
    {
        await EnsureReady();
        await applicationInsights.StartTrackPage(name);
    }

    public async Task StopTrackEvent(string name, Dictionary<string, object?>? properties = null, Dictionary<string, decimal>? measurements = null)
    {
        await EnsureReady();
        await applicationInsights.StopTrackEvent(name, properties, measurements);
    }

    public async Task StopTrackPage(string? name = null, string? url = null, Dictionary<string, object?>? customProperties = null, Dictionary<string, decimal>? measurements = null)
    {
        await EnsureReady();
        await applicationInsights.StopTrackPage(name, url, customProperties, measurements);
    }

    public async Task TrackDependencyData(DependencyTelemetry dependency)
    {
        await EnsureReady();
        await applicationInsights.TrackDependencyData(dependency);
    }

    public async Task TrackEvent(EventTelemetry @event)
    {
        await EnsureReady();
        await applicationInsights.TrackEvent(@event);
    }

    public async Task TrackException(ExceptionTelemetry exception)
    {
        await EnsureReady();
        await applicationInsights.TrackException(exception);
    }

    public async Task TrackMetric(MetricTelemetry metric)
    {
        await EnsureReady();
        await applicationInsights.TrackMetric(metric);
    }

    public async Task TrackPageView(PageViewTelemetry? pageView = null)
    {
        await EnsureReady();
        await applicationInsights.TrackPageView(pageView);
    }

    public async Task TrackPageViewPerformance(PageViewPerformanceTelemetry pageViewPerformance)
    {
        await EnsureReady();
        await applicationInsights.TrackPageViewPerformance(pageViewPerformance);
    }

    public async Task TrackTrace(TraceTelemetry trace)
    {
        await EnsureReady();
        await applicationInsights.TrackTrace(trace);
    }

    public async Task UpdateCfg(Config newConfig, bool mergeExisting = true)
    {
        await EnsureReady();
        await applicationInsights.UpdateCfg(newConfig, mergeExisting);
    }

    private Task EnsureReady()
    {
        async void CheckForAppInsightsReady()
        {
            while (true)
            {
                try
                {
                    var appInsightsVersion = await jsRuntime!.InvokeAsync<int>("eval", "window.appInsights.version");
                    appInsightsReady.SetResult();
                    break;
                }
                catch { await Task.Delay(250); }
            }
        }

        if (appInsightsReady is null)
        {
            appInsightsReady = new TaskCompletionSource();
            CheckForAppInsightsReady();
        }

        return appInsightsReady!.Task;
    }
}
