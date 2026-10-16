using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// A middleware that can veto actions before they run. The delegate returns <c>false</c> to cancel.
/// </summary>
public sealed class BlexFilterMiddleware : IBlexMiddleware
{
    private readonly Func<BlexPreActionContext, bool> _filter;

    /// <summary>Creates a filter; return <c>false</c> from <paramref name="filter"/> to veto the action.</summary>
    public BlexFilterMiddleware(Func<BlexPreActionContext, bool> filter) => _filter = filter;

    /// <inheritdoc />
    public void OnAction(BlexActionContext context)
    {
    }

    /// <inheritdoc />
    public void BeforeAction(BlexPreActionContext context)
    {
        if (!_filter(context))
            context.Cancel();
    }
}
