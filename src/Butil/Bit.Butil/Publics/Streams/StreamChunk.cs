namespace Bit.Butil;

/// <summary>One result from <see cref="ReadableStreamHandle.Read"/>.</summary>
/// <param name="Done">
/// True when the stream has ended and there is nothing more to read. The final read always answers
/// with <c>Done</c> and no data, rather than the last chunk and a flag - so a read loop ends on
/// this and never has to check for an empty array.
/// </param>
/// <param name="Data">The chunk's bytes, or null on the final read. Chunk sizes are the browser's choice and vary within one stream.</param>
/// <param name="Error">
/// Why the stream ended early, or null. A stream that errors is finished: <c>Done</c> is true and
/// there is nothing to resume.
/// </param>
public record StreamChunk(bool Done, byte[]? Data, string? Error);
