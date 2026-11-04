namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public partial class GalleryScoreRing
{
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    [Parameter] public int Index { get; set; }
}
