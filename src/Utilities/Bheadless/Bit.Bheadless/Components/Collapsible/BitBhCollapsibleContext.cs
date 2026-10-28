namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhCollapsible"/> to its trigger and content.
/// </summary>
public sealed class BitBhCollapsibleContext : IBitBhCloseContext
{
    /// <summary>Whether the content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether toggling is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The trigger id.</summary>
    public required string TriggerId { get; init; }

    /// <summary>The content id.</summary>
    public required string ContentId { get; init; }

    /// <summary>Toggles the content (fire-and-forget; prefer <see cref="ToggleAsync"/>).</summary>
    public required Action Toggle { get; init; }

    /// <summary>Closes the content (fire-and-forget; prefer <see cref="CloseAsync"/>).</summary>
    public required Action Close { get; init; }

    /// <summary>Toggles the content.</summary>
    public Func<Task> ToggleAsync { get; init; } = () => Task.CompletedTask;

    /// <summary>Closes the content.</summary>
    public Func<Task> CloseAsync { get; init; } = () => Task.CompletedTask;

    Task IBitBhCloseContext.CloseAsync() => CloseAsync();
}

/// <summary>Render-fragment context exposed by collapsible parts.</summary>
public sealed record BitBhCollapsibleRenderContext
{
    /// <summary>Whether the content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the content.</summary>
    public required Action Close { get; init; }
}
