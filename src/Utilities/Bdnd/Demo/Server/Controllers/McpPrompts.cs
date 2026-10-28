using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bdnd.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a drag-and-drop library for: add it
/// to an app, build a drag-and-drop feature with it, and work out why a drag does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a target that silently never lights up, or an
/// <c>OnDrop</c> that silently runs twice, rather than a compiler error - because the failure mode of
/// an agent with a handful of tools is not ignorance, it is calling them in a sequence that skips the
/// check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bdnd-to-app", Title = "Add Bit.Bdnd to a Blazor app")]
    [Description("Walks through adding Bit.Bdnd to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBdndToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode: server, wasm, auto, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Bdnd to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, and for a MAUI/WPF/Windows Forms BlazorWebView host, then continue with what you find.
            2. Call `GetBdndSetupGuide` with that render mode and follow it. In a Blazor Web App with an interactive
               client the package and the pages that drag belong in the .Client project.
            3. DO register the service: `builder.Services.AddBitBdndServices();` in every Program.cs that renders the
               components - in a Blazor Web App BOTH the server's and the client's, because the pages are prerendered on
               the server. The draggables and drop targets work without it, but a component of the app's own that injects
               `BdndInterop` (a sortable card measuring itself) fails with "There is no registered service of type
               'Bit.Bdnd.BdndInterop'". Only a WebAssembly client that lazy loads Bit.Bdnd leaves the registration out of
               its Program.cs, and takes the interop from `Services.GetBdndInterop()` instead - see
               `GetBdndGuideSection(heading: "Lazy loading on WebAssembly")`.
            4. Do NOT link a stylesheet or add a script tag: Bit.Bdnd ships no stylesheet, and the components import their
               script module themselves. Inventing a `bit-bdnd.css` link is a common mistake.
            5. Add `@using Bit.Bdnd` to `_Imports.razor`.
            6. Wrap every region that drags and drops in a `<BdndProvider>` - a `BdndDraggable`, `BdndDropTarget` or
               `BdndDragLayer` outside one throws as it initializes - and put it on an interactively rendered page. A
               statically rendered page shows the markup and nothing drags.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the drag-and-drop pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-bdnd-feature", Title = "Implement a drag-and-drop feature")]
    [Description("Implements a drag-and-drop feature with Bit.Bdnd - kanban boards, sortable lists, palettes, dustbins, nested targets, custom drag previews, drag handles, file drop zones - using its real API rather than a guessed one or React DnD's.")]
    public static string ImplementBdndFeature(
        [Description("What the feature should do, in your own words - e.g. 'a sortable list reordered by dragging' or 'drop files from the desktop onto an upload zone'.")] string feature)
    {
        return $"""
            Implement this with Bit.Bdnd: {feature}

            Work in this order:

            1. Call `SearchBdnd` with the request above. It searches the guide, every public member, the setup guides and
               the demo's pages at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code. When a demo page does what you
               need - sortable lists, nesting, drag layers, handles, native files - fetch it with `GetBdndSourceFile` and
               follow its shape.
            3. Call `GetBdndApi` for every Bdnd type you are about to use, and match the member names, types and defaults
               exactly. There is one package and one namespace, `Bit.Bdnd`, and every public type is prefixed `Bdnd`;
               React DnD's hooks and connector refs (`useDrag`, `useDrop`, `drag(ref)`) do not exist - the component's
               host element is the source or the target.
            4. Put every `BdndDraggable` and `BdndDropTarget` of the feature inside one `BdndProvider`. Give each draggable
               a `Type` string from a constants class and list it in the target's `Accept` or `AcceptTypes` - types are
               matched as strings, never by the item's .NET type. Render from the collected state: `IsDragging` on the
               draggable's `BdndDragState`, `IsOver`, `IsOverShallow` and `CanDrop` on the target's `BdndDropState`.
            5. Change your data in the target's `OnDrop` (with `args.Item`), or in the draggable's `OnEnd` from `DidDrop`
               and `DropResult`; set `args.Result` in `OnDrop` to tell the source what happened. Nested targets ALL get
               `OnDrop`, innermost first, and an outer non-null `Result` overrides an inner one - check
               `args.Monitor.DidDrop()` in an outer target. Gate drops with `CanDropPredicate`, reorder live from
               `OnHover`, and take files, text, links or HTML from outside the app with `BdndNativeTypes` and
               `OnNativeDrop` (never `OnDrop`).
            6. Prefer the library's own mechanism over hand-rolling one: `UseCustomDragLayer` and `BdndDragLayer` for a
               custom preview, `UseHandle` and `BdndDragHandle` for a grip, `DropEffect` for the cursor,
               `BdndInterop.GetBoundingClientRectAsync` to measure an element.
            7. If the feature nests targets or depends on which target handles a drop, describe the draggable and the
               target tree to `InspectBdndDrop`: it runs the drag through the real components and reports the hover
               state, the order the drops run in and what `OnEnd` receives.
            8. Write the code, then say which tool call each non-obvious choice came from.
            """;
    }

    [McpServerPrompt(Name = "debug-bdnd-drag", Title = "Debug a drag that misbehaves")]
    [Description("Diagnoses a Bit.Bdnd problem - nothing drags, a target never lights up, OnDrop never runs or runs twice, OnEnd reports DidDrop = false, files open in the browser, a custom preview flickers, an exception about BdndProvider or Interop.")]
    public static string DebugBdndDrag(
        [Description("What goes wrong, with the page, the draggable and the targets involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bdnd problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBdndSetupGuide` for this render mode first - the four failures it prevents:
               `AddBitBdndServices()` missing from a Program.cs while a component of the app's own injects `BdndInterop`
               (in a Blazor Web App it must be in both - "There is no registered service of type 'Bit.Bdnd.BdndInterop'"),
               a component outside a `BdndProvider` (it throws), a statically rendered page (nothing drags), and source
               and target in different providers (the target never reacts).
            2. Compare the draggable's `Type` with the target's `Accept`/`AcceptTypes` character by character - they are
               compared as strings, case included. A `CanDropPredicate` answering false keeps `IsOver` true but
               `CanDrop` false, and `OnDrop` does not run.
            3. Describe the draggable and the target tree to `InspectBdndDrop` - with `canDrop: false` where a predicate
               says no and the `result` each `OnDrop` sets - and hover the target in question. It runs the drag through
               the real components and reports each target's `CanDrop`, `IsOver` and `IsOverShallow`, the order `OnDrop`
               ran in, and the `DidDrop` and `DropResult` the draggable's `OnEnd` received, with notes on near-miss types.
            4. Call `SearchBdnd` with the symptom and read the guide section it points to - "Troubleshooting", "Drop
               targets", "Nesting", "Sortable lists" and "Native drags" cover the usual causes: nested targets all get
               `OnDrop` (check `args.Monitor.DidDrop()`), the gaps of a list are no-drop zones unless the list is a target
               too, native drops arrive through `OnNativeDrop`, a preview needs `pointer-events: none`.
            5. Confirm the API you are relying on with `GetBdndApi` before concluding it is a bug - check the actual
               default of the parameter involved (`CanDrag` is true, `DropEffect` is "move", `UseHandle` and
               `UseCustomDragLayer` are false).

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
