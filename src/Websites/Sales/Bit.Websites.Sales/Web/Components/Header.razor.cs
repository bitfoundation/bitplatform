namespace Bit.Websites.Sales.Web.Components;

public partial class Header
{
    [AutoInject] private NavigationManager NavigationManager = default!;
    [AutoInject] private IJSRuntime JSRuntime = default!;

    public string CurrentUrl { get; set; }
    public string MenuShadowStyles { get; set; }

    protected override void OnInitialized()
    {
        MenuShadowStyles = "";
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        base.OnInitialized();
    }

    private async Task OpenMenu()
    {
        MenuShadowStyles = "display:flex;";
        await JSRuntime.InvokeVoidAsync("App.setBodyStyle", "overflow:hidden;");
        StateHasChanged();
    }

    private async Task CloseMenu()
    {
        MenuShadowStyles = "display:none";
        await JSRuntime.InvokeVoidAsync("App.setBodyStyle", "overflow:auto;");
        StateHasChanged();
    }
}
