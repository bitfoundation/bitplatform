using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Bit.BlazorUI;

/// <summary>
/// Settings for evening out bursty text output.
/// </summary>
/// <remarks>
/// Each value is checked as it is set. Whether <see cref="MinCharsPerTick"/> exceeds
/// <see cref="MaxCharsPerTick"/> can only be judged once both are set, so that is checked when the
/// settings are handed to <see cref="BitAiChatSessionOptions.Pacing"/> or to
/// <see cref="BitAiEventStreamExtensions.Paced"/>.
/// </remarks>
public sealed record BitAiTextPacingOptions
{
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(16);
    private readonly int _minCharsPerTick = 1;
    private readonly int _maxCharsPerTick = 48;

    /// <summary>The default settings: a 16 millisecond tick draining the buffer over 150 milliseconds.</summary>
    public static readonly BitAiTextPacingOptions Default = new();

    /// <summary>How often to release a chunk. One animation frame by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public TimeSpan Interval
    {
        get => _interval;
        init
        {
            // Task.Delay reads minus one millisecond as "forever", which would stall the stream.
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _interval = value;
        }
    }

    /// <summary>
    /// How long the pacer aims to take to drain whatever is buffered. Larger values look smoother
    /// and add latency; smaller values track the model more closely.
    /// </summary>
    public TimeSpan TargetDrain { get; init; } = TimeSpan.FromMilliseconds(150);

    /// <summary>The fewest characters to release per tick. At least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int MinCharsPerTick
    {
        get => _minCharsPerTick;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _minCharsPerTick = value;
        }
    }

    /// <summary>
    /// The most characters to release per tick, which caps the smoothing latency. At least one, and
    /// no less than <see cref="MinCharsPerTick"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int MaxCharsPerTick
    {
        get => _maxCharsPerTick;
        init
        {
            // Releasing nothing per tick would never drain the buffer.
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxCharsPerTick = value;
        }
    }

    /// <summary>Checks the settings that depend on each other.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="MinCharsPerTick"/> exceeds <see cref="MaxCharsPerTick"/>.
    /// </exception>
    internal void Validate()
    {
        if (MinCharsPerTick > MaxCharsPerTick)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinCharsPerTick),
                MinCharsPerTick,
                $"{nameof(MinCharsPerTick)} ({MinCharsPerTick}) must not exceed {nameof(MaxCharsPerTick)} ({MaxCharsPerTick}).");
        }
    }
}

/// <summary>
/// Transformations over an event stream.
/// </summary>
public static class BitAiEventStreamExtensions
{
    /// <summary>
    /// Evens out text deltas so output appears at a steady rate instead of in bursts.
    /// </summary>
    /// <param name="source">The stream to pace.</param>
    /// <param name="options">Pacing settings. Defaults to <see cref="BitAiTextPacingOptions.Default"/>.</param>
    /// <param name="cancellationToken">Cancels pacing and the underlying stream.</param>
    /// <returns>The paced stream.</returns>
    /// <remarks>
    /// <para>
    /// Providers deliver text in uneven bursts: nothing for two hundred milliseconds, then a
    /// paragraph. Rendering that verbatim reads as stuttering, which users perceive as slowness even
    /// when the total time is identical. This operator buffers text and releases it at a steady rate
    /// while passing every other event through immediately and in order.
    /// </para>
    /// <para>
    /// It never delays the end of a stream by more than one drain window, and it adds no latency at
    /// all when the model is slower than the pacer, which is the common case.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="BitAiTextPacingOptions.MinCharsPerTick"/> exceeds <see cref="BitAiTextPacingOptions.MaxCharsPerTick"/>.
    /// </exception>
    public static IAsyncEnumerable<BitAiEvent> Paced(
        this IAsyncEnumerable<BitAiEvent> source,
        BitAiTextPacingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Checked here rather than in the iterator, so a bad setting fails at the call site instead
        // of on the first read.
        ArgumentNullException.ThrowIfNull(source);
        var settings = options ?? BitAiTextPacingOptions.Default;
        settings.Validate();

        return PacedIterator(source, settings, cancellationToken);
    }

