using Bit.Belemetry;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class ConsentBanner : IDisposable
{
    private const BelemetryFeatures PerformanceFeatures = BelemetryFeatures.Logs | BelemetryFeatures.Traces | BelemetryFeatures.Dependencies | BelemetryFeatures.Monitoring;

    private const BelemetryFeatures AnalyticsFeatures = BelemetryFeatures.PageViews | BelemetryFeatures.Interactions | BelemetryFeatures.Sessions | BelemetryFeatures.Events;

    [AutoInject] private IBelemetryConsent consent = default!;
    [AutoInject] private LandingConsentPrompt prompt = default!;

    [Parameter] public string? SitePath { get; set; }

    private bool isVisible;
    private bool isChoosing;
    private bool performance = true;
    private bool analytics = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            prompt.Requested += Open;

            await consent.RefreshAsync();

            if (consent.IsDecided is false)
            {
                isVisible = true;
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void Open()
    {
        performance = consent.IsGranted(BelemetryFeatures.Performance);
        analytics = consent.IsGranted(BelemetryFeatures.PageViews);
        isChoosing = true;
        isVisible = true;
        InvokeAsync(StateHasChanged);
    }

    private Task Accept() => Decide(PerformanceFeatures | AnalyticsFeatures);

    private Task Reject() => Decide(BelemetryFeatures.None);

    private Task Save() => Decide((performance ? PerformanceFeatures : BelemetryFeatures.None) | (analytics ? AnalyticsFeatures : BelemetryFeatures.None));

    private async Task Decide(BelemetryFeatures features)
    {
        isVisible = false;
        isChoosing = false;

        // Errors stay on whatever the visitor picks, as the legitimate interest the privacy page states
        await consent.SetAsync(features | BelemetryFeatures.Errors);
    }

    public void Dispose() => prompt.Requested -= Open;
}
