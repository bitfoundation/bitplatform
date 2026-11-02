namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LegalPage
{
    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public string? Icon { get; set; }

    [Parameter] public DateOnly UpdatedOn { get; set; } = new(2026, 11, 1);

    private LandingLegalKind kind;
    private string slug = default!;

    private int SectionCount => kind switch
    {
        LandingLegalKind.Privacy => 6,
        LandingLegalKind.Terms => 5,
        LandingLegalKind.Cookies => 3,
        LandingLegalKind.Accessibility => 3,
        _ => 0
    };

    protected override void OnParametersSet()
    {
        slug = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Segments[^1].Trim('/');
        kind = Enum.TryParse<LandingLegalKind>(slug, ignoreCase: true, out var parsed) ? parsed : LandingLegalKind.Privacy;

        base.OnParametersSet();
    }

    private string Text(string part) => Localizer[$"Legal{kind}{part}", Site.Company.LegalName, Site.Company.Email, Site.Company.Address, Site.Name];
}

public enum LandingLegalKind
{
    Privacy,
    Terms,
    Cookies,
    Imprint,
    Accessibility
}
