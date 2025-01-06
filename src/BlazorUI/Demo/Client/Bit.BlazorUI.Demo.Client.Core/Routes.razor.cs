namespace Bit.BlazorUI.Demo.Client.Core;

public partial class Routes
{
    private bool _showException;

    [AutoInject] private BitExtraServices _bitExtraServices = default!;
    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    protected override async Task OnInitializedAsync()
    {
#if DEBUG
        _showException = true;
#endif

        await _bitExtraServices.AddRootCssClasses();

        await base.OnInitializedAsync();
    }

    private void HandleOnError(Exception ex)
    {
        _exceptionHandler.Handle(ex);
    }
}
