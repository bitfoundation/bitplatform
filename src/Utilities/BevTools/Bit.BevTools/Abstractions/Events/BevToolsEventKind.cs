namespace Bit.BevTools;

/// <summary>Broad classification of a timeline entry. Extensions use <see cref="Custom"/> with their own <see cref="BevToolsEvent.Category"/>.</summary>
public enum BevToolsEventKind
{
    Render,
    RenderBatch,
    Lifecycle,
    UiEvent,
    StateChange,
    Navigation,
    Http,
    JsInterop,
    Error,
    Circuit,
    Browser,
    Diagnostic,
    Custom,

    /// <summary>An entry an <c>ILogger</c> of the application wrote (Logs panel).</summary>
    Log,
}
