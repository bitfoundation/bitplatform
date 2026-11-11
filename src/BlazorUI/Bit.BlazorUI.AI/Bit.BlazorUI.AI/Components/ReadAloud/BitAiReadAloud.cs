namespace Bit.BlazorUI;

/// <summary>
/// Reads replies aloud through the app's own text-to-speech service: the speech is synthesized in .NET (an
/// <see cref="IBitAiTextToSpeech"/>, or a delegate) and played in the browser, where it can be stopped.
/// </summary>
/// <remarks>
/// <para>
/// One instance drives the "Read aloud" action of every reply of a chat: <see cref="BitAiChat"/> creates one when
/// <see cref="BitAiChat.AllowReadAloud"/> is on and cascades it to its <see cref="BitAiMessageActions"/>. A chat assembled
/// by hand creates its own and cascades it the same way (<c>&lt;CascadingValue Value="readAloud"&gt;</c>), or hands it to
/// each <see cref="BitAiMessageActions.ReadAloud"/>, and calls <see cref="ReadNextAsync"/> as each reply finishes.
/// </para>
/// <para>
/// With <see cref="KeepReading"/> on, a reading started on a reply goes on with every reply that finishes after it, until
/// it is stopped - for a user who listens instead of reading. A reply is read once it has finished rather than as it
/// streams in, so the reading is one request to the service and plays without gaps.
/// </para>
/// <para>
/// One reading plays at a time in a page: starting one stops another. Nothing is read while the microphone records a
/// dictation (<see cref="BitAiVoiceInput"/>), and a dictation starting silences what is playing, since the recording
/// would carry it; keep reading stays on, so the reply to what the user is about to say is read in its turn.
/// </para>
/// <para>
/// It is driven from the renderer's dispatcher, as the event handlers of components are; its members are not safe to
/// call from other threads at once.
/// </para>
/// </remarks>
public sealed class BitAiReadAloud : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Func<BitAiMessage, CancellationToken, Task<BitAiSpeechAudio?>> _synthesize;
    private DotNetObjectReference<BitAiReadAloud>? _dotnetObj;
    private CancellationTokenSource? _cts;
    private BitAiMessage? _message;
    private BitAiMessage? _awaited;
    private int _handle;
    private int _version;
    private bool _loading;
    private bool _following;
    private bool _disposed;



    /// <summary>
    /// Creates a reader that synthesizes through a text-to-speech service, in the <see cref="Language"/> set on it.
    /// </summary>
    /// <param name="js">The JS runtime of the page the speech plays in.</param>
    /// <param name="textToSpeech">The service.</param>
    public BitAiReadAloud(IJSRuntime js, IBitAiTextToSpeech textToSpeech)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(textToSpeech);

        _js = js;
        _synthesize = (message, cancellationToken) => textToSpeech.SynthesizeAsync(message.GetText(), Language, cancellationToken);
    }

    /// <summary>
    /// Creates a reader that synthesizes through a delegate.
    /// </summary>
    /// <param name="js">The JS runtime of the page the speech plays in.</param>
    /// <param name="synthesize">
    /// Given the reply - its text (<see cref="BitAiMessage.GetText"/>, Markdown), and whatever else the app's service
    /// needs of it, such as a signature it carries - and a token cancelled when the reading is stopped, returns the audio
    /// to play, or null when there is nothing to read.
    /// </param>
    public BitAiReadAloud(IJSRuntime js, Func<BitAiMessage, CancellationToken, Task<BitAiSpeechAudio?>> synthesize)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(synthesize);

        _js = js;
        _synthesize = synthesize;
    }



    /// <summary>
    /// Raised whenever what is read, or whether it is loading or playing, changes - for the buttons showing it to render.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Raised when a reading fails, with the name of what failed: <c>synthesis-failed</c> (the service threw) or
    /// <c>playback-failed</c> (the browser could not play the audio). The reading is stopped, keep reading with it.
    /// </summary>
    public event Action<string>? Failed;



    /// <summary>
    /// Whether a reading goes on with each reply that finishes after the one it was started on, until it is stopped.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>Read as a reading starts: changing it does not affect one already following the conversation.</remarks>
    public bool KeepReading { get; set; }

    /// <summary>
    /// The language handed to the <see cref="IBitAiTextToSpeech"/> service, as a BCP 47 language tag such as
    /// <c>en-US</c>; null leaves it to the service. A delegate reads the language it wants itself.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The speed the speech plays at, 1 being as synthesized; the browser keeps its pitch.
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    public double PlaybackRate { get; set; } = 1;

    /// <summary>
    /// The reply being read - loading, playing, waiting to finish arriving, or, while keep reading follows the
    /// conversation, the last one it read.
    /// </summary>
    public BitAiMessage? Message => _message;

    /// <summary>Whether the speech of the reply is being synthesized.</summary>
    public bool IsLoading => _loading;

    /// <summary>Whether speech is playing.</summary>
    public bool IsPlaying => _handle > 0;

    /// <summary>Whether keep reading follows the conversation, reading each reply that finishes.</summary>
    public bool IsFollowing => _following;



    /// <summary>
    /// Whether the reply is the one being read (see <see cref="Message"/>), which its button stops rather than starts.
    /// </summary>
    /// <param name="message">The reply.</param>
    /// <returns>Whether it is the one being read.</returns>
    public bool IsReading(BitAiMessage? message) => message is not null && ReferenceEquals(_message, message);

    /// <summary>
    /// Stops the reading of the reply when it is the one being read, and reads it otherwise.
    /// </summary>
    /// <param name="message">The reply.</param>
    /// <returns>A task that completes once the speech has started playing, or the reading has stopped.</returns>
    public Task ToggleAsync(BitAiMessage message) => IsReading(message) ? StopAsync() : ReadAsync(message);

    /// <summary>
    /// Reads a reply aloud, in place of whatever is being read. A reply still arriving is read once it has finished.
    /// </summary>
    /// <param name="message">The reply.</param>
    /// <returns>A task that completes once the speech has started playing, or it is clear that nothing will.</returns>
    public async Task ReadAsync(BitAiMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_disposed) return;

        // The press asking for the reading is the one moment the browser lets audio start, and the speech arrives a round
        // trip later: the player is woken here so it can play then.
        await SafeAsync(_js.BitAiReadAloudPrime);

        await StopPlaybackAsync();

        _message = message;
        _following = KeepReading;

        if (message.IsBusy)
        {
            _awaited = message;
            Notify();
            return;
        }

        _awaited = null;

        await PlayAsync(message);
    }

    /// <summary>
    /// Tells the reader that a reply has finished: it is read when it is the one a reading was waiting for, or when keep
    /// reading follows the conversation.
    /// </summary>
    /// <param name="reply">The reply that finished.</param>
    /// <returns>A task that completes once its speech has started playing, or it is clear that nothing will.</returns>
    public async Task ReadNextAsync(BitAiMessage reply)
    {
        if (_disposed || reply is null || reply.IsBusy || reply.Role != BitAiRole.Assistant) return;

        if (ReferenceEquals(_awaited, reply))
        {
            _awaited = null;
            await PlayAsync(reply);
            return;
        }

        if (_following is false || ReferenceEquals(_message, reply)) return;

        await StopPlaybackAsync();

        _message = reply;

        await PlayAsync(reply);
    }

    /// <summary>
    /// Stops the reading, keep reading with it.
    /// </summary>
    /// <returns>A task that completes once the speech has stopped.</returns>
    public async Task StopAsync()
    {
        _following = false;
        _awaited = null;
        _message = null;

        await StopPlaybackAsync();

        Notify();
    }



    /// <summary>
    /// Called by the playback script when the speech has played out, or has been silenced from the page (the microphone
    /// opened, another reading started).
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="handle">The handle of the reading that ended.</param>
    /// <returns>A completed task.</returns>
    [JSInvokable("OnPlaybackEnded")]
    public Task _OnPlaybackEnded(int handle)
    {
        if (_disposed || handle == 0 || handle != _handle) return Task.CompletedTask;

        _handle = 0;
        EndReading();
        Notify();

        return Task.CompletedTask;
    }



    private async Task PlayAsync(BitAiMessage message)
    {
        var version = ++_version;

        if (string.IsNullOrWhiteSpace(message.GetText()))
        {
            EndReading();
            Notify();
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        _loading = true;
        Notify();

        var synthesized = false;

        try
        {
            var audio = await _synthesize(message, cts.Token);
            synthesized = true;

            await using (audio)
            {
                if (version != _version) return;

                if (audio is null || (audio.Url is null && audio.Content is null))
                {
                    EndReading();
                    return;
                }

                _dotnetObj ??= DotNetObjectReference.Create(this);

                int handle;
                if (audio.Url is { } url)
                {
                    handle = await _js.BitAiReadAloudPlay(_dotnetObj, url, null, null, PlaybackRate);
                }
                else
                {
                    // Streamed rather than inline, which on Blazor Server would exceed the size of a circuit message for
                    // anything but a few words. The script reads it whole before play resolves, so it is released after.
                    using var content = new DotNetStreamReference(audio.Content!, leaveOpen: true);
                    handle = await _js.BitAiReadAloudPlay(_dotnetObj, null, content, audio.ContentType, PlaybackRate);
                }

                if (version != _version)
                {
                    if (handle > 0) await SafeAsync(() => _js.BitAiReadAloudStop(handle));
                    return;
                }

                if (handle > 0)
                {
                    _handle = handle;
                }
                else if (handle < 0)
                {
                    Fail("playback-failed");
                }
                else
                {
                    // Nothing plays while the microphone records, which leaves this reply unread.
                    EndReading();
                }
            }
        }
        catch (OperationCanceledException) when (version != _version || cts.IsCancellationRequested) { }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page that would play it with it
        catch (Exception) when (version == _version && _disposed is false)
        {
            Fail(synthesized ? "playback-failed" : "synthesis-failed");
        }
        finally
        {
            if (version == _version) _loading = false;
            if (ReferenceEquals(_cts, cts)) _cts = null;
            cts.Dispose();

            Notify();
        }
    }

    private async Task StopPlaybackAsync()
    {
        _version++;
        _loading = false;

        if (_cts is { } cts)
        {
            _cts = null;
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
        }

        var handle = _handle;
        _handle = 0;

        if (handle > 0) await SafeAsync(() => _js.BitAiReadAloudStop(handle));
    }

    // A reading that is over leaves its reply the one being read only while keep reading follows the conversation.
    private void EndReading()
    {
        if (_following is false) _message = null;
    }

    private void Fail(string error)
    {
        _following = false;
        _awaited = null;
        _message = null;

        Failed?.Invoke(error);
    }

    private void Notify()
    {
        if (_disposed) return;

        Changed?.Invoke();
    }

    // Starting and stopping the player on the reader's own behalf never fails what it is part of.
    private static async Task SafeAsync(Func<ValueTask> call)
    {
        try
        {
            await call();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first
        catch (JSException) { } // a player that failed to stop has nothing left to stop
        catch (TaskCanceledException) { }
    }



    /// <summary>
    /// Stops the reading and releases what the browser holds of it.
    /// </summary>
    /// <returns>A task that completes once it has.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _following = false;
        _awaited = null;
        _message = null;

        await StopPlaybackAsync();

        _disposed = true;
        Changed = null;
        Failed = null;

        _dotnetObj?.Dispose();
        _dotnetObj = null;
    }
}
