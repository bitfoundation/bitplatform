namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public partial class GallerySpotlight
{
    private string? selected;

    [Parameter] public GallerySample[] Samples { get; set; } = [];

    protected override void OnParametersSet()
    {
        if (Samples.Any(s => s.Key == selected) is false)
        {
            selected = Samples.FirstOrDefault()?.Key;
        }

        base.OnParametersSet();
    }
}
