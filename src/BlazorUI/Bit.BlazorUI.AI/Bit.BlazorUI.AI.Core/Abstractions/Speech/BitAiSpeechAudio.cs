namespace Bit.BlazorUI;

/// <summary>
/// The audio a text-to-speech service produced (<see cref="IBitAiTextToSpeech"/>): either its bytes, streamed to the
/// browser to play, or the address the browser fetches it from.
/// </summary>
/// <remarks>
/// An address saves sending the bytes through .NET at all, which on Blazor Server is a trip over the circuit: a service
/// that stores what it synthesizes (or a cache in front of one) hands back where it is. It is played as given, so it is
/// the app's own address, never one a model wrote.
/// </remarks>
public sealed class BitAiSpeechAudio : IAsyncDisposable
{
    private BitAiSpeechAudio(Stream? content, string? contentType, string? url)
    {
        Content = content;
        ContentType = contentType;
        Url = url;
    }

    /// <summary>
    /// The bytes of the audio, when it is played from them. Disposed by whoever plays it, once they have been sent.
    /// </summary>
    public Stream? Content { get; }

    /// <summary>The media type of <see cref="Content"/>, such as <c>audio/mpeg</c>.</summary>
    public string? ContentType { get; }

    /// <summary>The address the browser fetches the audio from, when it is played from one.</summary>
    public string? Url { get; }

    /// <summary>Audio played from its bytes.</summary>
    /// <param name="content">The bytes, read from the current position.</param>
    /// <param name="contentType">Their media type, such as <c>audio/mpeg</c>, which the browser needs to decode them.</param>
    /// <returns>The audio.</returns>
    public static BitAiSpeechAudio FromStream(Stream content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return new(content, contentType, null);
    }

    /// <summary>Audio played from its bytes.</summary>
    /// <param name="content">The bytes.</param>
    /// <param name="contentType">Their media type, such as <c>audio/mpeg</c>, which the browser needs to decode them.</param>
    /// <returns>The audio.</returns>
    public static BitAiSpeechAudio FromBytes(byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);

        return FromStream(new MemoryStream(content, writable: false), contentType);
    }

    /// <summary>Audio the browser fetches from an address of the app's.</summary>
    /// <param name="url">The address: absolute, or relative to the base address of the page.</param>
    /// <returns>The audio.</returns>
    public static BitAiSpeechAudio FromUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new(null, null, url);
    }

    /// <summary>Disposes <see cref="Content"/>, if there is one.</summary>
    /// <returns>A task that completes once it is disposed.</returns>
    public ValueTask DisposeAsync() => Content?.DisposeAsync() ?? ValueTask.CompletedTask;
}
