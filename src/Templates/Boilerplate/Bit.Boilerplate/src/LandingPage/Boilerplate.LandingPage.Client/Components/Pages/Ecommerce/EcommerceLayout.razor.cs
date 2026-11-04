namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public partial class EcommerceLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<EcommerceStrings> E = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(E[nameof(EcommerceStrings.NavFeatures)], Link("#features")),
            new(E[nameof(EcommerceStrings.NavCare)], Link("#care")),
            new(E[nameof(EcommerceStrings.NavReviews)], Link("#reviews")),
            new(E[nameof(EcommerceStrings.NavFaq)], Link("#faq")),
            new(L[nameof(LandingStrings.About)], LandingCultures.Localize($"{EcommerceSite.Path}/about", CultureInfo.CurrentUICulture))
        ];

        suggestions =
        [
            E[nameof(EcommerceStrings.ChatSuggestion1)],
            E[nameof(EcommerceStrings.ChatSuggestion2)],
            E[nameof(EcommerceStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(EcommerceSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
