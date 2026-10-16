namespace Bit.Blex.Demo.Client;

/// <summary>One page in the documentation.</summary>
/// <param name="Href">Route, without a leading slash.</param>
/// <param name="Title">Sidebar and heading title.</param>
/// <param name="Summary">One-line description used by the "next up" cards and search.</param>
public sealed record DocEntry(string Href, string Title, string Summary);

/// <summary>A group of related pages in the sidebar.</summary>
/// <param name="Title">Group heading.</param>
/// <param name="Pages">Ordered pages.</param>
public sealed record DocSection(string Title, IReadOnlyList<DocEntry> Pages);

/// <summary>
/// The single source of truth for the site map: the sidebar, the previous/next footer links and
/// the introduction's contents grid all read from here, so adding a page in one place is enough.
/// </summary>
public static class DocsNav
{
    /// <summary>Every section in sidebar order.</summary>
    public static IReadOnlyList<DocSection> Sections { get; } =
    [
        new("Getting started",
        [
            new("", "Introduction", "What Blex is, and how it compares to the alternatives."),
            new("installation", "Installation", "Packages, DI registration and the provider component."),
            new("quick-start", "Quick start", "A working counter in three files."),
        ]),
        new("Core concepts",
        [
            new("stores", "Stores & state", "[BlexStore] and [BlexState]: the reactive container and its fields."),
            new("computed", "Computed state", "[BlexComputed]: memoized derived values that invalidate themselves."),
            new("actions", "Actions", "[BlexAction], batching, direct assignment, Batch and ResetState."),
            new("effects", "Effects", "[BlexEffect]: async work with generated loading, error and cancellation."),
        ]),
        new("Reacting to state",
        [
            new("subscriptions", "Components & selectors", "Re-render on the state you actually use."),
            new("cross-store", "Cross-store coordination", "React to one store's actions from another."),
            new("entities", "Entity adapter", "Normalized collections with generated CRUD."),
        ]),
        new("The pipeline",
        [
            new("middleware", "Middleware", "Observe every action, or veto it before it runs."),
            new("errors", "Error isolation", "Where non-fatal pipeline failures go."),
        ]),
        new("State lifecycle",
        [
            new("persistence", "Persistence", "Rehydration, debouncing, versioning and migrations."),
            new("history", "Undo & redo", "In-app history over the whole state tree."),
            new("devtools", "DevTools & time travel", "The Redux DevTools bridge, and how to secure it."),
        ]),
        new("Quality",
        [
            new("testing", "Testing", "The harness, the action log and waiting on state."),
            new("diagnostics", "Compile-time diagnostics", "Every BLEX#### the generator can report."),
        ]),
        new("Reference",
        [
            new("api", "API reference", "Every public type and member, grouped by area."),
            new("mcp", "MCP server", "Let an AI agent work against Blex's real API, docs and generator."),
        ]),
    ];

    /// <summary>All pages, flattened into reading order.</summary>
    public static IReadOnlyList<DocEntry> Flat { get; } = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>Finds the entry for a route, or <c>null</c> when the route is not in the site map.</summary>
    public static DocEntry? Find(string href)
    {
        var normalized = href.Trim('/');
        return Flat.FirstOrDefault(p => string.Equals(p.Href, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The pages before and after <paramref name="href"/> in reading order.</summary>
    public static (DocEntry? Previous, DocEntry? Next) Neighbours(string href)
    {
        var normalized = href.Trim('/');
        var index = -1;
        for (var i = 0; i < Flat.Count; i++)
        {
            if (string.Equals(Flat[i].Href, normalized, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return (null, null);

        return (index > 0 ? Flat[index - 1] : null,
                index < Flat.Count - 1 ? Flat[index + 1] : null);
    }

    /// <summary>The section a page belongs to.</summary>
    public static DocSection? SectionOf(string href)
    {
        var normalized = href.Trim('/');
        return Sections.FirstOrDefault(s => s.Pages.Any(p => string.Equals(p.Href, normalized, StringComparison.OrdinalIgnoreCase)));
    }
}
