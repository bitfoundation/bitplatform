using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Balette.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a command-palette library for: add
/// it to an app, build a palette feature with it, and work out why a palette does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a palette that silently never opens, opens unstyled, or
/// shows a stale command, rather than a compiler error - because the failure mode of an agent with a
/// handful of tools is not ignorance, it is calling them in a sequence that skips the check which
/// would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-balette-to-app", Title = "Add Bit.Balette to a Blazor app")]
    [Description("Walks through adding Bit.Balette to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBaletteToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode: server, wasm, auto, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Balette to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, for a MAUI/WPF/Windows Forms BlazorWebView host, and for where `@rendermode` is set -
               on `<Routes>` (global interactivity) or per page - then continue with what you find.
            2. Call `GetBaletteSetupGuide` with that render mode and follow it. Registering the services in only one of a
               Blazor Web App's two DI containers is the most common setup bug - it fails while prerendering, not at
               compile time. Call `AddBitBaletteServices` in BOTH `Program.cs` files, through one shared extension method
               so both get the same options.
            3. Link `<link rel="stylesheet" href="_content/Bit.Balette/bit-balette.css" />` in the host page (`App.razor`,
               or `wwwroot/index.html`), before the app's own stylesheet. It is NOT injected: without it the palette opens
               unstyled.
            4. Render exactly one `<Balette />`, in the main layout - and only if that layout is rendered interactively.
               In a statically rendered layout Ctrl+K does nothing; with per-page interactivity, wrap it in a component
               that has an interactive render mode.
            5. Add `@using Bit.Balette` to the `_Imports.razor` of every project with palette markup or commands. If you
               hold the component in a field inside a namespace under `Bit` (other than `Bit.Balette`), type it
               `Bit.Balette.Balette` - there, `Balette` alone is the namespace.
            6. Register the app-wide commands once, from the layout, with `<BaletteCommands Commands="..." />`.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the
            services in and where the stylesheet is linked.
            """;
    }

    [McpServerPrompt(Name = "implement-balette-feature", Title = "Implement a command-palette feature")]
    [Description("Implements a command-palette feature with Bit.Balette - commands, nested pages, prompts, multi-select, modes, async sources, shortcuts, theming - using its real API rather than a guessed one.")]
    public static string ImplementBaletteFeature(
        [Description("What the palette should do, in your own words - e.g. 'a rename command that asks for the new name' or 'search our API for documents as the user types'.")] string feature)
    {
        return $"""
            Implement this with Bit.Balette: {feature}

            Work in this order:

            1. Call `SearchBalette` with the request above. It searches the guide, every public member, the setup guides,
               the stylesheet and the demo's sources at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBaletteApi` for every Balette type you are about to use, and match the member names, types and
               defaults exactly. There is one package and one namespace, `Bit.Balette`, and every public type is prefixed
               `Balette` - the component is `Balette`, not `CommandPalette`; do not borrow names from cmdk, kbar or another
               palette library.
            4. Commands are immutable. Give a command an explicit `Id` whenever its title can change (a toggle's label, a
               translated title): a derived id changes with the title, and recents, pins and `OpenAsync(pageCommandId: ...)`
               lose it. To show a new state, hand `BaletteCommands` a new list, call `Update`, or register again - never
               expect a changed field to show up.
            5. Commands a page registers leave with the page: use `<BaletteCommands>`, or keep the `IDisposable` that
               `Register` returns and dispose it in the page's `Dispose`.
            6. Prefer the library's own mechanism over hand-rolling one: `BaletteCommandResult.Prompt` over a dialog of your
               own, `MultiSelect()` with `Ctrl+Enter` over tracking checkboxes, `Destructive("message")` (Enter twice) over
               a confirmation step, `Children` or `BaletteCommandResult.Page` over a second palette, a `BaletteCommandMode`
               over filtering yourself, an `IBaletteSource` - `ApplyMatcher = true` to have the palette re-rank what it
               returns, false to keep the server's order - over calling your API from the query callback, `Shortcut("G S")`
               over listening for keys, and the `--bit-balette-*` variables (`GetBaletteThemeVariables`) over overriding the
               library's selectors.
            7. If the feature depends on what a query finds - keywords, priority, operators - check it with
               `InspectBaletteSearch`, which runs the library's own matcher and ranker over your commands.
            8. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBaletteSourceFile` (the index lists
            them; `Demo/Client/Services/DemoCommands.cs` holds a whole app's registry) and follow its shape rather than
            inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-balette-palette", Title = "Debug a command palette")]
    [Description("Diagnoses a Bit.Balette problem - Ctrl+K does nothing, the palette opens unstyled, a command is missing, ranks oddly or shows a stale state, a shortcut does not fire, a source returns nothing.")]
    public static string DebugBalettePalette(
        [Description("What goes wrong, with the command, the query or the page involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Balette problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBaletteSetupGuide` for this render mode first: `AddBitBaletteServices` in every
               DI container that renders the palette (both projects of a Blazor Web App), the stylesheet linked by the host
               page (it is not injected - an unstyled palette means the link is missing), exactly one `<Balette />`, in a
               layout that is rendered interactively (a statically rendered one is why Ctrl+K does nothing). Most "nothing
               happens" reports end here.
            2. Call `SearchBalette` with the symptom, and read the guide section it points to - "Troubleshooting" lists the
               common ones, "Hosting models" covers render modes, "Keyboard" covers shortcuts that do not fire in text boxes,
               "Async sources" covers debouncing, cancellation and sources that throw.
            3. For a command that does not show up or ranks oddly, run the query through `InspectBaletteSearch` with the
               commands involved: it runs the library's own matcher and ranker and says which term failed - a `!negated`
               term, a capital letter that made a term case-sensitive, an operator - and what each field scored. A source
               with `ApplyMatcher = true` is re-scored the same way; with false its own order is kept.
            4. For a stale toggle or value, remember commands are immutable: the fix is a new list for `BaletteCommands`,
               `Update`, or registering again. For recents or pins that forget a command, check whether its `Id` is derived
               from a title that changed. For commands still listed after leaving a page, check that the page disposes what
               it registered.
            5. For a destructive command that runs at once, check it was given a message - `Destructive("...")` or `Confirm`
               - since only a message makes Enter arm it first.
            6. Confirm the API you are relying on with `GetBaletteApi` before concluding it is a bug - check the actual
               default of the option involved. A field typed `Balette` that does not compile inside a namespace under `Bit`
               needs `Bit.Balette.Balette`.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
