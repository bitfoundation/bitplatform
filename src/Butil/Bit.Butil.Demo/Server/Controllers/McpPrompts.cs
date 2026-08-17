using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Bit.Butil.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the four things people actually ask a browser-API wrapper for: add it
/// to an app, build a feature with it, replace hand-written JS interop, and work out why a call
/// that compiles does nothing.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce silence rather than a compiler error - because the
/// failure mode of an agent with a dozen tools is not ignorance, it is calling them in a sequence
/// that skips the check which would have caught the bug. With browser APIs that check is almost
/// always the same one: what does this need from the page before it will work at all.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-butil-to-app")]
    [Description("Walks through adding Bit.Butil to an existing Blazor app, in the right order for its hosting model.")]
    public static string AddButilToApp(
        [Description("The app's hosting model: wasm, web-app, server or hybrid. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add Bit.Butil to this Blazor app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, for WebAssemblyHostBuilder, or for AddMauiBlazorWebView - then continue with what
               you find.
            2. Call `GetButilSetupGuide` with that hosting model and follow it.
            3. Get the two things that fail silently right, and say out loud that you did:
               - the `bit-butil.js` script tag goes in the host page BEFORE the Blazor script;
               - `AddBitButilServices()` is called in EVERY DI container that renders components - a Blazor Web App
                 with an interactive client has two.
            4. Verify it end to end with a round trip that cannot pass by accident: write a value with
               `LocalStorage.SetItem`, read it back with `GetItem` from `OnAfterRenderAsync`, and show the result.
            5. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and state which DI containers you registered the services in
            and which file you put the script tag in.
            """;
    }

    [McpServerPrompt(Name = "implement-butil-feature")]
    [Description("Implements a browser-platform feature with Bit.Butil - clipboard, storage, media, sensors, observers - using its real API rather than a guessed one.")]
    public static string ImplementButilFeature(
        [Description("What the feature should do, in your own words - e.g. 'let the user pick a photo and save a cropped copy back to disk' or 'keep the screen awake while a recipe is on screen'.")] string feature)
    {
        return $"""
            Implement this with Bit.Butil: {feature}

            Work in this order:

            1. Call `SearchButil` with the request above. It searches the guide, the docs pages, every public member
               and the demo's sources at once, and every hit tells you the exact follow-up call to make. Do this even
               when you think you know the API: the browser's name for a capability is rarely the name a task
               suggests.
            2. Call `PlanButilFeature` with the services the search turned up. It tells you what the whole set
               demands together - HTTPS, permission prompts, a user gesture, which engines will run it, what has to
               be disposed - before you have written code that assumes otherwise.
            3. Call `GetButilApiDetails` for every service you are about to use and match the member names,
               signatures and default arguments exactly. Do not infer a member from the JavaScript API's shape.
            4. Write the code against these rules, which are what separates working Butil code from code that
               compiles:
               - touch the browser from `OnAfterRenderAsync` or an event handler, never `OnInitializedAsync`;
               - start anything gated on a user gesture directly from the click handler;
               - treat a `false`/`null` from a permission-gated call as a normal branch and show the user something;
               - dispose every `ButilSubscription` and every handle, in `DisposeAsync`.
            5. Say which tool call each non-obvious choice came from.

            The documentation site has one page per API, and each page IS a working example - fetch it with
            `GetButilSourceFile(path: "Demo/Client/Pages/<Api>Page.razor")` and follow its shape rather than
            inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "replace-jsinterop-with-butil")]
    [Description("Replaces hand-written IJSRuntime interop and .js files in an app with the equivalent Bit.Butil services.")]
    public static string ReplaceJsInteropWithButil()
    {
        return """
            Replace this app's hand-written JavaScript interop with Bit.Butil.

            Work in this order:

            1. Inventory what is there: every `IJSRuntime.InvokeAsync`/`InvokeVoidAsync` call site, every `.js` file
               under wwwroot that exists only to be called from C#, and every `IJSObjectReference` module.
            2. For each one, call `SearchButil` with what the JavaScript does (not with its function name) to find
               the wrapper that covers it. Butil wraps around sixty browser APIs; most bespoke interop in a Blazor
               app is one of them.
            3. Call `GetButilApiDetails` for each replacement before rewriting the call site, and
               `InspectButilApi` for anything gated on a permission, a gesture or a secure context - the hand-written
               version may have been getting away with something the wrapper is explicit about.
            4. Rewrite the call sites, then delete the JavaScript that no longer has a caller. Keep the interop that
               Butil genuinely does not cover, and say which pieces those are and why.
            5. Replace manual listener bookkeeping (`DotNetObjectReference`, `addEventListener` plus a remove
               function) with the subscription model: every Butil subscription returns a `ButilSubscription` that
               detaches on disposal.
            6. Build, run the affected pages, and report what changed.

            Do not change behaviour beyond the replacement, and do not remove a `.js` file that still has a caller.
            """;
    }

    [McpServerPrompt(Name = "debug-butil-issue")]
    [Description("Diagnoses a Bit.Butil problem - a call that returns nothing, a permission that never prompts, an event that never fires, a feature that works in one browser only.")]
    public static string DebugButilIssue(
        [Description("What goes wrong, with the service and member involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Butil problem: {symptom}

            Work in this order:

            1. Call `GetButilDocsPage` with slug "troubleshooting" - it lists the symptoms people hit first with the
               reason behind each, and the cause is often there verbatim.
            2. Call `InspectButilApi` for the service involved. A call that compiles and does nothing is almost
               always one of four things, and this reports all four: it ran during prerendering (no JS runtime, so
               reads return safe defaults and voids are no-ops), the page is not a secure context, the call did not
               come from a user gesture, or the engine does not implement the API.
            3. Confirm the setup itself against `GetButilSetupGuide` for this hosting model: the `bit-butil.js`
               script tag present and BEFORE the Blazor script, and `AddBitButilServices()` called in every DI
               container - a missing registration surfaces during prerendering, a missing script surfaces as every
               call doing nothing.
            4. Confirm the member with `GetButilApiDetails` before concluding it is a bug - check the actual
               signature and the default arguments, and read the XML remarks, which carry the per-member caveats.
            5. Call `SearchButil` with the symptom for anything the above did not cover.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
