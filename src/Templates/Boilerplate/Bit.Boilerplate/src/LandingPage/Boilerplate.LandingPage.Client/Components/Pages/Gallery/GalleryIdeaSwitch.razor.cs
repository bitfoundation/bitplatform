namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public partial class GalleryIdeaSwitch
{
    private static readonly string[] ideas =
    [
        nameof(LandingStrings.GalleryIdea1),
        nameof(LandingStrings.GalleryIdea2),
        nameof(LandingStrings.GalleryIdea3),
        nameof(LandingStrings.GalleryIdea4),
        nameof(LandingStrings.GalleryIdea5)
    ];

    [Parameter] public int Current { get; set; }
}
