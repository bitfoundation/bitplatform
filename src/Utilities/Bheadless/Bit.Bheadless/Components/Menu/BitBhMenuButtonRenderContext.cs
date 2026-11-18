namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhMenuButton"/>.</summary>
public sealed record BitBhMenuButtonRenderContext
{
    /// <summary>Whether the menu panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the menu is disabled.</summary>
    public required bool Disabled { get; init; }
}
