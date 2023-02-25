namespace Bit.Websites.Sales.Web.Shared;

public partial class MainLayout
{
    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }
}
