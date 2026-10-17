namespace Bit.Balette.Benchmarks;

/// <summary>
/// Realistic inputs for the benchmarks. Synthetic registries are shaped like the palette of a real application rather
/// than random strings: half of the commands are actions built from verb and object phrases ("Toggle Side Bar",
/// "Export Report as CSV") and half are navigation entries to named records ("Aurora Logistics", a customer), grouped
/// by area, with subtitles, aliases, descriptions and tags on a realistic share of them. That gives the matcher the
/// word boundaries, shared prefixes and near misses that decide what a keystroke costs; random letters would be
/// fast-rejected almost every time and flatter the numbers.
/// </summary>
internal static class Workloads
{
    /// <summary>The default seed, so every machine benchmarks the same registry.</summary>
    public const int Seed = 20261017;

    private static readonly string[] Verbs =
    [
        "Open", "Close", "Create", "Delete", "Rename", "Duplicate", "Toggle", "Show", "Hide", "Copy", "Move", "Run",
        "Search", "Find", "Format", "Export", "Import", "Sync", "Reload", "Reset", "Share", "Archive", "Restore", "Pin",
        "Split", "Focus", "Collapse", "Expand", "Sort", "Filter", "Preview", "Publish", "Configure", "Refresh", "Clear",
        "Select",
    ];

    // A few non-ASCII names keep the char.ToLowerInvariant path of the matcher's case folding in the mix, as a
    // localized application would.
    private static readonly (string Name, string Group)[] Objects =
    [
        ("Settings", "Preferences"), ("Keyboard Shortcuts", "Preferences"), ("Color Theme", "Preferences"),
        ("Préférences", "Preferences"), ("File", "File"), ("Folder", "File"), ("Recent Files", "File"),
        ("Workspace", "File"), ("Side Bar", "View"), ("Panel", "View"), ("Status Bar", "View"), ("Minimap", "View"),
        ("Zen Mode", "View"), ("Breadcrumbs", "View"), ("Word Wrap", "View"), ("Übersicht", "View"),
        ("Selection", "Edit"), ("Line", "Edit"), ("Comment", "Edit"), ("Snippet", "Edit"), ("Branch", "Git"),
        ("Commit", "Git"), ("Pull Request", "Git"), ("Stash", "Git"), ("Terminal", "Terminal"), ("Task", "Terminal"),
        ("Breakpoint", "Debug"), ("Debug Console", "Debug"), ("Watch Expression", "Debug"), ("Customer", "Sales"),
        ("Order", "Sales"), ("Invoice", "Billing"), ("Subscription", "Billing"), ("Report", "Reports"),
        ("Dashboard", "Reports"), ("User", "Admin"), ("Team", "Admin"), ("Role", "Admin"), ("API Key", "Admin"),
        ("Webhook", "Admin"), ("Notification", "Help"), ("Release Notes", "Help"), ("Résumé", "Account"),
        ("Profile", "Account"),
    ];

    private static readonly string[] Qualifiers =
    [
        "in New Window", "to the Side", "for Current File", "in Explorer", "as JSON", "as CSV", "and Close",
        "from Clipboard", "for All Users", "(Preview)", "in Background", "with Defaults",
    ];

    private static readonly string[] Sections =
    [
        "Appearance", "Layout", "Editor", "Workbench", "Source Control", "Accounts", "Billing", "Security",
        "Integrations", "Reports", "General",
    ];

    private static readonly string[] Aliases =
    [
        "prefs", "options", "config", "sidebar", "explorer", "layout", "vcs", "scm", "remove", "trash", "new", "add",
        "lookup", "jump", "navigate", "switch", "clone", "download", "upload", "link", "cmd", "shell", "billing",
        "users", "permissions", "theme", "dark", "light", "font", "zoom",
    ];

    private static readonly string[] DescriptionTails =
    [
        "Applies to the active editor and to every window opened afterwards; it can be undone with Ctrl+Z.",
        "Requires the Admin role. The change is recorded in the audit log and synchronized to your other devices.",
        "Uses the current selection when there is one, otherwise the whole document.",
        "Available offline; results are refreshed as soon as the connection comes back.",
        "Takes effect immediately and is remembered for the next session.",
    ];

    private static readonly string[] TagPool =
    [
        "layout", "git", "editor", "admin", "billing", "beta", "experimental", "ui", "data", "accessibility",
    ];

