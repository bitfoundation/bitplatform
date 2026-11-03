namespace Boilerplate.LandingPage.Client.Components;

public partial class LandingPageBase : LandingComponentBase
{
    [Parameter] public string? Culture { get; set; }

    protected override void OnParametersSet()
    {
        // A route's culture segment takes any word, so one that isn't a supported prefix makes the page a 404
        if (Culture is not null && LandingCultures.IsPrefix(Culture) is false)
        {
            NavigationManager.NotFound();
        }

        base.OnParametersSet();
    }
}
