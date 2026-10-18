namespace Bit.Balette.Demo.Client.Services;

/// <summary>
/// The commands the layout registers for the whole app, written with the fluent builder end to end. They are
/// rebuilt from <see cref="DemoState"/> every time it changes, so a toggle's switch and a setting's value are
/// always the current ones: <c>BaletteCommands</c> swaps the registration when it is handed a new list.
/// </summary>
public static class DemoCommands
{
    public const string GitHubUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Balette";

    public const string NuGetUrl = "https://www.nuget.org/packages/Bit.Balette";

    // Commands tagged with it are the ones the ">" mode lists: everything that does something, nothing that only
    // navigates.
    public const string CommandTag = "command";

    public static IEnumerable<BaletteCommand> Global(DemoState state) =>
    [
        // ---- Navigation: every page, also reachable with G-then-X sequences, the way Linear and GitHub do it ----
        BaletteCmd.Navigate("Introduction", "/").Group("Navigation").Shortcut("G H").Keywords("home", "start", "landing").Priority(BaletteCommandPriority.High),
        BaletteCmd.Navigate("Registering commands", "/commands").Group("Navigation").Shortcut("G R").Keywords("register", "builder", "declarative", "counter"),
        BaletteCmd.Navigate("Settings", "/commands#settings").Group("Navigation").Shortcut("G S").Keywords("preferences", "options", "config"),
        BaletteCmd.Navigate("Nested pages and flows", "/pages").Group("Navigation").Shortcut("G P").Keywords("prompt", "multi-select", "confirm", "toast", "actions"),
        BaletteCmd.Navigate("Search and ranking", "/search").Group("Navigation").Shortcut("G F").Keywords("fuzzy", "find", "score", "frecency", "operators"),
        BaletteCmd.Navigate("Modes and sources", "/modes").Group("Navigation").Shortcut("G M").Keywords("prefix", "async", "debounce"),
        BaletteCmd.Navigate("Keyboard and shortcuts", "/keyboard").Group("Navigation").Shortcut("G K").Keywords("hotkeys", "sequence", "accessibility"),
        BaletteCmd.Navigate("Theming and templates", "/theming").Group("Navigation").Shortcut("G T").Keywords("dark", "css", "variables", "detail"),
        BaletteCmd.Navigate("Large registry", "/performance").Group("Navigation").Shortcut("G L").Keywords("performance", "benchmark", "bulk"),
        BaletteCmd.Navigate("MCP server", "/mcp-server").Group("Navigation").Shortcut("G A").Keywords("ai", "agent", "model context protocol"),
        BaletteCmd.New("GitHub repository").Href(GitHubUrl, newTab: true).Group("Navigation").Keywords("source", "code", "repo"),
        BaletteCmd.New("NuGet package").Href(NuGetUrl, newTab: true).Group("Navigation").Keywords("install", "package", "download"),

        // ---- Actions: one of each kind of flow ----
        BaletteCmd.New("Rename project…")
            .Group("Actions")
            .Shortcut("F2")
            .Keywords("title", "name")
            .Tags(CommandTag)
            // A prompt: the input turns into a text box pre-filled with the current name, and Enter submits it.
            .Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
                "New project name",
                ctx =>
                {
                    if (!string.IsNullOrWhiteSpace(ctx.PromptValue))
                    {
                        state.Rename(ctx.PromptValue.Trim());
                    }

                    return Task.FromResult(BaletteCommandResult.Toast($"Renamed to “{ctx.PromptValue}”"));
                },
                placeholder: "Project name…",
                initialValue: state.ProjectName))),

        BaletteCmd.New("Set labels…")
            .Group("Actions")
            .Keywords("tags", "categorize")
            .Tags(CommandTag)
            // A multi-select page: Enter or Space checks a row, Ctrl+Enter hands the checked ones to the handler.
            .MultiSelect()
            .ChildrenPlaceholder("Toggle labels, Ctrl+Enter to apply…")
            .Children(
                BaletteCmd.New("bug").Id("label:bug").Icon("demo-dot demo-dot-red"),
                BaletteCmd.New("enhancement").Id("label:enhancement").Icon("demo-dot demo-dot-blue"),
                BaletteCmd.New("documentation").Id("label:documentation").Icon("demo-dot demo-dot-green"),
                BaletteCmd.New("performance").Id("label:performance").Icon("demo-dot demo-dot-yellow"),
                BaletteCmd.New("accessibility").Id("label:accessibility").Icon("demo-dot demo-dot-purple"))
            .Handler(ctx =>
            {
                state.SetLabels(ctx.SelectedCommands.Select(c => c.Title));
                return Task.FromResult(BaletteCommandResult.Toast($"{ctx.SelectedCommands.Count} labels applied"));
            }),

        BaletteCmd.New("Assign to…")
            .Group("Actions")
            .Keywords("owner", "person", "user")
            .Tags(CommandTag)
            // A lazily loaded page: the provider runs when the page opens, and the palette shows its loading state
            // until it returns.
            .Children(async (ctx, ct) =>
            {
                await Task.Delay(400, ct);
                return ctx.GetRequiredService<FakeDirectory>().People
                    .Select(p => BaletteCmd.New(p.Name)
                        .Subtitle(p.Role)
                        .Icon("demo-avatar")
                        .Data(p)
                        .Handler(_ =>
                        {
                            state.Record($"Assigned to {p.Name}");
                            return Task.FromResult(BaletteCommandResult.Toast($"Assigned to {p.Name}"));
                        })
                        .Build())
                    .ToList();
            }, placeholder: "Pick an assignee…"),

        BaletteCmd.New("Copy share link")
            .Group("Actions")
            .Shortcut("Mod+Shift+C")
            .Keywords("url", "clipboard")
            .Tags(CommandTag)
            .Handler(_ =>
            {
                state.Record("Share link copied");
                return Task.FromResult(BaletteCommandResult.Toast("Link copied to clipboard"));
            }),

        BaletteCmd.New("Export…")
            .Group("Actions")
            .Keywords("download", "save as")
            .Tags(CommandTag)
            // An ad-hoc page: the handler decides what to list when it runs, instead of the command declaring it.
            .Handler(_ => Task.FromResult(BaletteCommandResult.Page("Export format",
            [
                BaletteCmd.Action("CSV", () => state.Record("Exported CSV")).Subtitle("Comma separated values").Build(),
                BaletteCmd.Action("JSON", () => state.Record("Exported JSON")).Subtitle("Pretty printed").Build(),
                BaletteCmd.Action("PDF", () => state.Record("Exported PDF")).Subtitle("Print friendly").Badge("Pro").Disabled(true, "Requires the Pro plan").Build(),
            ]))),

        // ---- Preferences: toggles keep the palette open and update in place ----
        BaletteCmd.New("Theme")
            .Group("Preferences")
            .Keywords("appearance", "dark", "light", "color scheme")
            .Tags(CommandTag)
            .Value(Capitalize(state.Theme))
            .Children(
                ThemeOption(state, "auto", "Follow the operating system"),
                ThemeOption(state, "light", "Always light"),
                ThemeOption(state, "dark", "Always dark")),

        Toggle("Word wrap", "wordwrap", state.WordWrap, state.ToggleWordWrap, "Alt+Z", "soft wrap lines"),
        Toggle("Desktop notifications", "notifications", state.Notifications, state.ToggleNotifications, null, "alerts push"),
        Toggle("Show minimap", "minimap", state.ShowMinimap, state.ToggleMinimap, null, "outline overview"),
        // Changes the palette it is listed in: the detail pane appears or goes while the palette stays open.
        Toggle("Show detail pane", "detail", state.ShowDetail, () => state.SetShowDetail(!state.ShowDetail), null, "preview sidebar raycast"),

        // ---- Danger zone: a destructive command needs a second Enter ----
        BaletteCmd.New("Delete project")
            .Group("Danger zone")
            .Keywords("remove", "destroy")
            .Tags(CommandTag)
            .Destructive("This permanently deletes the project")
            .Handler(_ =>
            {
                state.Record("Project deleted (not really)");
                return Task.FromResult(BaletteCommandResult.Toast("Project deleted"));
            }),

        BaletteCmd.New("Reset all settings")
            .Group("Danger zone")
            .Keywords("defaults", "restore")
            .Tags(CommandTag)
            .Destructive("Reset every preference to its default?")
            .Handler(async _ =>
            {
                await state.ResetPreferencesAsync();
                return BaletteCommandResult.Toast("Settings reset");
            }),

        // ---- Help: what the "?" mode lists ----
        BaletteCmd.New("Keyboard shortcuts")
            .Group("Help")
            .Shortcut("Mod+/")
            .Keywords("hotkeys", "keys", "bindings")
            .Href("/keyboard#shortcuts")
            // After Href, which makes the command a navigation, so the row keeps the help icon.
            .Kind(BaletteCommandKind.Help),

        BaletteCmd.New("What's new")
            .Group("Help")
            .Badge("New")
            .Kind(BaletteCommandKind.Help)
            .Handler(_ => Task.FromResult(BaletteCommandResult.Toast("You are on the latest version", keepOpen: true))),

        // Always listed, last, whatever was typed: the query it was reached with is the feedback.
        BaletteCmd.New("Feedback: couldn't find a command?")
            .Group("Help")
            .Kind(BaletteCommandKind.Help)
            .AlwaysShow()
            .Priority(BaletteCommandPriority.Lowest)
            .Handler(ctx => Task.FromResult(BaletteCommandResult.Toast(ctx.Query.Length > 0 ? $"Thanks! We logged “{ctx.Query}”." : "Thanks for the feedback!"))),
    ];

    private static BaletteCommand ThemeOption(DemoState state, string theme, string subtitle) =>
        BaletteCmd.New(Capitalize(theme))
            .Id($"theme:{theme}")
            .Subtitle(subtitle)
            .Checked(state.Theme == theme)
            .Handler(async _ =>
            {
                await state.SetThemeAsync(theme);
                return BaletteCommandResult.Back;
            })
            .Build();

    private static BaletteCommand Toggle(string title, string id, bool value, Action toggle, string? shortcut, string keywords)
    {
        var builder = BaletteCmd.New(title)
            .Id($"toggle:{id}")
            .Group("Preferences")
            .Keywords(keywords.Split(' '))
            .Tags(CommandTag)
            .Checked(value)
            .Handler(_ =>
            {
                toggle();
                return Task.FromResult(BaletteCommandResult.KeepOpen);
            });

        if (shortcut is not null)
        {
            builder.Shortcut(shortcut);
        }

        return builder.Build();
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
