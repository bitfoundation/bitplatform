namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by disclosure parts.</summary>
public sealed record BitBhDisclosureRenderContext
{
    /// <summary>Whether the panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the disclosure (e.g. from a link inside the panel).</summary>
    public required Action Close { get; init; }
}
