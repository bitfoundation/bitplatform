namespace Bit.Butil;

/// <summary>
/// A message that arrived over a <see cref="PresentationConnectionHandle"/>, from the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/message_event">message</see>
/// event.
/// </summary>
/// <remarks>
/// Exactly one of the two is set: a connection carries either text or binary, and the pair is kept
/// apart rather than merged so that binary data does not have to survive a string round trip.
/// </remarks>
/// <param name="Text">The text that was sent, or null when the message was binary.</param>
/// <param name="Data">The bytes that were sent, or null when the message was text.</param>
public record PresentationMessage(string? Text, byte[]? Data);
