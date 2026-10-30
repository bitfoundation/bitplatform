namespace Bit.Bheadless;

/// <summary>Options for a toast shown with <see cref="BitBhToaster.Show(BitBhToastOptions)"/>.</summary>
public sealed class BitBhToastOptions
{
    /// <summary>The toast title.</summary>
    public string? Title { get; set; }

    /// <summary>The toast description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreground toasts are the result of a user action and are announced
    /// assertively (<c>role="alert"</c>); background toasts politely (<c>role="status"</c>).
    /// Default: background.
    /// </summary>
    public bool Foreground { get; set; }

    /// <summary>Auto-dismiss delay (ms). Null uses the viewport default; 0 or less keeps it until dismissed.</summary>
    public int? Duration { get; set; }

    /// <summary>Optional action button label (e.g. "Undo").</summary>
    public string? ActionLabel { get; set; }

    /// <summary>Invoked when the action is chosen.</summary>
    public Func<Task>? OnAction { get; set; }

    /// <summary>Arbitrary data for your template (icon, variant, …).</summary>
    public object? Data { get; set; }
}

/// <summary>A toast queued in a <see cref="BitBhToaster"/>.</summary>
public sealed class BitBhToastItem
{
    internal BitBhToastItem(string id, BitBhToastOptions options)
    {
        Id = id;
        Options = options;
    }

    /// <summary>Unique id.</summary>
    public string Id { get; }

    /// <summary>The options it was shown with.</summary>
    public BitBhToastOptions Options { get; }

    /// <summary>The title.</summary>
    public string? Title => Options.Title;

    /// <summary>The description.</summary>
    public string? Description => Options.Description;

    /// <summary>Custom data.</summary>
    public object? Data => Options.Data;

    /// <summary>Whether it is closing (exit animation).</summary>
    public bool IsClosing { get; internal set; }
}

/// <summary>
/// Queues toast notifications rendered by a <see cref="BitBhToastViewport"/>. Registered
/// (scoped) by <c>AddBitBheadlessServices()</c>; inject it anywhere.
///
/// <code>
/// @inject BitBhToaster Toaster
/// Toaster.Show(new() { Title = "Saved", Description = "Your changes are live.", ActionLabel = "Undo", OnAction = UndoAsync });
/// </code>
/// </summary>
public sealed class BitBhToaster
{
    // Show is often called off the dispatcher (a timer, a hub message) while a viewport is
    // enumerating Toasts, so every change swaps in a new array and Toasts is a snapshot.
    private readonly object _lock = new();
    private BitBhToastItem[] _toasts = [];
    private long _seq;

    /// <summary>The toasts currently shown, oldest first.</summary>
    public IReadOnlyList<BitBhToastItem> Toasts => Volatile.Read(ref _toasts);

    /// <summary>Maximum number of toasts kept; older ones are dropped. Default: 5.</summary>
    public int Limit { get; set; } = 5;

    /// <summary>Raised when toasts are added or removed.</summary>
    public event Action? Changed;

    /// <summary>Shows a toast and returns its id.</summary>
    public string Show(BitBhToastOptions options)
    {
        var item = new BitBhToastItem($"bit-bh-toast-{Interlocked.Increment(ref _seq)}", options);
        lock (_lock)
        {
            Volatile.Write(ref _toasts, [.. _toasts.Append(item).TakeLast(Math.Max(0, Limit))]);
        }
        Changed?.Invoke();
        return item.Id;
    }

    /// <summary>Shows a toast with a title and optional description.</summary>
    public string Show(string title, string? description = null)
        => Show(new BitBhToastOptions { Title = title, Description = description });

    /// <summary>Dismisses a toast.</summary>
    public void Dismiss(string id)
    {
        lock (_lock)
        {
            if (Array.FindIndex(_toasts, t => t.Id == id) < 0) return;
            Volatile.Write(ref _toasts, Array.FindAll(_toasts, t => t.Id != id));
        }
        Changed?.Invoke();
    }

    /// <summary>Dismisses every toast.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            if (_toasts.Length == 0) return;
            Volatile.Write(ref _toasts, []);
        }
        Changed?.Invoke();
    }
}