    private static readonly (string Name, string Group, string Code)[] Entities =
    [
        ("Customer", "Customers", "CUS"), ("Project", "Projects", "PRJ"), ("Invoice", "Invoices", "INV"),
        ("Page", "Pages", "PAG"), ("Document", "Documents", "DOC"), ("Repository", "Repositories", "REP"),
        ("Channel", "Channels", "CHN"), ("Employee", "People", "EMP"),
    ];

    private static readonly string[] NamePrefixes =
    [
        "Aurora", "Nimbus", "Quartz", "Cobalt", "Summit", "Harbor", "Atlas", "Beacon", "Cedar", "Delta", "Ember",
        "Falcon", "Granite", "Horizon", "Ivory", "Juniper", "Lumen", "Meridian", "Northwind", "Orbit", "Pioneer",
        "Redwood", "Sierra", "Tundra", "Vertex", "Willow", "Zenith", "Søndergaard", "Müller",
    ];

    private static readonly string[] NameSuffixes =
    [
        "Logistics", "Analytics", "Labs", "Systems", "Studio", "Partners", "Foods", "Health", "Media", "Energy",
        "Robotics", "Finance", "Outfitters", "Ventures", "Works",
    ];

    private static readonly string[] Regions = ["EMEA", "North America", "LATAM", "APAC", "Internal"];

    /// <summary>A registry of <paramref name="count"/> top-level commands.</summary>
    public static BaletteCommand[] Registry(int count, int seed = Seed) => [.. Commands(seed).Take(count)];

    /// <summary>
    /// The first <paramref name="count"/> matches a query produces over a registry, with the scores the matcher really
    /// gives them. What ranking costs depends on how many ranks tie (ties fall through to a culture-aware title
    /// comparison), which made-up scores would get wrong.
    /// </summary>
    public static List<BaletteMatch> Matches(int count, string query, int seed = Seed)
    {
        IBaletteMatcher matcher = BaletteFuzzyMatcher.Default;
        var parsed = BaletteQuery.Parse(query);
        var weights = new BaletteMatchWeights();
        var matches = new List<BaletteMatch>(count);
        foreach (var command in Commands(seed))
        {
            if (matcher.Match(command, parsed, weights) is { } match)
            {
                matches.Add(match);
                if (matches.Count == count)
                {
                    break;
                }
            }
        }

        return matches;
    }

    /// <summary>What the palette lists for an empty query: every command, unscored, ordered by priority and frecency.</summary>
    public static List<BaletteMatch> Unscored(IEnumerable<BaletteCommand> commands)
        => [.. commands.Select(c => new BaletteMatch(c, 0, [], [], null))];

    /// <summary>
    /// A loaded history in which a quarter of the registry has been used, capped at the default
    /// <see cref="BaletteOptions.MaxHistoryEntries"/> of 200, with usage spread over two months so that every frecency
    /// bucket is hit. Ranking asks the history for the frecency of every match, so a realistic history, and the real
    /// <see cref="BaletteHistory"/> rather than a stub, is part of the keystroke cost.
    /// </summary>
    public static BaletteHistory History(IReadOnlyList<BaletteCommand> commands, BaletteOptions options, int seed = Seed)
    {
        var random = new Random(seed);
        var entries = Math.Min(options.MaxHistoryEntries, commands.Count / 4);
        var indexes = Enumerable.Range(0, commands.Count).ToArray();
        random.Shuffle(indexes);

        var now = DateTimeOffset.UtcNow;
        var usage = new Dictionary<string, BaletteCommandUsage>(StringComparer.Ordinal);
        for (var i = 0; i < entries; i++)
        {
            var lastUsed = now - TimeSpan.FromHours(random.NextDouble() * 24 * 60);
            usage[commands[indexes[i]].Id] = new BaletteCommandUsage(1 + random.Next(30), lastUsed, random.Next(40) == 0);
        }

        // The in-memory store completes synchronously, so blocking here cannot deadlock.
        var store = new BaletteInMemoryHistoryStore();
        store.SaveAsync(usage).AsTask().GetAwaiter().GetResult();
        var history = new BaletteHistory(store, options);
        history.EnsureLoadedAsync().GetAwaiter().GetResult();
        return history;
    }

