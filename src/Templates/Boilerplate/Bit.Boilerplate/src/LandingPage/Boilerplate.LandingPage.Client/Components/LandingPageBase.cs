namespace Boilerplate.LandingPage.Client.Components;

public partial class LandingPageBase : LandingComponentBase
{
    [Parameter] public string? Culture { get; set; }

    protected override void OnParametersSet()
    {
        if (Culture is not null && LandingCultures.IsPrefix(Culture) is false)
        {
            NavigationManager.NotFound();
        }

        base.OnParametersSet();
    }
}
