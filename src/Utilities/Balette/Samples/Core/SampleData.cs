using System.Globalization;

namespace Bit.Balette.Samples.Core;

/// <summary>
/// A stand-in for the APIs a real app's pages and sources would call: in-memory data behind a delay, so the palette's
/// loading state, debounce, spinner and cancellation can be seen at work in every render mode.
/// </summary>
public static class SampleData
{
    public sealed record Person(string Name, string Role);

    public sealed record Issue(int Number, string Title, bool IsOpen);

    public sealed record IssuePage(IReadOnlyList<Issue> Items, string? NextCursor, int TotalCount);

    public sealed record DocTopic(string Title, string Anchor, string Summary);

    private static readonly string[] _verbs = ["Fix", "Improve", "Document", "Test", "Refactor", "Speed up"];

    private static readonly string[] _areas = ["keyboard shortcuts", "fuzzy matching", "nested pages", "async sources", "theming", "right-to-left layout", "history store", "virtualization"];

    public static IReadOnlyList<Person> People { get; } =
    [
        new("Ada Lovelace", "Analyst"),
        new("Alan Turing", "Cryptographer"),
        new("Barbara Liskov", "Architect"),
        new("Donald Knuth", "Typesetter"),
        new("Edsger Dijkstra", "Algorithms"),
        new("Frances Allen", "Compilers"),
        new("Grace Hopper", "Compilers"),
        new("Katherine Johnson", "Navigation"),
        new("Ken Thompson", "Operating systems"),
        new("Leslie Lamport", "Distributed systems"),
        new("Margaret Hamilton", "Flight software"),
        new("Radia Perlman", "Networking"),
    ];

    public static IReadOnlyList<Issue> Issues { get; } =
        [.. Enumerable.Range(0, 120).Select(i => new Issue(1000 + i, $"{_verbs[i % _verbs.Length]} {_areas[i % _areas.Length]}", i % 3 != 0))];

    public static IReadOnlyList<DocTopic> DocTopics { get; } =
    [
        new("Getting started", "getting-started", "Install, register the services, place the palette"),
        new("Commands", "commands", "The model, the fluent builder, toggles and settings"),
        new("Declarative markup", "declarative-markup", "BaletteGroup and BaletteItem"),
        new("Inline palettes", "inline-palettes", "A palette in the page rather than over it"),
        new("Handlers and results", "handlers-and-results", "Prompts, multi-select, toasts and errors"),
        new("Nested pages", "nested-pages", "Static, lazy, declarative and ad-hoc pages"),
        new("Search and ranking", "search-and-ranking", "The fuzzy matcher, operators, typos and frecency"),
        new("Modes", "modes", "Prefixes that scope the registry and the sources"),
        new("Async sources", "async-sources", "Debounced, cancellable searches against an API"),
        new("Keyboard", "keyboard", "Global shortcuts, sequences and platform-aware hints"),
        new("Theming", "theming", "CSS variables, light and dark, density"),
        new("Hosting models", "hosting-models", "WebAssembly, Server, Web App and Hybrid"),
    ];

    public static async Task<IReadOnlyList<Person>> GetPeopleAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(400, cancellationToken);
        return People;
    }

    public static async Task<IReadOnlyList<Person>> SearchPeopleAsync(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);
        return [.. People.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>One page of the issues matching <paramref name="query"/>; the cursor is the offset of the next page.</summary>
    public static async Task<IssuePage> SearchIssuesAsync(string query, string? cursor, int take, CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);

        var matches = Issues.Where(i => i.Number.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.Ordinal)
                                     || i.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        var skip = cursor is null ? 0 : int.Parse(cursor, CultureInfo.InvariantCulture);
        var next = skip + take < matches.Count ? (skip + take).ToString(CultureInfo.InvariantCulture) : null;

        return new([.. matches.Skip(skip).Take(take)], next, matches.Count);
    }
}
