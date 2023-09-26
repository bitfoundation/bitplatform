using AdminPanel.Client.Core.Services;

namespace AdminPanel.Client.Web.Services;

public partial class WebExceptionHandler : ExceptionHandlerBase
{
    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.Handle(exception, parameters);
    }
}
