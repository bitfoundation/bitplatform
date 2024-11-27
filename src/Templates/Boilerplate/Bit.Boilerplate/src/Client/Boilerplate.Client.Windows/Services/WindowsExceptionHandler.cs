namespace Boilerplate.Client.Windows.Services;

public partial class WindowsExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, bool nonInterrupting, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, nonInterrupting, parameters);
    }
}
