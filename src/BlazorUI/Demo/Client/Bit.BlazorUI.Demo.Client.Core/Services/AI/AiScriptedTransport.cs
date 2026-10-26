using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// A transport built from a delegate, used throughout the AI demos in place of a provider.
/// </summary>
/// <remarks>
/// The demos run entirely in the browser with no API key, which is only possible because the AI package never talks to
/// a provider itself. Every scenario is wired to one of these, and the components cannot tell the difference between
/// this and a real model.
/// </remarks>
public sealed class AiScriptedTransport(Func<BitAiRequest, CancellationToken, IAsyncEnumerable<BitAiEvent>> script)
    : IBitAiTransport
{
    /// <inheritdoc />
    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default) =>
        script(request, cancellationToken);
}

/// <summary>
/// Helpers for writing readable scripts.
/// </summary>
public static class AiScript
{
    /// <summary>Streams text a few characters at a time, the way a model does.</summary>
    /// <param name="text">The text to stream.</param>
    /// <param name="chunk">How many characters to send at once.</param>
    /// <param name="delayMs">How long to wait between chunks.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    /// <returns>The text delta events.</returns>
    public static async IAsyncEnumerable<BitAiEvent> Write(
        string text,
        int chunk = 4,
        int delayMs = 14,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < text.Length; i += chunk)
        {
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            yield return new BitAiTextDeltaEvent(text.Substring(i, Math.Min(chunk, text.Length - i)));
        }
    }

    /// <summary>Streams reasoning text.</summary>
    /// <param name="text">The reasoning to stream.</param>
    /// <param name="delayMs">How long to wait between chunks.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    /// <returns>The reasoning delta events.</returns>
    public static async IAsyncEnumerable<BitAiEvent> Think(
        string text,
        int delayMs = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in text.Split(' '))
        {
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            yield return new BitAiReasoningDeltaEvent(word + " ");
        }
    }

    /// <summary>Waits, so a demo reads at human speed.</summary>
    /// <param name="milliseconds">How long to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task that completes after the delay.</returns>
    public static Task Pause(int milliseconds, CancellationToken cancellationToken) =>
        Task.Delay(milliseconds, cancellationToken);

    /// <summary>The most recent user message.</summary>
    /// <param name="request">The request to read.</param>
    /// <returns>The message, or <see langword="null"/> when the user has not written yet.</returns>
    public static IBitAiMessage? LastUserMessage(BitAiRequest request)
    {
        for (var i = request.Messages.Count - 1; i >= 0; i--)
        {
            if (request.Messages[i].Role.IsUser) return request.Messages[i];
        }

        return null;
    }

    /// <summary>The text of the most recent user message.</summary>
    /// <param name="request">The request to read.</param>
    /// <returns>The user text, or an empty string.</returns>
    public static string LastUserText(BitAiRequest request) => LastUserMessage(request)?.GetText() ?? string.Empty;

    /// <summary>Whether a tool has already returned a result since the most recent user message.</summary>
    /// <param name="request">The request to read.</param>
    /// <returns><see langword="true"/> when this turn already ran a tool and the model should answer.</returns>
    /// <remarks>
    /// Scoped to the current turn, so a follow-up question calls the tool again instead of answering from a result an
    /// earlier turn produced.
    /// </remarks>
    public static bool HasToolResultThisTurn(BitAiRequest request)
    {
        for (var i = request.Messages.Count - 1; i >= 0; i--)
        {
            var message = request.Messages[i];
            if (message.Role.IsUser) return false;
            if (message.Parts.OfType<BitAiToolCallPart>().Any(c => c.Result is not null)) return true;
        }

        return false;
    }

    /// <summary>The outcome of the latest tool call that returned since the most recent user message.</summary>
    /// <param name="request">The request to read.</param>
    /// <returns>The outcome, or <see langword="null"/> when no tool has returned this turn.</returns>
    /// <remarks>
    /// What a model reads before it answers: a call the user declined comes back as a failure, and the answer has to
    /// say that nothing was done rather than report what the tool would have done.
    /// </remarks>
    public static BitAiToolOutcome? ToolOutcomeThisTurn(BitAiRequest request)
    {
        for (var i = request.Messages.Count - 1; i >= 0; i--)
        {
            var message = request.Messages[i];
            if (message.Role.IsUser) return null;
            if (message.Parts.OfType<BitAiToolCallPart>().LastOrDefault(c => c.Result is not null) is { } call) return call.Result;
        }

        return null;
    }

    /// <summary>Reports plausible token usage for a generated response.</summary>
    /// <param name="request">The request, used to size the prompt.</param>
    /// <param name="output">The generated text.</param>
    /// <returns>A usage event.</returns>
    public static BitAiUsageUpdatedEvent Usage(BitAiRequest request, string output)
    {
        var input = request.Messages.Sum(m => m.GetText().Length) / 4;
        var generated = output.Length / 4;

        return new BitAiUsageUpdatedEvent(new BitAiTokenUsage
        {
            InputTokens = input,
            OutputTokens = generated,
            Cost = (input * 3m + generated * 15m) / 1_000_000m,
            Currency = "USD",
        });
    }

    /// <summary>Cuts a value into slices of a fixed size, the way a model streams a long payload.</summary>
    /// <param name="value">The value to cut.</param>
    /// <param name="size">The length of every slice but the last.</param>
    /// <returns>The slices, in order.</returns>
    public static IEnumerable<string> Slice(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size)
        {
            yield return value.Substring(i, Math.Min(size, value.Length - i));
        }
    }

    /// <summary>Parses a JSON literal of a script into a value that outlives its document.</summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The value.</returns>
    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Turns a sequential script into an event stream, so a scenario reads top to bottom.
    /// </summary>
    /// <param name="token">Cancels the script.</param>
    /// <param name="body">The script, which yields events by calling the supplied delegate.</param>
    /// <returns>The event stream.</returns>
    public static async IAsyncEnumerable<BitAiEvent> Stream(
        [EnumeratorCancellation] CancellationToken token,
        Func<Func<BitAiEvent, Task>, Task> body)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<BitAiEvent>();

        var producer = Task.Run(async () =>
        {
            try
            {
                await body(evt => channel.Writer.WriteAsync(evt, token).AsTask());
                channel.Writer.TryComplete();
            }
            catch (Exception exception)
            {
                channel.Writer.TryComplete(exception);
            }
        }, token);

        await foreach (var evt in channel.Reader.ReadAllAsync(token)) yield return evt;
        await producer;
    }
}
