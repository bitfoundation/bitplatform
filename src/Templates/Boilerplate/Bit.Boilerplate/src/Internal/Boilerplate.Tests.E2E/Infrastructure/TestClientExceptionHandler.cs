using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Same unwrap/ignore as the shipped client handlers. SnackBarService only publishes on PubSub, so there is no UI to
/// open when a refresh fails inside this host.
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
