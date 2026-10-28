namespace Bit.Bheadless;

/// <summary>Cascading context provided by <see cref="BitBhHoverCard"/> to its trigger and content.</summary>
public sealed class BitBhHoverCardContext
{
    private readonly Func<Task> _scheduleOpenAsync;
    private readonly Func<Task> _scheduleCloseAsync;
    private readonly Func<bool, Task> _setOpenAsync;

    internal BitBhHoverCardContext(
        bool isOpen,
        string baseId,
        Func<Task> scheduleOpenAsync,
        Func<Task> scheduleCloseAsync,
        Func<bool, Task> setOpenAsync)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        _scheduleOpenAsync = scheduleOpenAsync;
        _scheduleCloseAsync = scheduleCloseAsync;
        _setOpenAsync = setOpenAsync;
    }

    /// <summary>Whether the card is open.</summary>
    public bool IsOpen { get; }

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The trigger id.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>The content id.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Opens after <c>OpenDelay</c> (or keeps it open).</summary>
    public Task ScheduleOpenAsync() => _scheduleOpenAsync();

    /// <summary>Closes after <c>CloseDelay</c>.</summary>
    public Task ScheduleCloseAsync() => _scheduleCloseAsync();

    /// <summary>Opens or closes immediately.</summary>
    public Task SetOpenAsync(bool open) => _setOpenAsync(open);
}

/// <summary>Render-fragment context exposed by hover-card parts.</summary>
public sealed record BitBhHoverCardRenderContext
{
    /// <summary>Whether the card is open.</summary>
    public required bool IsOpen { get; init; }
}
