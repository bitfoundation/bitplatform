namespace Boilerplate.LandingPage.Client.Components.Pages.Minimal;

public partial class MinimalLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<MinimalStrings> M = default!;

    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        suggestions =
        [
            M[nameof(MinimalStrings.ChatSuggestion1)],
            M[nameof(MinimalStrings.ChatSuggestion2)],
            M[nameof(MinimalStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(MinimalSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
