namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public sealed record GallerySample(LandingSite Site, IStringLocalizer Texts, string Mode, string Url)
{
    public string Key => Site.Key;

    public string CardIndustry => Texts[nameof(CardIndustry)];

    public string CardSummary => Texts[nameof(CardSummary)];

    public string CardTag1 => Texts[nameof(CardTag1)];

    public string CardTag2 => Texts[nameof(CardTag2)];

    public string CardTag3 => Texts[nameof(CardTag3)];

    public string[] Tags => [CardTag1, CardTag2, CardTag3];

    public string ModeClass => Site.Mode switch
    {
        LandingLaunchMode.Launched => "is-launched",
        LandingLaunchMode.Staged => "is-staged",
        _ => "is-pre-launch"
    };

    public string Shot(string scheme) => $"images/gallery/{Site.Key}-{scheme}{(CultureInfo.CurrentUICulture.Name == LandingCultures.Default.Name ? null : $"-{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}")}.jpg";
}
