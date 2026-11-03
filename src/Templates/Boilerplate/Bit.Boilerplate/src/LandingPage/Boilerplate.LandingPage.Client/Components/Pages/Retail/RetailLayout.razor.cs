namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(R[nameof(RetailStrings.NavOffers)], Link("#offers")),
            new(R[nameof(RetailStrings.NavCollect)], Link("#collect")),
            new(R[nameof(RetailStrings.NavCard)], Link("#card")),
            new(R[nameof(RetailStrings.NavStores)], LandingCultures.Localize($"{RetailSite.Path}/stores", CultureInfo.CurrentUICulture)),
            new(L[nameof(LandingStrings.About)], LandingCultures.Localize($"{RetailSite.Path}/about", CultureInfo.CurrentUICulture))
        ];

        suggestions =
        [
            R[nameof(RetailStrings.ChatSuggestion1)],
            R[nameof(RetailStrings.ChatSuggestion2)],
            R[nameof(RetailStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(RetailSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
