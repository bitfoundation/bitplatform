namespace Bit.Butil;

/// <summary>One event from a server-sent event stream.</summary>
/// <param name="EventName">
/// The server's event name, or <c>"message"</c> for an unnamed one.
/// </param>
/// <param name="Data">
/// The event's payload, always text. Multi-line payloads arrive joined by newlines, exactly as the
/// protocol defines - deserialize it yourself if the server is sending JSON.
/// </param>
/// <param name="LastEventId">
/// The id the server attached, if any. The browser sends it back as <c>Last-Event-ID</c> when it
/// reconnects, which is how a server resumes a stream rather than restarting it.
/// </param>
public record ServerSentEvent(string EventName, string Data, string LastEventId);
