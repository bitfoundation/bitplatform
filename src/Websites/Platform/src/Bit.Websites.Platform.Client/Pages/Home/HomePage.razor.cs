using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class HomePage
{
    private const string OUR_PRODUCTS_SECTION_ID = "our-products";
    private const string HERO_INTRO_STATE_KEY = "home-hero-intro";

    // The hero pill announces the release the site is built with. The site takes the bit packages of the
    // current release (see the csproj), and BlazorUI's Info.Version is a ReleaseVersionLiteral that its build
    // checks against Bit.Build.props, so the pill follows every version bump - preview to stable, and on to
    // the next major - with nothing here to update. Each bit major targets the .NET of the same number.
    private static readonly string releaseMajor = Bit.BlazorUI.Info.Version.Split('.')[0];
    private static readonly bool isPreviewRelease = Bit.BlazorUI.Info.Version.Contains("-pre-");

    private bool animateHeroIntro = true;

    private ElementReference ourProductsRef = default!;

    protected override async Task OnInitAsync()
    {
        // The prerendered markup plays the hero intro the moment it paints, and then the wasm app comes up,
        // throws that DOM away and rebuilds it - which replayed the animation from the start.
        // Prerender state is handed back to the client exactly once, on that boot render, so getting a value
        // out of it is the signal that the intro has already been seen and this render has to skip it.
        // Later client-side navigations back to the home page find nothing stored and animate as usual.
        var restoredFromPrerenderState = true;
        await PrerenderStateService.GetValue(HERO_INTRO_STATE_KEY, () =>
        {
            restoredFromPrerenderState = false;
            return Task.FromResult(true);
        });

        animateHeroIntro = restoredFromPrerenderState is false;
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(OUR_PRODUCTS_SECTION_ID))
        {
            await ourProductsRef.ScrollIntoView();
        }
    }

    private async Task ScrollToProducts()
    {
        await ourProductsRef.ScrollIntoView(new ScrollIntoViewOptions { Behavior = ScrollBehavior.Smooth });
    }
}
