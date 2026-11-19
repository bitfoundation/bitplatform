namespace Bit.Bheadless;

/// <summary>Everything the JS runtime needs to open a floating surface.</summary>
public sealed class BitBhFloatingRequest
{
    /// <summary>The id of the content element (layer, focus scope, positioning target).</summary>
    public required string ContentId { get; init; }

    /// <summary>
    /// Elements moved by the portal (in order) and kept visible by modal isolation.
    /// Defaults to <see cref="ContentId"/>. Use it to move a wrapper (dialog root)
    /// or several siblings (overlay + content) together.
    /// </summary>
    public IReadOnlyList<string?>? SurfaceIds { get; init; }

    /// <summary>Teleport the surface to the portal root (or <see cref="PortalTarget"/>) while open.</summary>
    public bool Portal { get; init; }

    /// <summary>Optional id / CSS selector of the portal container.</summary>
    public string? PortalTarget { get; init; }

    /// <summary>Id of the reference element used for anchor positioning.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>Virtual reference point (viewport coordinates) used instead of <see cref="ReferenceId"/>.</summary>
    public (double X, double Y)? Point { get; init; }

    /// <summary>Anchor positioning options; null disables positioning.</summary>
    public BitBhAnchorOptions? Anchor { get; init; }

    /// <summary>Id of an arrow element to position along the reference.</summary>
    public string? ArrowId { get; init; }

    /// <summary>Dismissal behaviour; null disables the layer.</summary>
    public BitBhLayerOptions? Layer { get; init; }

    /// <summary>Hide the rest of the page from assistive tech and lock scrolling.</summary>
    public bool Modal { get; init; }

    /// <summary>Lock document scroll when <see cref="Modal"/>. Default: true.</summary>
    public bool LockScroll { get; init; } = true;

    /// <summary>Focus management; null leaves focus alone.</summary>
    public BitBhFocusScopeOptions? Focus { get; init; }

    /// <summary>Whether the content is only revealed by the runtime (anchored or portalled).</summary>
    public bool RevealedByScript => Anchor is not null || Portal;

    internal Dictionary<string, object?> ToJs()
    {
        object? reference = Point is { } p
            ? new Dictionary<string, object?> { ["x"] = p.X, ["y"] = p.Y }
            : ReferenceId;

        return new Dictionary<string, object?>
        {
            ["portalIds"] = SurfaceIds?.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
            ["portal"] = Portal ? (object?)PortalTarget ?? true : null,
            ["reference"] = reference,
            ["anchor"] = Anchor?.ToJs(ArrowId),
            ["layer"] = Layer?.ToJs(),
            ["modal"] = Modal ? new Dictionary<string, object?> { ["hideOthers"] = true, ["scrollLock"] = LockScroll } : null,
            ["focus"] = Focus?.ToJs(),
        };
    }
}
