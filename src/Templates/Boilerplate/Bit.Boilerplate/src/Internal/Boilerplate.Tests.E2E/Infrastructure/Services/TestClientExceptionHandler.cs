using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Same unwrap/ignore as the shipped handlers, minus the UI: SnackBarService only publishes on PubSub here.
/// </summary>
public partial class TestClientExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object?> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, displayKind, parameters);
    }
}
