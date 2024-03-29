namespace Boilerplate.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    protected override void LogError(Exception exception, IDictionary<string, object?> parameters)
    {
        base.LogError(exception, parameters);
    }
}
