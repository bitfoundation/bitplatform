using System.Net;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout;

// The speaker half of the panel: speech out. Its counterpart is AppAiChatPanel.razor.Dictation.cs, and the two are
// deliberately not independent - only one of them may be live at a time, or the answer being read ends up in the
// recording. Every crossing between them goes through StopDictation / PauseReadAloud.
public partial class AppAiChatPanel
{
    [AutoInject] private ObjectUrls objectUrls = default!;


    // Read aloud follows the conversation once it is switched on: the answer it was started on, and then every answer
    // that arrives after it, until the user presses stop. An answer is read once it is complete rather than as it
    // streams in - a request per run of a streaming answer is a round trip and a provider charge each, and the gaps
    // between them are audible.
    private bool readAloudEnabled;
    private bool readAloudPaused;
    private bool isReadAloudLoading;
    private string? readAloudObjectUrl;
    private AiChatMessageResponse? readAloudMessage;
    private ElementReference readAloudAudioRef;


    /// <summary>
    /// Switches read aloud on, or off again when pressed a second time. It is a mode rather than a one-shot: from
    /// here the answer to every following prompt is read out as it completes, so a user who is listening instead of
    /// reading does not have to reach for the button again on each turn.
    /// </summary>
    private async Task ToggleReadAloud(AiChatMessageResponse message)
    {
        if (ReferenceEquals(readAloudMessage, message))
        {
            await StopReadAloud();
            return;
        }

        // Asked for here because this press is the only part of read aloud a user drives: everything past it runs on
        // its own, and a modal raised from there would arrive out of nowhere mid-reading.
        if (await EnsureSignedInForSpeech() is false) return;

        // Asking to be read to while the microphone is open is a change of mind about which of the two is wanted.
        await StopDictation();

        readAloudEnabled = true;

        FollowReadAloud(message);

        // An answer that is still arriving is left to ReadAloudCompletedAnswer, which runs the moment it is whole.
        if (ReferenceEquals(message, lastAssistantMessage) is false || isLoading is false)
        {
            await PlayReadAloudMessage();
        }
    }

    /// <summary>Points read aloud at <paramref name="message"/>, with none of it read yet.</summary>
    private void FollowReadAloud(AiChatMessageResponse message)
    {
        readAloudPaused = false;
        readAloudMessage = message;
    }

    /// <summary>
    /// Reads out the answer that has just finished arriving, when read aloud is following it. What is left of the
    /// previous answer gives way here rather than when the prompt was sent, which would have left the user in
    /// silence for as long as the model took to reply.
    /// </summary>
    private async Task ReadAloudCompletedAnswer()
    {
        if (readAloudEnabled is false || readAloudPaused) return;

        if (ReferenceEquals(readAloudMessage, lastAssistantMessage) is false) return;

        await PlayReadAloudMessage();
    }

    /// <summary>
    /// Asks for the answer read aloud is following as audio, and plays it. The whole answer goes up in one request,
    /// and one recording comes back however many times the endpoint had to ask a provider for it.
    /// </summary>
    private async Task PlayReadAloudMessage()
    {
        // isListening covers what readAloudPaused cannot: it is the microphone being open right now, whatever the
        // mode was doing when it opened. Nothing is said while the user is being recorded.
        if (readAloudEnabled is false || readAloudPaused || isListening || readAloudMessage is null) return;

        isReadAloudLoading = true;
        StateHasChanged();

        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/v1/Chatbot/SynthesizeSpeech",
                                                                  new SynthesizeSpeechRequestDto { Text = readAloudMessage.Content ?? string.Empty },
                                                                  JsonSerializerOptions.GetTypeInfo<SynthesizeSpeechRequestDto>(),
                                                                  CurrentCancellationToken);

            // Nothing but a code block or a picture leaves no words once the endpoint has stripped the markdown, so
            // there is nothing to play and nothing was paid for either.
            if (response.StatusCode is HttpStatusCode.NoContent) return;

            // Which container the provider produced is in the response rather than assumed: the element needs to be
            // told what it is being handed to decode it.
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";
            var audio = await response.Content.ReadAsByteArrayAsync(CurrentCancellationToken);

            // The user pressed stop, or opened the microphone, while this was in flight.
            if (readAloudEnabled is false || readAloudPaused || isListening) return;

            await ReleaseReadAloudAudio();

            readAloudObjectUrl = await objectUrls.Create(audio, mediaType);

            await readAloudAudioRef.SetMediaSource(readAloudObjectUrl);
            await readAloudAudioRef.Load();
            await readAloudAudioRef.Play();
        }
        catch (Exception exp)
        {
            // Reading is abandoned rather than retried: the same answer would fail the same way.
            await StopReadAloud();

            ExceptionHandler.Handle(exp, ExceptionDisplayKind.NonInterrupting);
        }
        finally
        {
            isReadAloudLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// The recording has played out. Read aloud stays on for the next answer, so nothing else would hand this back.
    /// </summary>
    private async Task ReadAloudEnded()
    {
        await ReleaseReadAloudAudio();
    }

    /// <summary>
    /// Silences the reading but leaves the mode on, so the answer to what the user is about to say is read out
    /// without them having to ask for it again. The rest of the answer that was cut off is not read: they talked
    /// over it.
    /// </summary>
    private async Task PauseReadAloud()
    {
        if (readAloudEnabled is false || readAloudPaused) return;

        readAloudPaused = true;

        await readAloudAudioRef.Pause();
    }

    private async Task StopReadAloud()
    {
        if (readAloudEnabled is false) return;

        readAloudEnabled = false;
        readAloudPaused = false;
        readAloudMessage = null;

        await readAloudAudioRef.Pause();

        await ReleaseReadAloudAudio();
    }

    /// <summary>
    /// Hands back the object url of the recording that has played: it pins the audio in memory until revoked.
    /// </summary>
    private async ValueTask ReleaseReadAloudAudio()
    {
        if (readAloudObjectUrl is null) return;

        var objectUrl = readAloudObjectUrl;
        readAloudObjectUrl = null;
        await objectUrls.Revoke(objectUrl);
    }
}
