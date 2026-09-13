namespace Bit.Butil;

/// <summary>
/// A request a key session wants delivered to the licence server, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeyMessageEvent">MediaKeyMessageEvent</see>.
/// </summary>
/// <remarks>
/// The bytes are opaque and must not be reshaped: POST them to the licence server as they are, and
/// hand the response back through <see cref="MediaKeySessionHandle.Update"/>. This event is the only
/// way a licence request ever reaches the app.
/// </remarks>
/// <param name="Type">Which kind of request this is.</param>
/// <param name="Message">The request bytes, to be sent to the licence server unchanged.</param>
public record MediaKeyMessage(MediaKeyMessageType Type, byte[] Message);
