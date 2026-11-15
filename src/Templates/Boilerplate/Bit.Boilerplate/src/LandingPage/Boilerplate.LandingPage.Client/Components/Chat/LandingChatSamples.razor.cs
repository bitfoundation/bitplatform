using Boilerplate.LandingPage.Client.Components.Pages.Gallery;

namespace Boilerplate.LandingPage.Client.Components.Chat;

public partial class LandingChatSamples
{
    [AutoInject] private IStringLocalizerFactory localizerFactory = default!;

    /// <summary>The keys of the samples to show, best match first. Ones no sample has are left out.</summary>
    [Parameter] public IReadOnlyList<string>? Keys { get; set; }

    private GallerySample[] samples = [];

    protected override void OnParametersSet()
    {
        samples = [.. (Keys ?? []).Select(LandingSites.Find)
                                  .OfType<LandingSite>()
                                  .DistinctBy(site => site.Key)
                                  .Select(site => new GallerySample(site, localizerFactory.Create(site.Strings), string.Empty, LocalUrl(site.Path)))];

        base.OnParametersSet();
    }
}
