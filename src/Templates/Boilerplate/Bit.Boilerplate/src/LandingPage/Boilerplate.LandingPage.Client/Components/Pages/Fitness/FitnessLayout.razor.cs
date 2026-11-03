namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    private LandingLink[] links = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(F[nameof(FitnessStrings.NavScreens)], $"{Page(string.Empty)}#screens"),
            new(F[nameof(FitnessStrings.NavWhatsNew)], $"{Page(string.Empty)}#whats-new"),
            new(F[nameof(FitnessStrings.NavDevices)], $"{Page(string.Empty)}#download"),
            new(F[nameof(FitnessStrings.NavFaq)], $"{Page(string.Empty)}#faq"),
            new(L[nameof(LandingStrings.About)], Page("/about"))
        ];

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{FitnessSite.Path}{path}", CultureInfo.CurrentUICulture);
}
