using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LanguageSuggestion
{
    private const string DismissedKey = "lp-language-suggestion-dismissed";

    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private LocalStorage localStorage = default!;

    private CultureInfo? suggested;
    private string? target;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && LandingCultures.InvariantGlobalization is false && await localStorage.GetItem(DismissedKey) is null)
        {
            var preferred = (await navigator.GetLanguages())
                .Select(language => LandingCultures.Supported.Select(s => s.Culture).FirstOrDefault(c => language.StartsWith(c.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(culture => culture is not null);

            if (preferred is not null && preferred.Name != CurrentCulture.Name)
            {
                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var (_, path) = LandingCultures.Split(uri.AbsolutePath);

                suggested = preferred;
                target = $"{LandingCultures.Localize(path, preferred)}{uri.Query}";
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Dismiss()
    {
        suggested = null;
        await localStorage.SetItem(DismissedKey, "true");
    }
}
