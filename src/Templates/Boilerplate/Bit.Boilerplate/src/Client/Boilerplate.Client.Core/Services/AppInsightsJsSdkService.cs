using BlazorApplicationInsights;
using BlazorApplicationInsights.Models;
using BlazorApplicationInsights.Interfaces;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// A Blazor Hybrid / Blazor Server compatible version of <see cref="ApplicationInsights"/>
/// </summary>
public partial class AppInsightsJsSdkService : IApplicationInsights
{
    private TaskCompletionSource? telemetryInitializerIsAddedTcs;
    private readonly TaskCompletionSource appInsightsJsFilesAreLoaded = new();

    private readonly ApplicationInsights applicationInsights = new();

    [AutoInject] private IJSRuntime jsRuntime = default!;

    public CookieMgr GetCookieMgr()
    {
        return applicationInsights.GetCookieMgr();
    }

    public void InitJSRuntime(IJSRuntime jSRuntime)
    {
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

    public async Task UpdateCfg(Config newConfig, bool mergeExisting = true)
    {
        await EnsureApplicationInsightsIsReady();
        await applicationInsights.UpdateCfg(newConfig, mergeExisting);
    }

    public async Task AddTelemetryInitializer(TelemetryItem telemetryItem)
    {
        await EnsureAppInsightsJsFilesAreLoaded();
        try
        {
            await applicationInsights.AddTelemetryInitializer(telemetryItem);
            telemetryInitializerIsAddedTcs!.TrySetResult();
        }
        catch (Exception exp)
        {
            telemetryInitializerIsAddedTcs!.TrySetException(exp);
        }
    }

    private async Task EnsureApplicationInsightsIsReady()
    {
        await appInsightsJsFilesAreLoaded.Task;
        await telemetryInitializerIsAddedTcs!.Task;
    }

    private async Task EnsureAppInsightsJsFilesAreLoaded()
    {
        try
        {
            if (telemetryInitializerIsAddedTcs is not null)
                return;

            telemetryInitializerIsAddedTcs = new();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            while (true)
            {
                if (await jsRuntime.InvokeAsync<bool>("window.hasOwnProperty", "appInsights") &&
                    await jsRuntime.InvokeAsync<bool>("window.hasOwnProperty", "blazorApplicationInsights"))
                {
                    appInsightsJsFilesAreLoaded.TrySetResult();
                    break;
                }
                await Task.Delay(250, cts.Token);
            }
        }
        catch (Exception exp)
        {
            appInsightsJsFilesAreLoaded.TrySetException(exp);
        }
    }
}
