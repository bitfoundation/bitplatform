namespace Bit.Butil;

/// <summary>
/// A chunk that arrived on a WebTransport stream, or - with <paramref name="Ended"/> set - the
/// notification that the stream is finished and nothing more will arrive on it.
/// </summary>
/// <param name="StreamId">Which stream it came from. Matches <see cref="WebTransportStream.Id"/>.</param>
/// <param name="Data">
/// The bytes, exactly as the browser's reader produced them. A stream is a byte stream, so chunk
/// boundaries are the network's, not the sender's - reassembling messages is the caller's job.
/// </param>
/// <param name="Ended">True for the final callback on a stream, whose <paramref name="Data"/> is empty.</param>
public sealed record WebTransportStreamData(string StreamId, byte[] Data, bool Ended);
