namespace Bit.BlazorUI.Demo.Client.Core;

public partial class Routes
{
    [AutoInject] BitExtraServices bitExtraServices = default!;

    protected override async Task OnInitializedAsync()
    {
        await bitExtraServices.AddRootCssClasses();

        await base.OnInitializedAsync();
    }
}
