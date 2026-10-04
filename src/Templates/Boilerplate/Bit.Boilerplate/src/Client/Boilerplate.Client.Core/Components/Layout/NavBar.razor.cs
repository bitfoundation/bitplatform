namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavBar
{
    private ElementReference sectionRef;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoidAsync("App.trackHeight", sectionRef, "--app-nav-bar-height");
    }
}
