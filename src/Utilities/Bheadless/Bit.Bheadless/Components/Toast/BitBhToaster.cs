namespace Bit.Bheadless;

/// <summary>Options for a toast shown with <see cref="BitBhToaster.Show(BitBhToastOptions)"/>.</summary>
public sealed class BitBhToastOptions
{
    /// <summary>
    /// The toast's id. Showing a toast with the id of one still shown updates that one in place (its timer starts
    /// over) instead of adding another - e.g. "Uploading 3 files…" becoming "Uploaded". Default: a new id.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// A variant for your template and styles (<c>data-variant</c>): "success", "error", "warning", "info" …
    /// <see cref="BitBhToaster.PromiseAsync{T}"/> uses "loading", "success" and "error".
    /// </summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Invoked once the toast is gone, however it was dismissed (timer, close button, swipe, Escape, code). It runs on the
    /// thread that dismissed the toast - from a component, change state through <c>InvokeAsync</c> - and an update that
    /// sets none keeps the toast's earlier one.
    /// </summary>
    public Func<Task>? OnDismiss { get; set; }

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

    /// <summary>The options it was shown (or last updated) with.</summary>
    public BitBhToastOptions Options { get; internal set; }

    /// <summary>Incremented each time the toast is updated: a toast restarts its timer and announces itself again.</summary>
    public int Version { get; internal set; }

    /// <summary>The variant (<see cref="BitBhToastOptions.Variant"/>).</summary>
    public string? Variant => Options.Variant;

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

    /// <summary>
    /// Shows a toast and returns its id. With the <see cref="BitBhToastOptions.Id"/> of a toast still shown, updates
    /// that toast instead.
    /// </summary>
    public string Show(BitBhToastOptions options)
    {
        if (options.Id is { } existing && Update(existing, options)) return existing;
        var item = new BitBhToastItem(options.Id ?? $"bit-bh-toast-{Interlocked.Increment(ref _seq)}", options);
        BitBhToastItem[] dropped;
        lock (_lock)
        {
            var all = _toasts.Append(item).ToArray();
            var keep = Math.Max(0, Limit);
            dropped = all.Length > keep ? all[..^keep] : [];
            Volatile.Write(ref _toasts, all.Length > keep ? all[^keep..] : all);
        }
        Changed?.Invoke();
        foreach (var gone in dropped) _ = NotifyDismissedAsync(gone);
        return item.Id;
    }

    /// <summary>
    /// Replaces the options of the toast <paramref name="id"/> (its id stays). The toast shows the new content, restarts
    /// its timer and announces itself again. Returns false when no such toast is shown.
    /// </summary>
    public bool Update(string id, BitBhToastOptions options)
    {
        lock (_lock)
        {
            var item = Array.Find(_toasts, t => t.Id == id);
            if (item is null) return false;
            options.Id = id;
            // The toast is still the same one: what it does when dismissed carries over unless the update replaces it
            // (a promise toast's loading options have the only OnDismiss).
            options.OnDismiss ??= item.Options.OnDismiss;
            item.Options = options;
            item.Version++;
            // A new array, so a viewport enumerating the old one sees a consistent list.
            Volatile.Write(ref _toasts, [.. _toasts]);
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>Changes the toast <paramref name="id"/>'s options in place (see <see cref="Update(string, BitBhToastOptions)"/>).</summary>
    public bool Update(string id, Action<BitBhToastOptions> change)
    {
        var item = Array.Find(Volatile.Read(ref _toasts), t => t.Id == id);
        if (item is null) return false;
        var options = Clone(item.Options);
        change(options);
        return Update(id, options);
    }

    /// <summary>
    /// Shows <paramref name="loading"/> (variant "loading", no timer) while <paramref name="task"/> runs, then turns the same
    /// toast into <paramref name="success"/> (variant "success") or <paramref name="error"/> (variant "error"). Returns the
    /// task's result; a failure is rethrown after the error toast is shown.
    /// </summary>
    public async Task<T> PromiseAsync<T>(Task<T> task, BitBhToastOptions loading, Func<T, BitBhToastOptions> success, Func<Exception, BitBhToastOptions> error)
    {
        loading.Variant ??= "loading";
        loading.Duration ??= 0;
        var id = Show(loading);
        try
        {
            var result = await task;
            var done = success(result);
            done.Variant ??= "success";
            if (!Update(id, done)) Show(done);
            return result;
        }
        catch (Exception ex)
        {
            var failed = error(ex);
            failed.Variant ??= "error";
            if (!Update(id, failed)) Show(failed);
            throw;
        }
    }

    /// <summary>The same as <see cref="PromiseAsync{T}"/> for a task without a result.</summary>
    public Task PromiseAsync(Task task, BitBhToastOptions loading, BitBhToastOptions success, Func<Exception, BitBhToastOptions> error)
        => PromiseAsync(Wrap(task), loading, _ => success, error);

    private static async Task<bool> Wrap(Task task)
    {
        await task;
        return true;
    }

    private static BitBhToastOptions Clone(BitBhToastOptions o) => new()
    {
        Id = o.Id,
        Title = o.Title,
        Description = o.Description,
        Foreground = o.Foreground,
        Duration = o.Duration,
        ActionLabel = o.ActionLabel,
        OnAction = o.OnAction,
        Data = o.Data,
        Variant = o.Variant,
        OnDismiss = o.OnDismiss,
    };

    private static async Task NotifyDismissedAsync(BitBhToastItem item)
    {
        if (item.Options.OnDismiss is not { } onDismiss) return;
        try
        {
            await onDismiss();
        }
        catch (Exception ex)
        {
            // Dismissing never throws at the caller (a timer, another toast pushing it out): the failure is traced, not lost.
            System.Diagnostics.Trace.TraceError($"BitBhToaster: OnDismiss of toast '{item.Id}' failed: {ex}");
        }
    }

    /// <summary>Shows a toast with a title and optional description.</summary>
    public string Show(string title, string? description = null)
        => Show(new BitBhToastOptions { Title = title, Description = description });

    /// <summary>Dismisses a toast.</summary>
    public void Dismiss(string id)
    {
        BitBhToastItem? item;
        lock (_lock)
        {
            item = Array.Find(_toasts, t => t.Id == id);
            if (item is null) return;
            Volatile.Write(ref _toasts, Array.FindAll(_toasts, t => t.Id != id));
        }
        Changed?.Invoke();
        _ = NotifyDismissedAsync(item);
    }

    /// <summary>Dismisses every toast.</summary>
    public void Clear()
    {
        BitBhToastItem[] all;
        lock (_lock)
        {
            all = _toasts;
            if (all.Length == 0) return;
            Volatile.Write(ref _toasts, []);
        }
        Changed?.Invoke();
        foreach (var item in all) _ = NotifyDismissedAsync(item);
    }
}
