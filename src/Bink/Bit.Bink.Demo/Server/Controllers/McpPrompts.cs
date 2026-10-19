using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bink.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a drawing library for: add it to
/// an app, build a drawing feature with it, and work out why a canvas does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a canvas that silently never draws, or a document that
/// silently loses data, rather than a compiler error - because the failure mode of an agent with a
/// handful of tools is not ignorance, it is calling them in a sequence that skips the check which
/// would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bink-to-app", Title = "Add Bit.Bink to a Blazor app")]
    [Description("Walks through adding Bit.Bink to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBinkToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode: server, wasm, auto, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Bink to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, and for a MAUI/WPF/Windows Forms BlazorWebView host, then continue with what you find.
            2. Call `GetBinkSetupGuide` with that render mode and follow it. In a Blazor Web App with an interactive
               client the package and the pages with a canvas belong in the .Client project.
            3. Do NOT register services or link a stylesheet: Bit.Bink has no services, and its Blazor initializer
               injects `_content/Bit.Bink/bit-bink.css` itself. Inventing an `AddBink...` call is the most common mistake.
            4. Add `@using Bit.Bink` to `_Imports.razor` (plus `@using System.Numerics` where documents are built in code).
            5. Put the canvas on an interactively rendered page - a statically rendered one never attaches it.
            6. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the canvas pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-bink-feature", Title = "Implement a drawing feature")]
    [Description("Implements a drawing feature with Bit.Bink - a signature field, a handwriting or whiteboard surface, image markup, saving and loading, export, collaboration - using its real API rather than a guessed one.")]
    public static string ImplementBinkFeature(
        [Description("What the drawing feature should do, in your own words - e.g. 'a signature pad that exports a PNG' or 'save the drawing to the database and load it back'.")] string feature)
    {
        return $"""
            Implement this with Bit.Bink: {feature}

            Work in this order:

            1. Call `SearchBink` with the request above. It searches the guide, every public member, the brushes,
               the setup guides and the demo's sources at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBinkApi` for every Bink type you are about to use, and match the member names, types and
               defaults exactly. There is one package and one namespace, `Bit.Bink`, and every public type is prefixed
               `Bink`; do not borrow names from another canvas or ink library.
            4. For a brush, read its real defaults with `GetBinkBrushes` and derive variants with a `with`
               expression - brushes are immutable records.
            5. Prefer the library's own mechanism over hand-rolling one: `BinkSerializer` (and `TryDeserialize` for
               untrusted input) over your own JSON, `ExportSvgAsync`/`ExportPngAsync` or `BinkSvgExporter`/
               `BinkPngExporter` over capturing pixels yourself, `Undo`/`Redo`/`History` over keeping snapshots,
               the built-in tools (`BinkDrawTool`, `BinkEraserTool`, `BinkSelectTool`, `BinkShapeTool`, `BinkTextTool`,
               `BinkPanTool`) over handling pointer events, `BinkCollaborationSession` over syncing documents yourself.
            6. Bind `DocumentChanged` when your markup reads canvas state (`CurrentDocument.IsEmpty`, `History.CanUndo`):
               drawing never re-renders the page on its own.
            7. If the feature saves documents, run a saved one through `InspectBinkDocument` to check that it loads,
               round-trips and exports the way you expect.
            8. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBinkSourceFile` and follow its
            shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-bink-canvas", Title = "Debug a canvas or document")]
    [Description("Diagnoses a Bit.Bink problem - a canvas that does not draw, ink that lags, touch that scrolls, a document that does not load back, an export that is missing something.")]
    public static string DebugBinkCanvas(
        [Description("What goes wrong, with the page, the tool or the document involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bink problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBinkSetupGuide` for this render mode first: the canvas on an interactively
               rendered page, in the project that runs it, with a height; no invented service registration. Handle
               `Unavailable` (or read `BinkCanvas.AttachFailed`) - when the browser layer cannot start, it says why.
            2. Call `SearchBink` with the symptom, and read the guide section it points to - "Hosting models" covers
               Blazor Server latency and wet ink, "Mobile, touch and stylus" covers touch that scrolls
               (`EnableTouchGestures`) and palm rejection, "Serialization" covers documents that do not load,
               "SVG and PNG export" and "Cropping" cover exports.
            3. For a document problem, pass the document to `InspectBinkDocument`: it runs the library's own serializer
               and exporters, and its notes name unregistered brush kinds, unknown elements, hidden layers, elements
               outside the page and the text and images the server-side PNG path skips.
            4. Confirm the API you are relying on with `GetBinkApi` before concluding it is a bug - check the actual
               default value of the parameter or option involved.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
