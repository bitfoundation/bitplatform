namespace Boilerplate.Workbench.Components.Pages.LandingPages.Minimal;

/// <summary>
/// What a visitor changes on the landing page: the open menu, the waitlist form and the line typed in the phone's demo.
/// It is persisted per view, so a reload or a screenshot shows the page as it was left.
/// </summary>
[BlexStore(Name = "landing-minimal", Persist = true)]
public partial class MinimalLandingStore
{
    [BlexState] private bool _isMenuOpen;
    [BlexState] private string? _email;
    [BlexState] private string[] _offPlatforms = [];
    // The visitor's place on the waitlist; null until they join.
    [BlexState] private int? _position;
    [BlexState] private string? _line;
    [BlexState] private string? _savedLine;
    [BlexState] private bool _isConsentOpen;
    [BlexState] private bool _isConsentChoosing;
    [BlexState] private bool _isConsentDecided;
    [BlexState] private bool _wantsPerformance = true;
    [BlexState] private bool _wantsAnalytics = true;

    [BlexAction] private void OnShowMenu(bool isOpen) => IsMenuOpen = isOpen;

    [BlexAction] private void OnTypeEmail(string? email) => Email = email;

    [BlexAction]
    private void OnChoosePlatform(string platform, bool isChosen) => OffPlatforms = isChosen ? [.. OffPlatforms.Where(p => p != platform)] : [.. OffPlatforms, platform];

    [BlexAction] private void OnJoin(int position) => Position = position;

    [BlexAction] private void OnTypeLine(string? line) => Line = line;

    [BlexAction] private void OnSaveLine(string line) => SavedLine = line;

    [BlexAction]
    private void OnOpenConsent()
    {
        IsConsentOpen = true;
        IsConsentChoosing = true;
    }

    [BlexAction] private void OnChooseConsent() => IsConsentChoosing = true;

    [BlexAction] private void OnWantPerformance(bool wanted) => WantsPerformance = wanted;

    [BlexAction] private void OnWantAnalytics(bool wanted) => WantsAnalytics = wanted;

    [BlexAction]
    private void OnDecideConsent(bool performance, bool analytics)
    {
        WantsPerformance = performance;
        WantsAnalytics = analytics;
        IsConsentDecided = true;
        IsConsentOpen = false;
        IsConsentChoosing = false;
    }
}