    /// <summary>
    /// A command with every searchable field populated (title, subtitle, group, four keywords, a description and two
    /// tags): the most fields a single term can be scored against, so the most work per command.
    /// </summary>
    public static BaletteCommand RichCommand() => new()
    {
        Id = "view.toggle-primary-side-bar",
        Title = "Toggle Primary Side Bar Visibility",
        Subtitle = "View › Appearance",
        Group = "View",
        Keywords = ["sidebar", "explorer", "panel", "layout"],
        Description = "Shows or hides the primary side bar, which hosts the explorer, search, source control and extensions views.",
        Tags = ["layout", "ui"],
    };

    /// <summary>
    /// Help-article style prose of exactly <paramref name="length"/> characters, for long description fields. It
    /// mentions "rollback" so a query for it matches rather than being fast-rejected.
    /// </summary>
    public static string LongText(int length)
    {
        var builder = new System.Text.StringBuilder(length + 128);
        builder.Append("Deployments can be paused, resumed or rolled back from this page. ");
        for (var i = 0; builder.Length < length; i++)
        {
            builder.Append(DescriptionTails[i % DescriptionTails.Length]).Append(' ');
            if (i == 3)
            {
                builder.Append("A rollback restores the previous release and keeps its configuration. ");
            }
        }

        return builder.ToString(0, length);
    }

    /// <summary>
    /// An endless deterministic stream of commands, alternating actions and navigation entries. Ids are explicit and
    /// unique so that each history entry belongs to exactly one command, as in a real registry.
    /// </summary>
    private static IEnumerable<BaletteCommand> Commands(int seed)
    {
        var random = new Random(seed);
        for (var i = 0; ; i++)
        {
            yield return i % 2 == 0 ? Action(random, i) : Navigation(random, i);
        }
    }

    /// <summary>An action such as "Export Report as CSV", in the group of the thing it acts on.</summary>
    private static BaletteCommand Action(Random random, int index)
    {
        var verb = Verbs[random.Next(Verbs.Length)];
        var (name, group) = Objects[random.Next(Objects.Length)];
        var title = random.Next(4) == 0 ? $"{verb} {name} {Qualifiers[random.Next(Qualifiers.Length)]}" : $"{verb} {name}";

        return new BaletteCommand
        {
            Id = $"cmd.{index:D6}",
            Title = title,
            Subtitle = random.Next(2) == 0 ? $"{group} › {Sections[random.Next(Sections.Length)]}" : null,
            Group = group,
            Keywords = Pick(random, Aliases, random.Next(4)),
            Description = random.Next(4) == 0
                ? $"{verb} the {name.ToLowerInvariant()}. {DescriptionTails[random.Next(DescriptionTails.Length)]}"
                : null,
            Tags = Pick(random, TagPool, random.Next(3)),
            Priority = random.Next(20) switch
            {
                0 => BaletteCommandPriority.High,
                1 => BaletteCommandPriority.Low,
                _ => BaletteCommandPriority.Normal,
            },
        };
    }

    /// <summary>A navigation entry to a named record, such as the customer "Aurora Logistics", with its record code as an alias.</summary>
    private static BaletteCommand Navigation(Random random, int index)
    {
        var (entity, group, code) = Entities[random.Next(Entities.Length)];
        var title = $"{NamePrefixes[random.Next(NamePrefixes.Length)]} {NameSuffixes[random.Next(NameSuffixes.Length)]}";

        return new BaletteCommand
        {
            Id = $"nav.{index:D6}",
            Title = title,
            Subtitle = $"{entity} · {Regions[random.Next(Regions.Length)]}",
            Group = group,
            Keywords = [$"{code}-{index:D5}"],
            Tags = random.Next(3) == 0 ? ["navigation"] : [],
            Kind = BaletteCommandKind.Navigation,
            Href = $"/{group.ToLowerInvariant()}/{index}",
        };
    }

    /// <summary>Up to <paramref name="count"/> distinct entries of <paramref name="pool"/>.</summary>
    private static string[] Pick(Random random, string[] pool, int count)
    {
        if (count == 0)
        {
            return [];
        }

        var picked = new HashSet<string>(StringComparer.Ordinal);
        while (picked.Count < count)
        {
            picked.Add(pool[random.Next(pool.Length)]);
        }

        return [.. picked];
    }
}
