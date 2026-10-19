namespace Bit.BbScanner;

/// <summary>An error raised by the scanner.</summary>
public sealed class BbScannerException : Exception
{
    /// <summary>Creates an exception.</summary>
    /// <param name="kind">Classification of the failure.</param>
    /// <param name="message">Human readable description.</param>
    /// <param name="innerException">The underlying error, when there is one.</param>
    public BbScannerException(BbScannerErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    /// <summary>Classification of the failure, so an application can react without parsing text.</summary>
    public BbScannerErrorKind Kind { get; }

    /// <summary>
    /// Classifies a browser <c>getUserMedia</c> rejection, whose only reliable signal is the
    /// DOMException name embedded in the message.
    /// </summary>
    /// <param name="exception">The exception thrown by the interop call.</param>
    public static BbScannerException FromBrowserError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message;

        var kind = message switch
        {
            var m when m.Contains("NotAllowedError", StringComparison.Ordinal) ||
                       m.Contains("SecurityError", StringComparison.Ordinal) => BbScannerErrorKind.PermissionDenied,
            var m when m.Contains("NotFoundError", StringComparison.Ordinal) ||
                       m.Contains("DevicesNotFoundError", StringComparison.Ordinal) => BbScannerErrorKind.NoCamera,
            var m when m.Contains("NotReadableError", StringComparison.Ordinal) ||
                       m.Contains("TrackStartError", StringComparison.Ordinal) => BbScannerErrorKind.CameraUnavailable,
            var m when m.Contains("OverconstrainedError", StringComparison.Ordinal) ||
                       m.Contains("ConstraintNotSatisfiedError", StringComparison.Ordinal) =>
                BbScannerErrorKind.ConstraintsUnsatisfied,
            var m when m.Contains("NotSupportedError", StringComparison.Ordinal) => BbScannerErrorKind.NotSupported,
            _ => BbScannerErrorKind.Unknown,
        };

        var description = kind switch
        {
            BbScannerErrorKind.PermissionDenied => "Camera permission was denied.",
            BbScannerErrorKind.NoCamera => "No camera matching the request was found.",
            BbScannerErrorKind.CameraUnavailable => "The camera could not be opened; another application may be using it.",
            BbScannerErrorKind.ConstraintsUnsatisfied => "No camera can satisfy the requested constraints.",
            BbScannerErrorKind.NotSupported => "This browser does not expose a camera API to the page.",
            // Not a camera problem at all: the page, or the scanner's own video element, was not there to play
            // into. Anything camera shaped would send the user looking for a fault their device does not have.
            _ when message.Contains("InvalidStateError", StringComparison.Ordinal) => "The scanner is not attached to the page, so the camera has nowhere to play.",
            _ => "The camera could not be started.",
        };

        return new BbScannerException(kind, description, exception);
    }
}
