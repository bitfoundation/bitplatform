namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhContextMenuTrigger"/>.</summary>
public sealed record BitBhContextMenuTriggerRenderContext
{
    /// <summary>Whether the menu is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the trigger is disabled.</summary>
    public required bool Disabled { get; init; }
}
