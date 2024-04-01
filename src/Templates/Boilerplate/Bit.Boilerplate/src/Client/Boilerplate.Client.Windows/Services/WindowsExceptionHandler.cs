using Boilerplate.Client.Core.Services;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsExceptionHandler : ExceptionHandlerBase
{
    protected override void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        base.Handle(exception, parameters);
    }
}
