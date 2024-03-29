using Boilerplate.Client.Core.Services;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsExceptionHandler : ExceptionHandlerBase
{
    protected override void LogError(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.LogError(exception, parameters);
    }
}
