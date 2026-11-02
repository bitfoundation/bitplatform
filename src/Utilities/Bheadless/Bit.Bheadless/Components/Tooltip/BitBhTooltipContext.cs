namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhTooltip"/> to its trigger and content.
/// </summary>
public sealed class BitBhTooltipContext
{
    private readonly Func<bool, Task> _setOpenAsync;
    private readonly Func<Task> _scheduleOpenAsync;
    private readonly Func<Task> _scheduleCloseAsync;
    private readonly Action<bool> _registerContent;

    internal BitBhTooltipContext(
        bool isOpen,
        bool delayedOpen,
        string baseId,
        bool disableHoverableContent,
        bool hasContent,
        Func<bool, Task> setOpenAsync,
        Func<Task> scheduleOpenAsync,
        Func<Task> scheduleCloseAsync,
        Action<bool> registerContent)
    {
        IsOpen = isOpen;
        DelayedOpen = delayedOpen;
        BaseId = baseId;
        DisableHoverableContent = disableHoverableContent;
        HasContent = hasContent;
        _setOpenAsync = setOpenAsync;
        _scheduleOpenAsync = scheduleOpenAsync;
        _scheduleCloseAsync = scheduleCloseAsync;
        _registerContent = registerContent;
    }

    /// <summary>Whether the tooltip is open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether it opened after the hover delay (vs. instantly).</summary>
    public bool DelayedOpen { get; }

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The trigger id.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>The content id (referenced by the trigger's <c>aria-describedby</c>).</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Whether the pointer may move into the content without closing it.</summary>
    public bool DisableHoverableContent { get; }

    /// <summary>Whether a content element is rendered.</summary>
    public bool HasContent { get; }

    /// <summary>The value of <c>data-state</c>.</summary>
    public string DataState => !IsOpen ? "closed" : DelayedOpen ? "delayed-open" : "instant-open";

    /// <summary>Opens or closes immediately.</summary>
    public Task SetOpenAsync(bool open) => _setOpenAsync(open);

    /// <summary>Opens after the configured delay.</summary>
    public Task ScheduleOpenAsync() => _scheduleOpenAsync();

    /// <summary>Closes after a short grace period.</summary>
    public Task ScheduleCloseAsync() => _scheduleCloseAsync();

    internal void RegisterContent(bool present) => _registerContent(present);
}

/// <summary>Render-fragment context exposed by tooltip parts.</summary>
public sealed record BitBhTooltipRenderContext
{
    /// <summary>Whether the tooltip is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether it opened after the hover delay.</summary>
    public required bool DelayedOpen { get; init; }
}
