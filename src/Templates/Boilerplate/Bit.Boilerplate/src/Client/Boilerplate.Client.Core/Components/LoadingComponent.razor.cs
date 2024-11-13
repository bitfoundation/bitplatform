
namespace Boilerplate.Client.Core.Components;

public partial class LoadingComponent
{
    [Parameter] public string Color { get; set; } = "#123456";

    [Parameter] public bool FullScreen { get; set; }


    private int zIndex = -1;
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            zIndex = 999999;
            StateHasChanged();
        }
        return base.OnAfterRenderAsync(firstRender);
    }
}
