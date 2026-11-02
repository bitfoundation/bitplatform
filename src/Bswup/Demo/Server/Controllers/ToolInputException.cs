using ModelContextProtocol;

namespace Bit.Bswup.Demo.Server.Controllers;

/// <summary>
/// A tool refusing the input it was called with: the caller's mistake, never a fault of the server.
/// <para>
/// Its message is the answer, on both surfaces. Over MCP the call-tool filter in Program.cs turns
/// it into the tool's error result, before the SDK would log it as an unhandled exception with a
/// stack trace; over the HTTP mirror <see cref="McpErrorMiddleware"/> turns it into a 400 whose
/// problem details carry it. Neither logs it as a server failure. It is an
/// <see cref="McpException"/> as well, so a path that bypassed the filter would still hand the
/// model the reason rather than a bare "an error occurred".
/// </para>
/// <para>
/// Only throw it for input a caller can fix. Its own type, rather than the SDK's, is what keeps
/// the mapping narrow: an <see cref="McpException"/> raised for a real server fault still reaches
/// the exception handler as the 500 it is.
/// </para>
/// </summary>
public class ToolInputException(string message, Exception? innerException = null) : McpException(message, innerException);
