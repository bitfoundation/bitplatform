namespace Bit.Butil;

/// <summary>
/// What a runtime's handwriting recognizer will actually do, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/queryHandwritingRecognizer">queryHandwritingRecognizerSupport()</see>.
/// </summary>
/// <remarks>
/// Asked per feature rather than as one yes/no: a runtime that supports handwriting still says no to
/// a language it has no model for, and no to hints it doesn't implement.
/// </remarks>
public class HandwritingSupport
{
    /// <summary>True when every language you asked about has a model on this device.</summary>
    public bool Languages { get; set; }

    /// <summary>True when the recognizer can return more than one candidate per drawing.</summary>
    public bool Alternatives { get; set; }

    /// <summary>True when the recognizer takes preceding text into account while recognizing.</summary>
    public bool TextContext { get; set; }
}
