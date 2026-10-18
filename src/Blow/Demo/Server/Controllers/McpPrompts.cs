using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Blow.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a node-editor library for: add it
/// to an app, build a flow feature with it, and work out why a flow does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a canvas that silently shows nothing, or edges that
/// silently attach to the wrong place, rather than a compiler error - because the failure mode of an
/// agent with a handful of tools is not ignorance, it is calling them in a sequence that skips the
/// check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-blow-to-app", Title = "Add Bit.Blow to a Blazor app")]
    [Description("Walks through adding Bit.Blow to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBlowToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode: server, wasm, auto, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Blow to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, and for a MAUI/WPF/Windows Forms BlazorWebView host, then continue with what you find.
            2. Call `GetBlowSetupGuide` with that render mode and follow it. In a Blazor Web App with an interactive
               client the package and the pages with a canvas belong in the .Client project.
            3. Do NOT register services: Bit.Blow has none. Inventing an `AddBlow...` call is the most common mistake.
            4. DO link the stylesheet from the host page - `_content/Bit.Blow/bit-blow.css` (App.razor, or
               wwwroot/index.html). Nothing injects it, and without it the nodes pile up unstyled in a corner.
            5. Add `@using Bit.Blow` to `_Imports.razor`.
            6. Put the canvas on an interactively rendered page, inside a container that has a height (or give the
               canvas one through `Style`): `BlowCanvas` fills its container, so in one with no height it is zero
               pixels tall. A statically rendered page never makes it interactive.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the canvas pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-blow-feature", Title = "Implement a flow feature")]
    [Description("Implements a node-editor or diagram feature with Bit.Blow - custom nodes and handles, connection rules, sub-flows, drag and drop, layout, saving and restoring, undo, copy and paste, theming - using its real API rather than a guessed one.")]
    public static string ImplementBlowFeature(
        [Description("What the feature should do, in your own words - e.g. 'a workflow editor with custom nodes and typed handles' or 'save the flow to the database and restore it'.")] string feature)
    {
        return $"""
            Implement this with Bit.Blow: {feature}

            Work in this order:

            1. Call `SearchBlow` with the request above. It searches the guide, every public member, the setup guides,
               the theme and the demo's pages and examples at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code. When an example of the
               gallery does what you need, fetch it with `GetBlowSourceFile` and follow its shape.
            3. Call `GetBlowApi` for every Blow type you are about to use, and match the member names, types and
               defaults exactly. There is one package and one namespace, `Bit.Blow`, and every public type is prefixed
               `Blow`; React Flow's names (`ReactFlow`, `useReactFlow`, `onNodesChange`) are not this library's.
            4. Keep the graph in `List<BlowNode>` and `List<BlowEdge>` fields handed to `BlowCanvas`. The canvas edits them
               in place and raises `NodesChanged`/`EdgesChanged`; bind those (or `OnSelectionChange`, `OnConnect`) when
               your own markup reads the graph, call `Refresh(remeasure: true)` after changing the graph outside a render
               of your component, and `UpdateNodeInternals()` after moving a node's handles. A new edge from a connection
               is added by the canvas itself - do not add it again in `OnConnect`.
            5. A `NodeTemplate` draws every node, built-in types included: switch on `node.Type` and hand the rest back
               with `<BlowDefaultNode Node="node" />`. Give each `BlowHandle` of a node its own `Id` when it has several of a type, and set
               `SourceHandle`/`TargetHandle` on the edges to match. A child node's `Position` is relative to its parent.
            6. Prefer the library's own mechanism over hand-rolling one: `BlowLayout` over your own positioning,
               `BlowAlign` and `SnapToGuides` for alignment, `HistoryEnabled` (or `BlowHistory`) for undo, `Copy`/`PasteAsync`
               and `BlowClipboard` for copy and paste, `ToObject`/`RestoreAsync` for saving, `IsValidConnection` for
               connection rules, and the `--bit-blow-*` properties `GetBlowTheme` lists for restyling.
            7. If the feature builds or saves flows, run one through `InspectBlowFlow` to check that it deserializes,
               that the canvas raises nothing through OnError, and what a layout does with it.
            8. Write the code, then say which tool call each non-obvious choice came from.
            """;
    }

    [McpServerPrompt(Name = "debug-blow-flow", Title = "Debug a canvas or flow")]
    [Description("Diagnoses a Bit.Blow problem - a canvas that shows nothing, nodes piled up unstyled, nodes that cannot be dragged or connected, edges that attach to the wrong place or vanish, a page that does not update after an edit, a saved flow that does not restore.")]
    public static string DebugBlowFlow(
        [Description("What goes wrong, with the page, the nodes or the flow involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Blow problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBlowSetupGuide` for this render mode first - the three silent failures:
               the stylesheet `_content/Bit.Blow/bit-blow.css` linked from the host page (without it nodes pile up
               unstyled), a container with a height around the canvas (without one it is zero pixels tall and shows
               nothing), and an interactively rendered page (on a static one nothing can be dragged, connected or
               measured). No invented service registration.
            2. Bind `OnError` on the canvas, or pass the flow (`JsonSerializer.Serialize(canvas.ToObject())`) to
               `InspectBlowFlow`: it renders it through the real canvas and reports duplicate node, edge and handle ids,
               edges to missing nodes, missing parents and parent cycles, and edges naming a handle the node lacks.
            3. Call `SearchBlow` with the symptom and read the guide section it points to - "Nodes", "Handles and
               connections", "Edges", "Sub-flows and grouping", "Saving and restoring" and "Errors and diagnostics" cover
               the usual causes.
            4. If the page does not update after an edit, check how the change happened: the canvas edits the lists in
               place and re-renders only itself, so markup that reads the graph needs `NodesChanged`/`EdgesChanged`
               bound, a change made outside a render of your component needs `Refresh(remeasure: true)` (or
               `InvokeAsync(StateHasChanged)`), and a handle moved to another side needs `UpdateNodeInternals()`.
            5. Confirm the API you are relying on with `GetBlowApi` before concluding it is a bug - check the actual
               default value of the parameter involved (several are off by default: `HistoryEnabled`,
               `ClipboardShortcutsEnabled`, `SnapToGrid`, `SelectionOnDrag`).

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
