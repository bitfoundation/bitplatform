
namespace Boilerplate.Client.Core.Components;

public partial class LoadingComponent
{
    [Parameter] public string Color { get; set; } = "#123456";

    /// <summary>
    /// Renders the Loading component in full-screen mode that renders over all other components.
    /// </summary>
    [Parameter] public bool FullScreen { get; set; }


    /// <summary>
    /// Since we are using this component in multiple places with different circumstances,
    /// we have to use different z-index value for the root of the component.
    /// 
    /// 1. The first place that utilizes this loading component is the App.razor for non-prerendered apps
    /// to show a minimum content before the Blazor runtime starts and tries to render other components.
    /// In this state the loading will render with the z-index of -1 so after the Blazor renders the actual
    /// components of the app it will vanish under the main content.
    /// 
    /// 2. The next place that this component gets rendered, is in the Navigating parameter of the Blazor Router.
    /// In this location the component needs to be rendered with a high value of the z-index so it can be shown
    /// over other components that are already rendered. we are using the OnAfterRender lifecycle method to apply
    /// the new value of the z-index since in this state unlike the App.razor the component will have its Blazor
    /// lifecycle running and active.
    /// </summary>
    private int zIndex = -1;
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            zIndex = 999999;
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
