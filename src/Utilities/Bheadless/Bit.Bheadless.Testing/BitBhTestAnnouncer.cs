using System.Globalization;

namespace Bit.Bheadless.Testing;

/// <summary>
/// An <see cref="IBitBhAnnouncer"/> that records announcements instead of sending them
/// to a live region — assert what screen-reader users would hear.
/// </summary>
public sealed class BitBhTestAnnouncer : IBitBhAnnouncer
{
    private readonly Lock _gate = new();
    private readonly List<(string Message, BitBhPoliteness Politeness)> _messages = new();
    private readonly List<(Func<string, bool> Match, TaskCompletionSource<string> Result)> _waiters = new();

    /// <summary>Every announcement made, in order.</summary>
    public IReadOnlyList<(string Message, BitBhPoliteness Politeness)> Announcements
    {
        get { lock (_gate) return _messages.ToArray(); }
    }

    /// <summary>The announced messages, in order.</summary>
    public IReadOnlyList<string> Messages
    {
        get { lock (_gate) return _messages.Select(m => m.Message).ToArray(); }
    }

    /// <summary>The most recent message, or null.</summary>
    public string? Last
    {
        get { lock (_gate) return _messages.Count == 0 ? null : _messages[^1].Message; }
    }

    /// <inheritdoc />
    public ValueTask AnnounceAsync(string message, BitBhPoliteness politeness = BitBhPoliteness.Polite)
    {
        List<TaskCompletionSource<string>> matched = new();
        lock (_gate)
        {
            _messages.Add((message, politeness));
            for (var i = _waiters.Count - 1; i >= 0; i--)
            {
                if (!_waiters[i].Match(message)) continue;
                matched.Add(_waiters[i].Result);
                _waiters.RemoveAt(i);
            }
        }
        foreach (var result in matched) result.TrySetResult(message);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Waits for an announcement (optionally one matching <paramref name="predicate"/>),
    /// including one already made. Announcements are often debounced (result counts,
    /// drag-and-drop steps), so they may arrive after the interaction that caused them.
    /// </summary>
    /// <exception cref="TimeoutException">Nothing matching was announced in time (default: 3 seconds).</exception>
    public async Task<string> WaitForAsync(Func<string, bool>? predicate = null, TimeSpan? timeout = null)
    {
        predicate ??= static _ => true;
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            foreach (var (message, _) in _messages)
                if (predicate(message)) return message;
            _waiters.Add((predicate, tcs));
        }

        var limit = timeout ?? TimeSpan.FromSeconds(3);
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(limit)).ConfigureAwait(false);
        if (finished == tcs.Task) return await tcs.Task.ConfigureAwait(false);
        lock (_gate) _waiters.RemoveAll(w => w.Result == tcs);
        throw new TimeoutException(string.Create(CultureInfo.InvariantCulture, $"No matching announcement within {limit.TotalSeconds:0.#} s. Announced: [{string.Join(", ", Messages.Select(m => $"\"{m}\""))}]"));
    }

    /// <summary>Waits for an announcement equal to <paramref name="message"/>.</summary>
    public Task<string> WaitForAsync(string message, TimeSpan? timeout = null)
        => WaitForAsync(m => m == message, timeout);

    /// <summary>Forgets every recorded announcement.</summary>
    public void Clear()
    {
        lock (_gate) _messages.Clear();
    }
}
