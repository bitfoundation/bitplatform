namespace Bit.BlazorUI.Demo.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        if (exception is TaskCanceledException)
        {
            return;
        }

        base.Handle(exception, parameters);
    }
}
