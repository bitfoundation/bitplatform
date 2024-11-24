
namespace Boilerplate.Client.Core.Components;

public partial class LoadingComponent
{
    [Parameter] public string Color { get; set; } = "#123456";

    /// <summary>
    /// Renders the Loading component in full-screen mode that renders over all other components.
    /// </summary>
    [Parameter] public bool FullScreen { get; set; }

    /// <summary> 
    /// This component is used in different parts of the application under varying conditions.
    ///
    /// 1. In App.razor, for non-prerendered applications, this component displays minimal content before
    /// Blazor fully loads. Since it doesn’t automatically get removed after the app loads, the z-index is set to -1 
    /// to ensure that once Blazor renders the main components, the loader falls behind the main content and becomes hidden.
    /// Additionally, the z-index will not change because @rendermode in App.razor is null, which prevents the OnAfterRender 
    /// method from being invoked.
    ///
    /// 2. In other parts of the project, like during Authorizing and Navigating, the component is automatically 
    /// removed from the screen, so a negative z-index is unnecessary and could actually cause it to be invisible. 
    /// In these cases, it needs a higher z-index to ensure it appears above other components. The new z-index value 
    /// is applied in the OnAfterRender lifecycle method.
    /// </summary>
    private string zIndex = "-1";
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            zIndex = "999999";
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
