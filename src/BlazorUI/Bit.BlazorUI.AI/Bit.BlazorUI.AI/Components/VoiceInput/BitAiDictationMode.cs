namespace Bit.BlazorUI;

/// <summary>
/// How <see cref="BitAiVoiceInput"/> turns speech into text.
/// </summary>
public enum BitAiDictationMode
{
    /// <summary>
    /// Through the app's transcriber when it supplies one (a <see cref="BitAiVoiceInput.Transcriber"/>, or an
    /// <see cref="IBitAiSpeechToText"/> service), recording the microphone in the browser; through the speech API of the
    /// browser otherwise.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Through the speech API of the browser where it has one, and through the app's transcriber, when it supplies one,
    /// only where it has none (Firefox, Android WebView).
    /// </summary>
    Browser = 1,

    /// <summary>
    /// Through the app's transcriber only, recording the microphone in the browser. With no transcriber the button is
    /// not shown.
    /// </summary>
    Recording = 2,
}
