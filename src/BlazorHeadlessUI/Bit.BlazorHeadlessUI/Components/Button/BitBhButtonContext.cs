namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Exposes the current state of an <see cref="BitBhButton"/> to its render
/// fragment, enabling consumers to conditionally render content
/// (e.g. spinners, icons) based on component state without external tracking.
///
/// Passed via <c>RenderFragment&lt;BitBhButtonContext&gt;</c> — consumers can
/// access it through the implicit <c>@context</c> variable or a named
/// <c>Context="btn"</c> parameter.
/// </summary>
public sealed record BitBhButtonContext
{
    /// <summary>Whether the button is currently disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether the button is in a loading/busy state.</summary>
    public required bool Loading { get; init; }

    /// <summary>The resolved HTML button type (button, submit, reset).</summary>
    public required string Type { get; init; }

    /// <summary>Shorthand: the button can receive interaction.</summary>
    public bool Interactive => !Disabled && !Loading;
}
