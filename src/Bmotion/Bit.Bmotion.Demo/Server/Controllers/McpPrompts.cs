using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Bit.Bmotion.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the four things people actually ask an animation library for: build an
/// animation, add the library to an app, tune how something feels, and work out why nothing moves.
/// <para>
/// Each prompt spends its words on the order to call the tools in, because the failure mode of an
/// agent holding a shelf of them is not ignorance - it is calling them in a sequence that skips the
/// check which would have caught the bug. For Bmotion that check is nearly always the same one:
/// nothing here fails loudly. An animation that will not play on Blazor Server compiles, deploys
/// and renders; it just does not move. So every workflow below ends by running the code back
/// through the engine rather than by declaring victory when it builds.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "animate-with-bmotion")]
    [Description("Builds an animation with Bit.Bmotion - an entrance, an exit, a gesture, a list, a scroll reveal - using its real API, and verifies the motion before finishing.")]
    public static string AnimateWithBmotion(
        [Description("What should move, and how it should feel, in your own words - e.g. 'the cards should appear one after another as I scroll to them' or 'the dialog should pop in and fade out quickly'.")] string request,
        [Description("The app's Blazor render mode: wasm, server, auto or standalone-wasm. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Build this with Bit.Bmotion: {request}

            The app's render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project before writing anything: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs and for a separate
               .Client project. It decides which features are available at all, not just how fast they are.
            2. Call `GetBmotionRecipes` and look for a pattern that already covers the request. If one does, fetch
               it with `GetBmotionRecipe` and follow its shape - including its Notes, which carry the caveat the
               code cannot show. If none fits, call `SearchBmotion` with the request as written.
            3. Call `GetBmotionApiDetails` for every Bmotion type you are about to use and match the parameter
               names, types and defaults exactly. Do not carry over parameter names from Framer Motion or from
               another animation library - several are close enough to look right and behave differently.
            4. Choose the transition with evidence, not adjectives: call `SimulateBmotionTransition`, passing two or
               three candidates separated by semicolons, and pick by settle time and overshoot. Nothing in a
               transition states how long it takes to settle - a spring's falls out of the physics - so this is the
               only way to know.
            5. Write the code.
            6. Call `AnalyzeBmotionAnimation` with the properties and transition you settled on. If it reports the
               C# frame loop and this app is not WebAssembly-only, either switch to the compositor-friendly
               equivalent it suggests, or tell me explicitly that this animation will snap rather than play on
               Server and let me decide.
            7. Call `ReviewBmotionCode` on what you wrote and fix every Error and Warning it reports.
            8. Build the app and fix what the compiler says.

            Show me the final markup, say which transition you chose and what its measured settle time and overshoot
            were, and state plainly whether the animation works in every render mode this app uses.
            """;
    }

    [McpServerPrompt(Name = "add-bmotion-to-app")]
    [Description("Walks through adding Bit.Bmotion to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBmotionToApp(
        [Description("The app's Blazor render mode: wasm, server, auto or standalone-wasm. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Bmotion to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs and for a separate
               .Client project, then continue with what you find.
            2. Call `GetBmotionSetupGuide` with that render mode and follow it. Registering the services in only one
               of a Blazor Web App's two DI containers is the most common setup bug - it fails during prerendering,
               not at compile time.
            3. Add `@using Bit.Bmotion` to `_Imports.razor` in every project that renders animations.
            4. Set the reduced-motion policy while you are in Program.cs:
               `AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User)`. The default is
               back-compatible rather than correct, and this is the one moment when adding it costs nothing.
            5. Add one small animation from `GetBmotionRecipe(id: "fade-in-on-mount")` to prove the wiring works
               end to end, and build.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the
            services in. If the render mode is 'server' or 'auto', also summarise for me which parts of the library
            will not be available.
            """;
    }

    [McpServerPrompt(Name = "tune-bmotion-motion")]
    [Description("Tunes how an existing Bit.Bmotion animation feels - too slow, too bouncy, too abrupt - by measuring the alternatives instead of guessing at numbers.")]
    public static string TuneBmotionMotion(
        [Description("The transition as it is written today, e.g. 'Bm.Spring(stiffness: 100, damping: 10)'.")] string current,
        [Description("What is wrong with how it feels - e.g. 'too slow to settle', 'too bouncy', 'feels lifeless', 'the tail drags'.")] string complaint)
    {
        return $"""
            Tune this Bit.Bmotion transition: {current}

            What is wrong with it: {complaint}

            Work in this order:

            1. Call `SimulateBmotionTransition` on the current transition. Read the settle time, the overshoot, the
               number of target crossings and the time to 90% of the distance. State which of those numbers
               corresponds to the complaint - "too slow" is usually a long tail after a fast start, which is the gap
               between TimeTo90Percent and SettleSeconds, not the duration anyone configured.
            2. Propose three candidates that move that specific number in the right direction, and call
               `SimulateBmotionTransition` on all three at once, separated by semicolons.
            3. Recommend one, citing its measurements against the current transition's.
            4. If the complaint is about feel rather than timing, consider the form as well as the numbers:
               `Bm.Spring(bounce:, duration:)` expresses intent directly and cannot be configured into a spring that
               never settles, while stiffness and damping can. Say so if switching form is the better fix.
            5. Call `AnalyzeBmotionAnimation` with the animated properties and the new transition, to confirm the
               change did not cost the animation its compositor path - a spring with an initial velocity, for one,
               drops off it silently.

            Give me the replacement line, the before-and-after numbers, and one sentence on what the reader will
            actually notice.
            """;
    }

    [McpServerPrompt(Name = "debug-bmotion-animation")]
    [Description("Diagnoses a Bit.Bmotion animation that does not work - nothing moves, the exit never plays, it works locally but not in production, the wrong item animates.")]
    public static string DebugBmotionAnimation(
        [Description("What goes wrong, with the markup involved if you have it.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bmotion problem: {symptom}

            Bmotion fails silently almost everywhere, so work through the silent causes in order of how often they
            are the answer:

            1. Call `ReviewBmotionCode` on the markup involved. It checks the mistakes that compile cleanly and then
               do nothing - an `Exit` with no presence component, a `@foreach` with no `@key`, a spring whose
               stiffness is discarded, a nested-quote attribute that does not parse as intended. The cause is
               often there verbatim.
            2. If nothing animates at all, establish the render mode, then call `AnalyzeBmotionAnimation` with the
               properties and transition. An animation the engine keeps on the C# frame loop becomes an instant
               state change on Blazor Server - which is exactly the "works on my machine, not in production" shape,
               and the "works on the second visit but not the first" shape under InteractiveAuto.
            3. If the element does not move on mount, check whether `Initial` is set. `Animate` alone leaves the
               element already at its target with nothing to travel from.
            4. If the motion happens but looks wrong, call `SimulateBmotionTransition` on the transition. A spring
               that appears not to stop, or one that arrives instantly and then drifts, is visible in the numbers.
            5. Confirm the API you are relying on with `GetBmotionApiDetails` before concluding it is a bug -
               check the actual default value of the parameter involved.
            6. Check the setup itself against `GetBmotionSetupGuide` for this render mode: services registered in
               every DI container the components run in, and `@using Bit.Bmotion` present.
            7. Rule out reduced motion. If the operating system asks for reduced motion and the app sets
               `BmReducedMotionMode.User` or `Always`, transforms and layout changes snap by design while opacity
               and colour keep animating. That is correct behaviour, and it looks exactly like a bug.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
