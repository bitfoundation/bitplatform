namespace Bit.Butil;

/// <summary>An uncaught failure inside a worker.</summary>
/// <param name="Message">
/// The error's message. Cross-origin scripts are reported as the bare string <c>"Script error."</c>
/// with no location - the browser withholds the detail rather than leak it to the page.
/// </param>
/// <param name="FileName">The script the error came from, or an empty string when it is withheld.</param>
/// <param name="LineNumber">The line, or 0 when it is withheld.</param>
/// <param name="ColumnNumber">The column, or 0 when it is withheld.</param>
/// <remarks>
/// A worker that reports an error is still running: the error does not terminate it, and it will go
/// on answering messages. Terminate it yourself if that is not what you want.
/// </remarks>
public record WorkerError(string Message, string FileName, int LineNumber, int ColumnNumber);
