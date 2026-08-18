using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Brouter.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the four things people actually ask a router for: add it to an app,
/// build a feature with it, move off the built-in Router, and work out why a URL is not matching.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a blank page rather than a compiler error - because
/// the failure mode of an agent with a dozen tools is not ignorance, it is calling them in a
/// sequence that skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-brouter-to-app", Title = "Add Bit.Brouter to a Blazor app")]
    [Description("Walks through adding Bit.Brouter to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBrouterToApp(
        // The five values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm")]
        [Description("The app's Blazor render mode: server, wasm, auto or standalone-wasm. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Brouter to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs and for a separate
               .Client project, then continue with what you find.
            2. Call `GetBrouterSetupGuide` with that render mode and follow it. Registering the services in only one
               of a Blazor Web App's two DI containers is the single most common setup bug - it fails during
               prerendering, not at compile time.
            3. Replace Blazor's built-in `<Router>`: add the catch-all host page and render `<Brouter>` from it. Do
               not leave both routers in the tree.
            4. Keep the app's existing `@page` components working by pointing `AppAssembly` (and
               `AdditionalAssemblies` for any razor class libraries) at their assemblies - templates are
               superset-compatible, so nothing about them has to change.
            5. Before finishing, call `AnalyzeBrouterRouteTable` with every route template the app now declares, and
               resolve anything it reports as ambiguous or invalid.
            6. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the
            services in.
            """;
    }

    [McpServerPrompt(Name = "implement-brouter-feature", Title = "Implement a routing feature")]
    [Description("Implements a routing feature with Bit.Brouter - guards, loaders, nested layouts, keep-alive, transitions - using its real API rather than a guessed one.")]
    public static string ImplementBrouterFeature(
        [Description("What the routing should do, in your own words - e.g. 'warn before leaving a half-filled form' or 'load the order before the page renders and cache it for 30 seconds'.")] string feature)
    {
        return $"""
            Implement this with Bit.Brouter: {feature}

            Work in this order:

            1. Call `SearchBrouter` with the request above. It searches the guide, the docs pages, every public
               member and the demo's sources at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBrouterApiDetails` for every Brouter type you are about to use, and match the parameter
               names, types and defaults exactly - do not infer a parameter from another router library.
            4. If the work touches route templates, check them with `InspectBrouterRouteTemplate` (or
               `AnalyzeBrouterRouteTable` for several) before you ship them.
            5. Prefer the framework's own mechanism over hand-rolling one: a route `Guard` / `LeaveGuard` over manual
               checks in OnInitialized, a route `Loader` over fetching in the component, `KeepAlive` over caching
               state yourself, `<BrouterOutlet />` over conditional rendering.
            6. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBrouterSourceFile` and follow
            its shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "migrate-to-brouter", Title = "Migrate off the built-in Router")]
    [Description("Migrates an app from Blazor's built-in Router to Bit.Brouter, keeping its existing @page components and authorization.")]
    public static string MigrateToBrouter()
    {
        return """
            Migrate this app from Blazor's built-in `<Router>` to Bit.Brouter.

            Work in this order:

            1. Call `GetBrouterGuideSection` with heading "Migrating from the built-in Router" and read it in full,
               then `GetBrouterDocsPage` with slug "migration" for the parameter-by-parameter mapping table.
            2. Call `GetBrouterSetupGuide` for this app's render mode and reconcile it with what the app already has.
            3. Keep every existing `@page` component where it is: enable discovery with `AppAssembly` /
               `AdditionalAssemblies` instead of rewriting templates. Route templates are superset-compatible.
            4. Map the built-in Router's pieces rather than dropping them - `Found`/`RouteView`, `NotFound`,
               `Navigating`, `AuthorizeRouteView` and layouts all have documented equivalents; the guide's migration
               section states each one.
            5. Call `AnalyzeBrouterRouteTable` with the full set of templates once discovery is on, since a
               hand-declared route and a discovered `@page` can now collide.
            6. Build, then walk the app's routes - including deep links and the 404 path - and report what changed.

            Do not change route templates or component code beyond what the migration requires.
            """;
    }

    [McpServerPrompt(Name = "debug-brouter-routing", Title = "Debug a routing problem")]
    [Description("Diagnoses a Bit.Brouter routing problem - a URL that does not match, a parameter that arrives null, a guard that does not fire, stale loader data.")]
    public static string DebugBrouterRouting(
        [Description("What goes wrong, with the URL and the route template involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Brouter problem: {symptom}

            Work in this order:

            1. Call `GetBrouterDocsPage` with slug "faq" - it lists the common symptoms with the reason behind each,
               and the cause is often there verbatim.
            2. If a route template is involved, call `InspectBrouterRouteTemplate` on it. It parses with Brouter's
               own parser and reports the parameters, constraints and specificity - and the exact error if the
               template is invalid. For a URL that picks the wrong route, call `AnalyzeBrouterRouteTable` with every
               competing template: it ranks them the way the router does.
            3. Call `SearchBrouter` with the symptom for anything the FAQ did not cover.
            4. Confirm the API you are relying on with `GetBrouterApiDetails` before concluding it is a bug - check
               the actual default value of the parameter involved.
            5. Check the setup itself against `GetBrouterSetupGuide` for this render mode: services registered in
               every DI container, the catch-all host page present, the built-in `<Router>` gone, `AppAssembly` set
               if the route is a discovered `@page`.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
