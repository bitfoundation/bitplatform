namespace Bit.Balette;

/// <summary>
/// A mode scopes the palette to a subset of commands/sources, activated by typing a prefix
/// (VS Code <c>&gt;</c> for commands, GitHub <c>#</c> for issues, etc.).
/// </summary>
public sealed class BaletteCommandMode
{
    /// <summary>Prefix that activates the mode, e.g. <c>&gt;</c>, <c>#</c>, <c>@</c>, <c>/</c>.</summary>
    public required string Prefix { get; init; }

    /// <summary>Human readable name shown as a chip next to the input.</summary>
    public required string Name { get; init; }

    /// <summary>Placeholder shown while the mode is active.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Help text listed in the default mode's "modes" hint group.</summary>
    public string? Description { get; init; }

    /// <summary>Only registry commands with one of these tags are shown. Null = no tag filter.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Only registry commands with one of these groups are shown. Null = no group filter.</summary>
    public IReadOnlyList<string>? Groups { get; init; }

    /// <summary>Hide registry commands entirely (only <see cref="Sources"/> are used).</summary>
    public bool HideRegistryCommands { get; init; }

    /// <summary>Async sources queried in this mode.</summary>
    public IReadOnlyList<IBaletteSource> Sources { get; init; } = [];

    /// <summary>Whether a hint item for this mode is listed in the default mode when the query is empty.</summary>
    public bool ShowHint { get; init; } = true;

    internal bool Accepts(BaletteCommand c)
    {
        if (HideRegistryCommands)
        {
            return false;
        }

        if (Tags is { Count: > 0 } && !c.Tags.Any(t => Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (Groups is { Count: > 0 } && (c.Group is null || !Groups.Contains(c.Group, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}
