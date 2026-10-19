using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Blex.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the four things people actually ask a state library for: add it to an app,
/// build a feature with it, move off another library, and work out why state is not behaving.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are expensive
/// to make - the ones that produce state that silently resets rather than a compiler error - because the
/// failure mode of an agent with a handful of tools is calling them in a sequence that skips the check
/// which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-blex-to-app", Title = "Add Bit.Blex to an app")]
    [Description("Walks through adding Bit.Blex to an existing Blazor or .NET MAUI app, in the right order for its hosting model.")]
    public static string AddBlexToApp(
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "maui-hybrid", "maui")]
        [Description("The app's hosting model: server, wasm, auto, standalone-wasm, maui-hybrid or maui. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add Bit.Blex to this app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for MauiProgram.cs and
               BlazorWebView (MAUI), for AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in
               Program.cs and for a separate .Client project (Blazor Web App), then continue with what you find.
            2. Call `GetBlexSetupGuide` with that hosting model and follow it. Add BOTH `Bit.Blex` and
               `Bit.Blex.Generators` - without the generator a store compiles to nothing.
            3. Register Blex in every DI container that renders components. A Blazor Web App with an interactive
               client has two; registering in only one fails during prerendering, not at compile time.
            4. Wrap the router in `<BlexProvider>` once, inside the interactive render boundary (or call `UseBitBlex()`
               in a native MAUI app).
            5. Write the first store, then check it with `InspectBlexStore` and use the member names it reports.
            6. Build the app and fix what the compiler says. `GetBlexDiagnostics` explains any BLEX error.

            Show me the diff for each file you change, and say explicitly which DI containers you registered Blex in.
            """;
    }

    [McpServerPrompt(Name = "implement-blex-feature", Title = "Implement a state feature")]
    [Description("Implements a state-management feature with Bit.Blex - stores, computed values, effects, persistence, undo/redo - using its real API rather than a guessed one.")]
    public static string ImplementBlexFeature(
        [Description("What the state should do, in your own words - e.g. 'a shopping cart with a computed total that survives a reload'.")] string feature)
    {
        return $"""
            Implement this with Bit.Blex: {feature}

            Work in this order:

            1. Call `SearchBlex` with the request above. Every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBlexApi` for every Blex type you are about to use, and match the names, types and defaults
               exactly - do not carry a pattern over from Fluxor, Redux or MobX.
            4. Write the store as a `partial class` marked `[BlexStore]`, then call `InspectBlexStore` on it and fix every
               BLEX diagnostic it reports. Use the generated member names it lists - `_items` becomes `Items`,
               `OnAddItem` becomes `AddItem`, an effect gets a `CancelXxx` method - rather than guessed ones.
            5. Prefer the library's own mechanism to a hand-rolled one: `[BlexComputed]` over recomputing in the component,
               `[BlexEffect]` (with a `CancellationToken` and a concurrency mode) over manual loading flags,
               `[BlexStore(Persist = true)]` over saving by hand, a selector subscription over re-rendering on every change,
               `BlexEntityAdapter` over a hand-maintained dictionary.
            6. Write the components, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBlexSourceFile` and follow its
            shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "migrate-to-blex", Title = "Migrate from Fluxor or hand-rolled state")]
    [Description("Migrates an app's state from Fluxor (or from hand-rolled services with change events) to Bit.Blex, one feature at a time.")]
    public static string MigrateToBlex()
    {
        return """
            Migrate this app's state management to Bit.Blex.

            Work in this order:

            1. Call `GetBlexGuideSection` with heading "Why Blex" for the concept mapping: a Fluxor Feature + State
               class becomes `[BlexState]` fields on one store, an Action class + Reducer becomes one `[BlexAction]`
               method, an Effect class becomes one `[BlexEffect]` method, a selector becomes `[BlexComputed]` or a
               selector subscription.
            2. Call `GetBlexSetupGuide` for this app's hosting model and add Blex next to the existing library - both can
               run side by side while the migration is in progress.
            3. Migrate one feature at a time. For each: write the store, check it with `InspectBlexStore`, switch its
               components to `BlexComponentBase` + `Subscribe`, then delete the old Feature/State/Action/Reducer/Effect
               classes for it.
            4. Keep the behaviour, not the ceremony: middleware that logged or vetoed actions maps to `IBlexMiddleware`
               (`GetBlexDocsPage(slug: "middleware")`), persisted state to `[BlexStore(Persist = true)]`
               (`GetBlexDocsPage(slug: "persistence")`).
            5. Build after every feature and run the tests; `Bit.Blex.Testing`'s harness replaces dispatcher mocks
               (`GetBlexDocsPage(slug: "testing")`).

            Do not change component markup or behaviour beyond what the migration requires.
            """;
    }

    [McpServerPrompt(Name = "debug-blex-state", Title = "Debug a state problem")]
    [Description("Diagnoses a Bit.Blex problem - a component that does not re-render, persisted state that is lost, a build full of BLEX errors, an effect that will not cancel.")]
    public static string DebugBlexState(
        [Description("What goes wrong, with the store and the component involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Blex problem: {symptom}

            Work in this order:

            1. If the build fails, call `InspectBlexStore` on the store involved and `GetBlexDiagnostics` for each BLEX id
               it reports. A rejected store gets no generated code at all, so fix the BLEX error before reading any of the
               "does not contain a definition" errors that come with it.
            2. Call `SearchBlex` with the symptom, then read the best hit in full.
            3. Check the wiring against `GetBlexSetupGuide` for this hosting model: Blex registered in every DI container,
               `<BlexProvider>` present once and inside the interactive boundary, the store registered with
               `AddBitBlexStore<T>()`, persistence registered when a store is marked `Persist = true`.
            4. Check the component: it must inherit `BlexComponentBase` and call `Subscribe(...)` in `OnInitialized`, or it
               never learns the state changed. State replaced in place (a list mutated rather than reassigned) does not
               notify - read `GetBlexDocsPage(slug: "stores")` on change detection.
            5. Confirm the API you rely on with `GetBlexApi` before concluding it is a bug - check the default value of the
               option involved.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
