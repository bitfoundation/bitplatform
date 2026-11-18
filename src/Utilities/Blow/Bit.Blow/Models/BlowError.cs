namespace Bit.Blow;

/// <summary>
/// Something the flow found wrong with the graph it was handed. Reported through
/// <c>BlowCanvas.OnError</c> once per distinct problem rather than on every render.
/// </summary>
/// <param name="Code">A stable machine-readable code; see <see cref="BlowErrorCode"/>.</param>
/// <param name="Message">A human-readable description, suitable for a log line.</param>
public readonly record struct BlowError(string Code, string Message);
