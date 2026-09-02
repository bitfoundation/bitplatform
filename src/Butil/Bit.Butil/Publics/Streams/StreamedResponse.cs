namespace Bit.Butil;

/// <summary>
/// What <see cref="Streams.FromResponse"/> answers with: the response's status, and its body as a
/// stream that has not been read yet.
/// </summary>
/// <param name="Stream">
/// The body, or null when there is nothing to read - a failed request, or a response that has no
/// body at all (a 204, a <c>HEAD</c>, an opaque <c>no-cors</c> response). Dispose it when done.
/// </param>
/// <param name="Status">The HTTP status code, or 0 when the request never reached a server.</param>
/// <param name="StatusText">The status text, which many servers leave empty over HTTP/2.</param>
/// <param name="Url">The URL the response came from - which is not the requested one if a redirect was followed.</param>
/// <param name="TotalBytes">
/// The <c>Content-Length</c> header, when the server sent one. It describes the <em>transfer</em>,
/// not the stream: a response the server compressed reports the encoded size while the chunks you
/// read are the decoded ones, so the two disagree and the ratio can pass 100%. A chunked response
/// has no header at all. Treat it as a hint for a progress bar, never as the number of bytes you are
/// going to read.
/// </param>
/// <param name="Error">
/// Why there is no stream, or null when there is one. A network failure, a CORS refusal and a
/// bodyless response all arrive here rather than as an exception.
/// </param>
public record StreamedResponse(ReadableStreamHandle? Stream, int Status, string StatusText, string Url, long? TotalBytes, string? Error);
