namespace Bit.Butil;

/// <summary>
/// One <see href="https://developer.mozilla.org/en-US/docs/Web/API/Client">client</see> of a service
/// worker - a page, a worker or a shared worker it controls - as reported by
/// <see cref="ServiceWorker.MatchAllClients"/>.
/// </summary>
public class ServiceWorkerClientInfo
{
    /// <summary>The client's id, stable for its lifetime. This is what identifies a tab to the worker.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The client's current URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// <c>"window"</c>, <c>"worker"</c>, <c>"sharedworker"</c>, or empty where the browser doesn't
    /// report it.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// For a window client: <c>"top-level"</c>, <c>"nested"</c>, <c>"auxiliary"</c> or
    /// <c>"none"</c> - which tells an iframe apart from a tab.
    /// </summary>
    public string FrameType { get; set; } = string.Empty;

    /// <summary>True when this window client has focus. Always false for a worker client.</summary>
    public bool Focused { get; set; }

    /// <summary>
    /// <c>"visible"</c> or <c>"hidden"</c> for a window client; empty for a worker client.
    /// </summary>
    public string VisibilityState { get; set; } = string.Empty;
}
