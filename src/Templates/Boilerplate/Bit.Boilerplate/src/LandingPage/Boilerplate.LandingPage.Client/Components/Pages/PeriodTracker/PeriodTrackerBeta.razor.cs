namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerBeta
{
    [Parameter] public string? Campaign { get; set; }

    [Parameter] public string? Note { get; set; }

    [Parameter] public bool ShowAndroid { get; set; }

    [Parameter] public string? Class { get; set; }

    private string Url => Campaign is null ? PeriodTrackerSite.BetaUrl : $"{PeriodTrackerSite.BetaUrl}?ct={Uri.EscapeDataString(Campaign)}";
}
