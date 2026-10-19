using Bit.Balette;
using Bit.Balette.Demo.Client.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: every page is prerendered on the server before it runs in the browser,
/// so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // The sources are created here, once, with the options; the log they report to has to exist before them.
        var sourceLog = new SourceLog();
        services.AddSingleton(sourceLog);

        services.AddBitBaletteServices(options =>
        {
            // Ctrl+K / ⌘K is the default; Ctrl+Shift+P is there for hands trained on VS Code.
            options.AdditionalToggleShortcuts.Add("Mod+Shift+P");

            // Groups appear in this order on the empty query; the ones not listed follow alphabetically.
            options.GroupOrder.AddRange(["Navigation", "Actions", "Preferences", "Danger zone", "Help"]);

            // Ctrl+J / Ctrl+K move through the list as well as the arrows and Ctrl+N / Ctrl+P.
            options.VimBindings = true;

            // Modes are typed prefixes, as in VS Code and GitHub. Each one can narrow the registry, add its own
            // sources, or both.
            options.Modes.Add(new BaletteCommandMode
            {
                Prefix = ">",
                Name = "Commands",
                Description = "Run any command",
                Placeholder = "Run a command…",
                Tags = [DemoCommands.CommandTag],
            });
            options.Modes.Add(new BaletteCommandMode
            {
                Prefix = "#",
                Name = "Issues",
                Description = "Search issues by number or title",
                Placeholder = "Search issues…",
                HideRegistryCommands = true,
                Sources = [new IssueSource(sourceLog)],
            });
            options.Modes.Add(new BaletteCommandMode
            {
                Prefix = "@",
                Name = "People",
                Description = "Find a teammate",
                Placeholder = "Search people…",
                HideRegistryCommands = true,
                Sources = [new PeopleSource(sourceLog)],
            });
            options.Modes.Add(new BaletteCommandMode
            {
                Prefix = "?",
                Name = "Help",
                Description = "Keyboard shortcuts and docs",
                Groups = ["Help"],
            });

            // In the default mode the documentation is searched too, next to the registry: debounced, cancelled
            // when the query moves on, and re-ranked with the commands.
            options.Sources.Add(new DocsSource(sourceLog));
        });

        // What the commands act on, and the directory the sources and the lazy "Assign to…" page read from. Neither
        // touches JavaScript until a command runs, so resolving them while prerendering is harmless.
        services.AddScoped<DemoState>();
        services.AddSingleton<FakeDirectory>();

        return services;
    }
}
