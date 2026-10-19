using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bonboarding.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask an onboarding library for: add it
/// to an app, build a tour with it, and work out why a tour does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a tour that silently never appears rather than a
/// compiler error - because the failure mode of an agent with a handful of tools is not ignorance,
/// it is calling them in a sequence that skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bonboarding-to-app", Title = "Add Bit.Bonboarding to a Blazor app")]
    [Description("Walks through adding Bit.Bonboarding to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBonboardingToApp(
        // The five values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm")]
        [Description("The app's Blazor render mode: server, wasm, auto or standalone-wasm. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Bonboarding to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs and for a separate
               .Client project, then continue with what you find.
            2. Call `GetBonboardingSetupGuide` with that render mode and follow it. Registering the services in only one
               of a Blazor Web App's two DI containers is the most common setup bug - it fails during prerendering, not
               at compile time. Give both registrations the same options.
            3. Reference `_content/Bit.Bonboarding/bit-bonboarding.css` from the host page, before the app's own stylesheet.
            4. Render exactly one `<BonboardingRoot />`, in the main layout. Without it a started tour draws nothing.
            5. Add `@using Bit.Bonboarding` to the `_Imports.razor` of every project that declares tours.
            6. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the
            services in.
            """;
    }

    [McpServerPrompt(Name = "implement-bonboarding-tour", Title = "Implement a product tour")]
    [Description("Implements an onboarding feature with Bit.Bonboarding - a tour, a hint, an interactive or branching or multi-page flow - using its real API rather than a guessed one.")]
    public static string ImplementBonboardingTour(
        [Description("What the onboarding should do, in your own words - e.g. 'a welcome tour that runs once per user' or 'a step that advances when the user clicks the highlighted button'.")] string feature)
    {
        return $"""
            Implement this with Bit.Bonboarding: {feature}

            Work in this order:

            1. Call `SearchBonboarding` with the request above. It searches the guide, every public member, the theme
               variables and the demo's sources at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBonboardingApi` for every Bonboarding type you are about to use, and match the parameter names,
               types and defaults exactly. Every public type is prefixed `Bonboarding`; do not borrow names from another
               tour library.
            4. Point steps at elements through `data-bonboarding-anchor` attributes and `Anchor="..."`, not captured
               element references: anchors survive re-renders and work for elements that render late.
            5. Prefer the library's own mechanism over hand-rolling one: `AutoStart="BonboardingAutoStart.Once"` or
               `StartOnceAsync` over tracking "seen" yourself, `When`/`ResolveNext` over separate tours per user type,
               `AdvanceOn` over listening for clicks yourself, `Route` over navigating manually between steps,
               `MissingTarget` over checking for the element first, `IBonboardingObserver` over wrapping every call.
            6. If a step sits near an edge of the screen, check its placement with `InspectBonboardingPlacement`.
            7. Start the tour from `OnAfterRenderAsync(firstRender: true)`, a user action or `AutoStart` - never from
               `OnInitializedAsync`, which also runs during prerendering.
            8. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBonboardingSourceFile` and follow
            its shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-bonboarding-tour", Title = "Debug a tour")]
    [Description("Diagnoses a Bit.Bonboarding problem - a tour that never appears, a step stuck waiting, a popover in the wrong place, progress that does not resume.")]
    public static string DebugBonboardingTour(
        [Description("What goes wrong, with the step or the target involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bonboarding problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBonboardingSetupGuide` for this render mode first: the services registered in
               every DI container, the stylesheet referenced, exactly one `<BonboardingRoot />` rendered, the tour started
               after the first render rather than in `OnInitializedAsync`. Most "nothing happens" reports end here.
            2. Call `SearchBonboarding` with the symptom, and read the guide section it points to - "Dynamic and async
               targets" covers steps that wait, "Persistence" covers tours that do not start or do not resume (completed
               and dismissed tours are recorded, and `Version` must be bumped when steps change), "Multi-page tours"
               covers routes.
            3. For a popover in the wrong place, measure the target and call `InspectBonboardingPlacement` with those
               numbers: it runs the library's own placement engine and says whether it slid, flipped or clipped.
            4. Confirm the API you are relying on with `GetBonboardingApi` before concluding it is a bug - check the
               actual default value of the option involved, and that the step's `Anchor` matches a
               `data-bonboarding-anchor` value exactly.
            5. Subscribe to `IBonboardingService.EventRaised` (or register an `IBonboardingObserver`) to see the event
               stream: `TargetMissing`, `StepSkipped` and `TourFailed` usually name the cause outright.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
