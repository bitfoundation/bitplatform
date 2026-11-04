namespace Boilerplate.Workbench.Components.Pages.LandingPages.Minimal;

/// <summary>The consent banner: opened by the footer's privacy settings, or by itself on a first visit, which the page's switch shows.</summary>
public partial class MinimalConsentBanner
{
    [AutoInject] private MinimalLandingStore store = default!;

    [Parameter] public EventCallback OnPrivacyLink { get; set; }

    private bool IsVisible => store.IsConsentOpen || (Scenario.GetSwitch("consent") is "first-visit" && store.IsConsentDecided is false);

    protected override void OnInitialized()
    {
        Subscribe(store);

        base.OnInitialized();
    }
}