    private static async IAsyncEnumerable<BitAiEvent> PacedIterator(
        IAsyncEnumerable<BitAiEvent> source,
        BitAiTextPacingOptions settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<BitAiEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });

        // The pump gets its own token so that a consumer that stops early, or throws, also stops the
        // source. Otherwise the transport keeps streaming into the channel with nobody reading it.
        using var pumpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = PumpAsync(source, channel.Writer, pumpCts.Token);

        // Text deltas for the part currently being paced accumulate in `pending`; every other event
        // waits in `deferred` until that text has drained. Order is preserved, and a MessageCompleted
        // that arrives right behind the last token no longer dumps the whole backlog in one frame.
        var pending = new StringBuilder();
        var deferred = new Queue<BitAiEvent>();
        string? pendingPartId = null;
        string? pendingMessageId = null;
        var completed = false;
        var ticksToDrain = Math.Max(1, (int)(settings.TargetDrain.Ticks / Math.Max(1, settings.Interval.Ticks)));

        // The chunk size is kept at the rate the largest backlog since the buffer last ran dry called for. Recomputed
        // from what is left on every tick instead, it would shrink with the buffer, and the tail of a burst would
        // trickle out a character a tick - holding every event queued behind it far longer than one drain window.
        var rate = 0;

        try
        {
            while (true)
            {
                while (channel.Reader.TryRead(out var evt))
                {
                    if (evt is BitAiTextDeltaEvent text && deferred.Count == 0
                        && (pending.Length == 0 || (text.PartId == pendingPartId && text.MessageId == pendingMessageId)))
                    {
                        pendingPartId = text.PartId;
                        pendingMessageId = text.MessageId;
                        pending.Append(text.Delta);
                        continue;
                    }

                    if (pending.Length == 0)
                    {
                        yield return evt;
                        continue;
                    }

                    // A terminal error outranks smoothness: flush everything and surface it now.
                    if (evt is BitAiErrorEvent { IsTerminal: true })
                    {
                        while (pending.Length > 0) yield return Take(pending, pending.Length, pendingPartId, pendingMessageId);
                        rate = 0;
                        while (deferred.TryDequeue(out var queued)) yield return queued;
                        yield return evt;
                        continue;
                    }

                    deferred.Enqueue(evt);
                }

                if (pending.Length > 0)
                {
                    rate = Math.Max(rate, (pending.Length + ticksToDrain - 1) / ticksToDrain);

                    var size = Math.Clamp(rate, settings.MinCharsPerTick, settings.MaxCharsPerTick);

                    yield return Take(pending, size, pendingPartId, pendingMessageId);

                    if (pending.Length == 0)
                    {
                        rate = 0;

                        // Drained: release what queued behind the text, then look for more input
                        // straight away so a following part starts pacing without a gap.
                        while (deferred.TryDequeue(out var queued)) yield return queued;
                        continue;
                    }

                    await Task.Delay(settings.Interval, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                while (deferred.TryDequeue(out var queued)) yield return queued;

                if (completed) yield break;
                if (!await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) completed = true;
            }
        }
        finally
        {
            pumpCts.Cancel();
        }
    }

    private static BitAiTextDeltaEvent Take(StringBuilder pending, int count, string? partId, string? messageId)
    {
        // At least one character, so the drain loop always makes progress.
        count = Math.Clamp(count, 1, pending.Length);

        // Never split a surrogate pair: each half alone renders as a replacement character.
        if (count > 0 && count < pending.Length && char.IsHighSurrogate(pending[count - 1])) count++;

        var chunk = pending.ToString(0, count);
        pending.Remove(0, count);
        return new BitAiTextDeltaEvent(chunk) { PartId = partId, MessageId = messageId };
    }

    private static async Task PumpAsync(
        IAsyncEnumerable<BitAiEvent> source,
        ChannelWriter<BitAiEvent> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var evt in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await writer.WriteAsync(evt, cancellationToken).ConfigureAwait(false);
            }

            writer.TryComplete();
        }
        catch (Exception exception)
        {
            writer.TryComplete(exception);
        }
    }
}
