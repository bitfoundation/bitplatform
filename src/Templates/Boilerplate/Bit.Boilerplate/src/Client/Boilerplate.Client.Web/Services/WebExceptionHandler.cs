namespace Boilerplate.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    protected override void LogError(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.LogError(exception, parameters);
    }
}
