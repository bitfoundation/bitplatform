using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Demo.Client.Services;

public sealed record Person(string Name, string Role, string Email);

public sealed record Issue(int Number, string Title, string State, string[] Labels);

public sealed record Doc(string Title, string Path, string Excerpt);

/// <summary>
/// An in-memory directory standing in for the API a real application would query. It is resolved from DI
/// wherever it is used - by the sources through <see cref="BaletteSourceContext.Services"/>, by the lazy
/// "Assign to…" page through <see cref="BaletteCommandContext.Services"/> - which is where a real HTTP client
/// would come from too.
/// </summary>
public sealed class FakeDirectory
{
    public IReadOnlyList<Person> People { get; } =
    [
        new("Ada Lovelace", "Engineering", "ada@example.com"),
        new("Grace Hopper", "Engineering", "grace@example.com"),
        new("Alan Turing", "Research", "alan@example.com"),
        new("Katherine Johnson", "Data", "katherine@example.com"),
        new("Margaret Hamilton", "Engineering", "margaret@example.com"),
        new("Linus Torvalds", "Platform", "linus@example.com"),
        new("Barbara Liskov", "Architecture", "barbara@example.com"),
        new("Dennis Ritchie", "Platform", "dennis@example.com"),
        new("Hedy Lamarr", "Research", "hedy@example.com"),
        new("Tim Berners-Lee", "Web", "tim@example.com"),
    ];

    public IReadOnlyList<Issue> Issues { get; } =
    [
        new(101, "Palette flickers when opened during navigation", "open", ["bug", "ui"]),
        new(102, "Add vim keybindings", "closed", ["enhancement"]),
        new(103, "Support Ctrl+Shift+P as alternative toggle", "closed", ["enhancement"]),
        new(104, "Recents should survive page reload", "open", ["bug"]),
        new(105, "Nested pages lose query on back", "open", ["bug"]),
        new(106, "Document theming variables", "open", ["docs"]),
        new(107, "Virtualize long result lists", "open", ["performance"]),
        new(108, "Fuzzy match should prefer word starts", "closed", ["search"]),
        new(109, "Screen reader announces result count", "closed", ["a11y"]),
        new(110, "Mobile layout: full screen dialog", "open", ["ui"]),
    ];

    // Every entry points at the demo page that shows it, so a documentation hit is a working link.
    public IReadOnlyList<Doc> Docs { get; } =
    [
        new("Getting started", "/#get-started", "Install the package, add the services, put the component in your layout."),
        new("Registering commands", "/commands", "Fluent builder, declarative markup, page-scoped registration."),
        new("Nested pages and prompts", "/pages", "Multi-step flows, action panels, free-text prompts, multi-select."),
        new("Search operators", "/search#operators", "Exact, prefix, suffix and negated terms, combined with AND."),
        new("Modes and prefixes", "/modes", "Scope the palette with > # @ ? prefixes like VS Code and GitHub."),
        new("Async sources", "/modes#sources", "Debounced, cancellable searches against your APIs."),
        new("Keyboard shortcuts", "/keyboard#shortcuts", "Global chords and sequences like G then I."),
        new("Accessibility", "/keyboard#accessibility", "ARIA combobox pattern, focus management, live announcements."),
        new("Theming", "/theming", "CSS custom properties, dark mode, templates, unstyled mode."),
        new("Large registries", "/performance", "Thousands of commands, measured per keystroke."),
    ];
}

/// <summary>
/// Simulates a remote people search with latency. <see cref="ApplyMatcher"/> is true, so the palette re-scores
/// what comes back with its own matcher: the server only has to return candidates, and the highlighting matches
/// the rest of the list.
/// </summary>
public sealed class PeopleSource(SourceLog log) : IBaletteSource
{
    public string Name => "People";

    public int MinQueryLength => 1;

    public bool ApplyMatcher => true;

    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
        => log.TrackAsync(Name, query, async ct =>
        {
            await Task.Delay(250, ct);
            var directory = context.Services.GetRequiredService<FakeDirectory>();
            return directory.People
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(p => BaletteCmd.New(p.Name)
                    .Id($"person:{p.Email}")
                    .Subtitle(p.Role)
                    .Keywords(p.Email)
                    .Icon("demo-avatar")
                    .Data(p)
                    // Tab opens these next to the built-in pin and forget actions.
                    .Actions(
                        BaletteCmd.Action("Copy e-mail", _ => Task.FromResult(BaletteCommandResult.Toast($"Copied {p.Email}"))).Build(),
                        BaletteCmd.Action("Mention in issue", _ => Task.FromResult(BaletteCommandResult.Toast($"Mentioned {p.Name}"))).Build())
                    .Handler(_ => Task.FromResult(BaletteCommandResult.Toast($"Opening the profile of {p.Name}")))
                    .Build())
                .ToList();
        }, cancellationToken);
}

/// <summary>
/// A source whose own order is kept (<c>ApplyMatcher</c> is false): open issues first, newest first, the way a
/// server that already sorted by relevance would answer. The results are listed as one block under the source's name.
/// </summary>
public sealed class IssueSource(SourceLog log) : IBaletteSource
{
    public string Name => "Issues";

    // Zero asks on the empty query too, so the mode opens on a list instead of a blank box.
    public int MinQueryLength => 0;

    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
        => log.TrackAsync(Name, query, async ct =>
        {
            await Task.Delay(180, ct);
            IEnumerable<Issue> hits = context.Services.GetRequiredService<FakeDirectory>().Issues;
            if (query.Length > 0)
            {
                hits = int.TryParse(query.TrimStart('#'), out var number)
                    ? hits.Where(i => i.Number.ToString().StartsWith(number.ToString(), StringComparison.Ordinal))
                    : hits.Where(i => i.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || i.Labels.Any(l => l.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            return hits
                .OrderByDescending(i => i.State == "open")
                .ThenByDescending(i => i.Number)
                .Select(i => BaletteCmd.New($"#{i.Number} {i.Title}")
                    .Id($"issue:{i.Number}")
                    .Subtitle(string.Join(" · ", i.Labels))
                    .Badge(i.State)
                    .Kind(BaletteCommandKind.Navigation)
                    .Handler(_ => Task.FromResult(BaletteCommandResult.Toast($"Opened issue #{i.Number}")))
                    .Build())
                .ToList();
        }, cancellationToken);
}

/// <summary>
/// Searched in the default mode next to the registry. It returns every document and lets the matcher decide,
/// and its low priority keeps a command ahead of an equally good documentation hit.
/// </summary>
public sealed class DocsSource(SourceLog log) : IBaletteSource
{
    public string Name => "Documentation";

    // One letter matches nearly every document; two is where a hit starts to mean something.
    public int MinQueryLength => 2;

    public bool ApplyMatcher => true;

    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
        => log.TrackAsync(Name, query, async ct =>
        {
            await Task.Delay(300, ct);
            return context.Services.GetRequiredService<FakeDirectory>().Docs
                .Select(d => BaletteCmd.New(d.Title)
                    .Id($"doc:{d.Path}")
                    .Subtitle(d.Excerpt)
                    .Href(d.Path)
                    // Href makes a command a navigation; the kind is set after it to keep the help icon.
                    .Kind(BaletteCommandKind.Help)
                    .Priority(BaletteCommandPriority.Low)
                    .Build())
                .ToList();
        }, cancellationToken);
}
