namespace Boilerplate.Client.Web.Services;

public partial class WebExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, displayKind, parameters);
    }
}
