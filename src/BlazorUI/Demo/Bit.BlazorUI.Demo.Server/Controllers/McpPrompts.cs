using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the five things people actually ask a component library for: add it to
/// an app, build a screen with it, replace another library or hand-written markup, make it match a
/// brand, and work out why something looks wrong.
/// <para>
/// Each prompt spends its words on the order to call the tools in, because the failure mode of an
/// agent here is not ignorance, it is writing markup from a remembered API before it has read the
/// current one - which compiles, renders, and is subtly wrong.
/// </para>
/// <para>
/// What none of them spends words on is the library's six standing rules - the registrations, the
/// styling API, the enums, IsEnabled, the binding form. Those are in the server's
/// <c>instructions</c>, which the client has had in context since <c>initialize</c>; a prompt that
/// repeated them would be paying a second time for a model that had already read them once.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bit-blazorui-to-app", Title = "Add bit BlazorUI to a Blazor app")]
    [Description("Walks through adding bit BlazorUI to an existing Blazor app, in the right order for its hosting model.")]
    public static string AddToApp(
        [Description("The app's hosting model: web-app, wasm, server or hybrid. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add bit BlazorUI to this Blazor app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, for WebAssemblyHostBuilder, or for AddMauiBlazorWebView - then continue with what
               you find.
            2. Call `GetBitBlazorUISetupGuide` with that hosting model and follow it exactly.
            3. Get the two things that fail without a build error right, and say out loud that you did: the
               service registration in every DI container that renders components, and the stylesheet and script
               in the host page. Rule 1 of this server's instructions is the short version of why.
            4. Add only the optional packages the app will actually use. Every component page says which package
               it comes from; adding Extras for a component that lives in the core package is a stylesheet, a
               script and a megabyte for nothing.
            5. Verify with the check at the end of the setup guide, then build and fix what the compiler says.

            Show me the diff for each file you change, and state which DI containers you registered the services
            in and which file you put the tags in.
            """;
    }

    [McpServerPrompt(Name = "build-bit-blazorui-screen", Title = "Build a screen with bit BlazorUI")]
    [Description("Builds a page or a feature out of bit BlazorUI components, using their real API rather than a remembered one.")]
    public static string BuildScreen(
        [Description("What the screen should do, in your own words - e.g. 'a product list with search, filters and a details panel' or 'a signup form with validation'.")] string screen)
    {
        return $"""
            Build this with bit BlazorUI: {screen}

            Work in this order:

            1. Call `SearchBitBlazorUI` with the description above, and again for each distinct piece of the
               screen. Do this even where a component name comes to mind: this library's name for a thing is
               often not the one the task suggests, and the search returns the aliases as well as the names.
            2. Call `GetBitBlazorUIComponent` for every component you settled on. Match parameter names,
               types and defaults exactly - and check the package line: a component from Extras needs its
               package, its registration and its two tags before it renders at all.
            3. Call `GetBitBlazorUIComponentExamples` for anything with a shape you would otherwise guess at -
               templated items, data binding, validation inside an EditForm, a service-driven modal or panel.
               Follow the example's shape rather than inventing one.
            4. Lay the screen out with the layout components (BitStack, BitGrid, BitCard, BitSeparator,
               BitSpacer) before reaching for a div and a stylesheet, and say which pieces genuinely needed
               custom CSS and why.
            5. Look up any icon name with `FindBitBlazorUIIcons` before you use it.
            6. Build the app and fix what the compiler says. Then say which tool call each non-obvious choice
               came from.
            """;
    }

    [McpServerPrompt(Name = "migrate-to-bit-blazorui", Title = "Migrate a UI to bit BlazorUI")]
    [Description("Replaces another component library, or hand-written HTML and CSS, with the equivalent bit BlazorUI components.")]
    public static string Migrate(
        [Description("What the UI is built with today - e.g. 'MudBlazor', 'Radzen', 'Bootstrap markup', 'hand-written HTML and CSS'.")] string current = "hand-written HTML and CSS")
    {
        return $"""
            Migrate this app's UI from {current} to bit BlazorUI.

            Work in this order:

            1. Inventory what is there: every component of the old library, every hand-written control, and every
               stylesheet rule that exists to make one of them look right.
            2. For each one, call `SearchBitBlazorUI` with what it DOES rather than what it is called. The
               search matches the names other libraries use, so a MudSelect finds BitDropdown and a Toast finds
               BitSnackBar.
            3. Call `GetBitBlazorUIComponent` for each replacement before rewriting the call site, and
               `GetBitBlazorUIComponentExamples` for anything with data binding or templates. Do not translate
               parameter names across libraries by their shape - the same idea often has a different name and a
               different default here.
            4. Replace the styling as you go rather than carrying it over. Most of the old stylesheet exists to
               produce a look this library already has as Variant, Color and Size; what genuinely differs
               belongs in the component's Classes / Styles bag, and what differs app-wide belongs in a
               --bit-* token - `GetBitBlazorUIThemingGuide(section: "Design tokens")` covers that.
            5. Do the migration one screen at a time, building between screens, and report what changed and
               what you deliberately left on the old library and why.
            6. Delete the old package reference and its stylesheet only once nothing references them.
            """;
    }

    [McpServerPrompt(Name = "theme-bit-blazorui-app", Title = "Make bit BlazorUI match a brand")]
    [Description("Themes an app built with bit BlazorUI - brand colors, dark mode, a design system, density - through the token system rather than by overriding component CSS.")]
    public static string Theme(
        [Description("What the app should look like - a brand color, a design system ('Material', 'Cupertino', 'Fluent 2'), 'dark mode by default', or a description in your own words.")] string look)
    {
        return $"""
            Make this bit BlazorUI app look like: {look}

            Work in this order:

            1. Call `GetBitBlazorUIThemingGuide` with no argument for the index, then read the chapters that
               match the request. "At a glance" says which of the three layers a given change belongs in, and
               getting that right is most of the work.
            2. If a packaged design system covers it, use it: Fluent 2, Material and Cupertino ship with
               Bit.BlazorUI.Extras as one extra stylesheet link each. Read "Presets" before writing any CSS.
            3. If it is a brand color, derive the palette rather than picking the shades by hand -
               "Color derivation and contrast" covers deriving a whole theme from one seed and checking the
               result for contrast.
            4. Whatever is left is token overrides. Change --bit-* custom properties; do not write rules that
               select .bit-<component> classes. A rule that targets a component's internals is a rule that
               breaks on the next release, and it is the signal that a token is missing rather than that CSS
               was needed.
            5. If the app renders on the server, handle the first frame - "Server-side rendering" - or the app
               flashes the wrong theme before any JavaScript has run.
            6. Show me the diff, and say which layer each change landed in and why.
            """;
    }

    [McpServerPrompt(Name = "debug-bit-blazorui-issue", Title = "Debug a bit BlazorUI problem")]
    [Description("Diagnoses a bit BlazorUI problem - components rendering unstyled, a callout that never opens, a parameter that seems to do nothing, an icon showing as an empty box.")]
    public static string Debug(
        [Description("What goes wrong, with the component involved if you know it.")] string symptom)
    {
        return $"""
            Diagnose this bit BlazorUI problem: {symptom}

            Work in this order:

            1. Rule out the wiring first, with `GetBitBlazorUISetupGuide` for this app's hosting model. Three
               symptoms are almost always one of these and nothing else: everything renders as plain HTML (the
               stylesheet is missing), the interactive components - callouts, dropdowns, tooltips, the modal -
               never open (the script is missing), and injecting a service throws (the registration is missing
               from one of the app's DI containers, usually the server half of a Blazor Web App).
            2. If a component from Extras or Legacy is involved, confirm its own package, registration and two
               tags are present as well: the core ones are not enough for it.
            3. Confirm the parameter with `GetBitBlazorUIComponent` before concluding it is a bug - check the
               exact name, the type and the DEFAULT, which is where "it does nothing" usually comes from, and
               read the notes, which carry the per-component caveats.
            4. If it is an icon showing as an empty box, check the name with `FindBitBlazorUIIcons` and check
               that Bit.BlazorUI.Icons is referenced and its stylesheet is on the page.
            5. If it is a color, a size or a spacing that is wrong rather than absent, it is a theming question:
               `GetBitBlazorUIThemingGuide`. A component that looks right in light and wrong in dark is a
               hard-coded value somewhere in the app.
            6. Call `SearchBitBlazorUI` with the symptom for anything the above did not cover.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
