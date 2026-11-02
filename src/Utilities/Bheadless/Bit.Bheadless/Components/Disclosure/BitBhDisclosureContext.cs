namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhDisclosure"/> to its button and panel.
/// </summary>
public sealed class BitBhDisclosureContext : IBitBhCloseContext
{
    /// <summary>Whether the panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether toggling is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The button id (referenced by the panel).</summary>
    public required string ButtonId { get; init; }

    /// <summary>The panel id (referenced by the button's <c>aria-controls</c>).</summary>
    public required string PanelId { get; init; }

    /// <summary>Toggles the panel (fire-and-forget; prefer <see cref="ToggleAsync"/>).</summary>
    public required Action Toggle { get; init; }

    /// <summary>Closes the panel (fire-and-forget; prefer <see cref="CloseAsync"/>).</summary>
    public required Action Close { get; init; }

    /// <summary>Toggles the panel.</summary>
    public Func<Task> ToggleAsync { get; init; } = () => Task.CompletedTask;

    /// <summary>Closes the panel.</summary>
    public Func<Task> CloseAsync { get; init; } = () => Task.CompletedTask;

    Task IBitBhCloseContext.CloseAsync() => CloseAsync();
}

/// <summary>Render-fragment context exposed by disclosure parts.</summary>
public sealed record BitBhDisclosureRenderContext
{
    /// <summary>Whether the panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the disclosure (e.g. from a link inside the panel).</summary>
    public required Action Close { get; init; }
}
