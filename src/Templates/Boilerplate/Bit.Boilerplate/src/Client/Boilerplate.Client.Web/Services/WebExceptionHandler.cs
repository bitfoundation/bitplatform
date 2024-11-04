namespace Boilerplate.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    protected override void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        if (IgnoreException(exception))
            return;

        base.Handle(exception, parameters);
    }
}
