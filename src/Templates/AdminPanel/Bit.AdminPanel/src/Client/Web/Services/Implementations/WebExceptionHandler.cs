
namespace AdminPanel.Client.Web.Services.Implementations;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.Handle(exception, parameters);
    }
}
