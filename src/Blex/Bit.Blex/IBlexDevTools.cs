using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Abstraction over a DevTools sink. The core library depends only on this interface; the concrete
/// Redux DevTools bridge lives in <c>Bit.Blex.Blazor</c> so the core has no JS dependency.
/// </summary>
public interface IBlexDevTools
{
    /// <summary>Sends the initial application state when the DevTools session connects.</summary>
    void Init(JsonObject globalState);

    /// <summary>Sends a dispatched action and the resulting global state.</summary>
    void Send(string actionName, JsonObject globalState);

    /// <summary>
    /// Sends a dispatched action with its argument payload and the resulting global state.
    /// The default implementation drops the payload for sinks that predate it.
    /// </summary>
    void Send(string actionName, JsonObject globalState, JsonObject? payload) => Send(actionName, globalState);
}
