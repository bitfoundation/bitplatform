using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bortable.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a sortable-list library for: add it
/// to an app, build a drag-and-drop feature with it, and work out why a drag does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a list that silently refuses a drop, or an original that
/// silently fails to hide, rather than a compiler error - because the failure mode of an agent with a
/// handful of tools is not ignorance, it is calling them in a sequence that skips the check which
/// would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bortable-to-app", Title = "Add Bit.Bortable to a Blazor app")]
    [Description("Walks through adding Bit.Bortable to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBortableToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode: server, wasm, auto, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.Bortable to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, and for a MAUI/WPF/Windows Forms BlazorWebView host, then continue with what you find.
            2. Call `GetBortableSetupGuide` with that render mode and follow it. In a Blazor Web App with an interactive
               client the package and the pages with lists belong in the .Client project.
            3. DO register the services: `builder.Services.AddBitBortableServices();` - in EVERY Program.cs that renders a
               list. In a Blazor Web App with WebAssembly or Auto that means both the server and the .Client project: the
               server prerenders the lists and the client runs them, and without the registration in either the first
               list that renders there throws "Bit.Bortable's services are not registered". (Only a WebAssembly client
               that lazy loads Bit.Bortable leaves the registration out, and sets the `Bit.Bortable.Unregistered` runtime
               switch instead - see the guide's "Lazy loading on WebAssembly" section.)
            4. DO link the stylesheet from the host page - `_content/Bit.Bortable/bit-bortable.css` (App.razor, or
               wwwroot/index.html). Nothing injects it, and without it the dragged item is not dimmed and its original
               does not hide while it is previewed in another list.
            5. Add `@using Bit.Bortable` to `_Imports.razor`.
            6. Put the list on an interactively rendered page, bound with `@bind-Items` to an `IList<TItem>` field holding a
               `List<T>` of unique items. A statically rendered page shows the items and never answers a drag.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the list pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-bortable-feature", Title = "Implement a drag-and-drop feature")]
    [Description("Implements a sortable-list feature with Bit.Bortable - a reorderable list, a kanban board, a palette to clone from, drag handles, locked items, swap mode, multi-drag, nested trees, persisting the order, restyling the drag states - using its real API rather than a guessed one.")]
    public static string ImplementBortableFeature(
        [Description("What the feature should do, in your own words - e.g. 'a kanban board with columns cards move between' or 'save the new order to the database after a drop'.")] string feature)
    {
        return $"""
            Implement this with Bit.Bortable: {feature}

            Work in this order:

            1. Call `SearchBortable` with the request above. It searches the guide, every public member, the setup guides,
               the theme and the demo's pages at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code. When a demo page does what you
               need, fetch the sample it renders - a file of `Demo/Client/Samples` - with `GetBortableSourceFile` and
               follow its shape.
            3. Call `GetBortableApi` for every Bortable type you are about to use, and match the member names, types and
               defaults exactly. There is one package and one namespace, `Bit.Bortable`: the component is `Bortable<TItem>`,
               and SortableJS's options (`group.pull`, `group.put`, `onEnd`, `toArray()`) are not this library's API - they
               are the parameters `Group`, `Pull`, `Put`, `OnEnd` and the bound list itself.
            4. Bind every list with `@bind-Items` to an `IList<TItem>` field holding a mutable `List<T>`. The list is
               changed in place and `ItemsChanged` fires once per drag for each list that changed; persist or react in
               `OnUpdate`, `OnAdd`, `OnRemove` or `OnEnd`, which fire after the lists are final. Give the lists an `Id`
               of your own when a handler needs to tell them apart.
            5. Items must be unique within a list (Blazor's `@key`): a list never takes an item equal to one it holds. For
               `Pull="BortablePull.Clone"` give the source a `Clone` factory that returns a distinct item.
            6. Prefer the library's own mechanism over hand-rolling one: `Group` with `Pull`/`Put`/`Sort` for which list
               takes what, `Filter` for locked items, `Handle` for a grip, `Swap` to trade places, `MultiDrag` for a
               selection, a `Bortable` inside an `ItemTemplate` for a tree, and the `--bit-bortable-*` properties and
               `GhostClass`/`ChosenClass`/`SwapClass`/`SelectedClass` that `GetBortableTheme` lists for restyling.
            7. Before relying on a drag behaving a certain way, run it through `InspectBortableDrag` with your lists: it
               reports whether each list takes the item, the lists afterwards and every event in order.
            8. Write the code, then say which tool call each non-obvious choice came from.
            """;
    }

    [McpServerPrompt(Name = "debug-bortable-drag", Title = "Debug a list or a drag")]
    [Description("Diagnoses a Bit.Bortable problem - a page that throws that Bit.Bortable's services are not registered, an app that cannot load Bit.Bortable because it lazy loads it, items that cannot be dragged, a list that refuses a drop, a refused clone, an original that does not hide, an empty list that cannot be dropped into, a field that does not update.")]
    public static string DebugBortableDrag(
        [Description("What goes wrong, with the lists and their parameters involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bortable problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBortableSetupGuide` for this render mode first - `AddBitBortableServices()` in
               every Program.cs that renders a list (both projects of a Blazor Web App), the stylesheet
               `_content/Bit.Bortable/bit-bortable.css` linked from the host page (without it the original does not hide
               and the dragged item is not dimmed), and an interactively rendered page (on a static one nothing drags).
            2. Put the lists and the failing drag through `InspectBortableDrag` - their items, Group, Pull, Put, Sort,
               Disabled, Filter, Swap, MultiDrag. It runs the real component and says which step was refused and why:
               different groups, Put="false", a Pull of None, Sort="false", a Disabled or read-only list, a filtered item,
               or an item the target already holds.
            3. Call `SearchBortable` with the symptom and read the guide section it points to - "Shared lists",
               "Read-only lists", "Events", "Limitations" and "Troubleshooting" cover the usual causes, and "Lazy loading on
               WebAssembly" covers an app that cannot load Bit.Bortable because it lazy loads it.
            4. If the page does not update after a drag, check the binding: `@bind-Items` needs an `IList<TItem>` field (the
               parameter's type) holding a mutable list, and markup elsewhere on the page that reads the list re-renders
               on `ItemsChanged` or in `OnEnd`. A read-only list - an array - is never changed.
            5. Confirm the API you are relying on with `GetBortableApi` before concluding it is a bug - check the actual
               default value of the parameter involved (`Pull` is Move, `Put` and `Sort` are true, `SwapThreshold` is 1,
               `Animation` is 150).

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
