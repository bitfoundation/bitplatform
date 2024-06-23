namespace Boilerplate.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    protected override void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        base.Handle(exception, parameters);
    }
}
