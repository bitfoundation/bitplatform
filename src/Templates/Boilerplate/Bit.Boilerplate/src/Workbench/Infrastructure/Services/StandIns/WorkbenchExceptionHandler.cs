namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The app's client exception handling, as the web client does it: unwraps the exception and drops the ones the app ignores.
/// </summary>
public partial class WorkbenchExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object?> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        // Only logged: the app's handler breaks into an attached debugger here, which would freeze every view.
        if (displayKind is ExceptionDisplayKind.None)
        {
            Logger.LogError(exception, GetExceptionMessageToLog(exception));
            return;
        }

        base.Handle(exception, displayKind, parameters);
    }
}
