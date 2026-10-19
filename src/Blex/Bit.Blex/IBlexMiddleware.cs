using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// A pipeline hook invoked for every dispatched action. Use for logging, analytics, persistence, etc.
/// Middleware runs synchronously and must not throw; exceptions are swallowed and reported to other middleware.
/// </summary>
public interface IBlexMiddleware
{
    /// <summary>Invoked after an action has been applied to its store.</summary>
    void OnAction(BlexActionContext context);

    /// <summary>
    /// Invoked before an action runs. Override to inspect or <see cref="BlexPreActionContext.Cancel">veto</see>
    /// the action. The default implementation does nothing (the action proceeds).
    /// </summary>
    void BeforeAction(BlexPreActionContext context)
    {
    }

    /// <summary>
    /// Invoked when an action's body throws, before the exception continues on its way. Override
    /// for logging, telemetry or crash reporting. The default implementation does nothing.
    /// </summary>
    /// <remarks>
    /// This cannot swallow the failure -- the exception still propagates (an effect captures it
    /// into <c>XxxError</c>, a plain action surfaces it to the caller). Cancellation is not
    /// reported; see <see cref="BlexActionErrorContext"/>.
    /// </remarks>
    void OnActionError(BlexActionErrorContext context)
    {
    }
}
