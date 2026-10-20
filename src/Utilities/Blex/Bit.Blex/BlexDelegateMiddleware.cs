using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>A simple middleware that forwards each applied action to a delegate.</summary>
public sealed class BlexDelegateMiddleware : IBlexMiddleware
{
    private readonly Action<BlexActionContext> _handler;

    /// <summary>Creates a middleware from a delegate.</summary>
    public BlexDelegateMiddleware(Action<BlexActionContext> handler) => _handler = handler;

    /// <inheritdoc />
    public void OnAction(BlexActionContext context) => _handler(context);
}
