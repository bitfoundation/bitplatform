using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bcrdt.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a collaboration library for: add it to an
/// app, build a collaborative feature with it, and work out why collaboration does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are expensive to
/// make - the ones that produce an app that silently never syncs, a form that quietly throws away one person's
/// typing, or a replica shared by every visitor, rather than a compiler error - because the failure mode of an
/// agent with a handful of tools is not ignorance, it is calling them in a sequence that skips the check which
/// would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bcrdt-to-app", Title = "Add Bit.Bcrdt to an app")]
    [Description("Walks through adding real-time collaboration with Bit.Bcrdt to an existing app, in the right order for its hosting model.")]
    public static string AddBcrdtToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the model is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "web-app", "standalone-wasm", "server", "hybrid", "websocket", "console")]
        [Description("The app's hosting model: web-app, standalone-wasm, server, hybrid, websocket or console. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add real-time collaboration with Bit.Bcrdt to this app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: a separate .Client project and
               AddInteractiveWebAssemblyComponents mean 'web-app'; AddInteractiveServerComponents alone means 'server'; a
               WebAssemblyHostBuilder with no server means 'standalone-wasm'; a BlazorWebView means 'hybrid'; no UI at all
               means 'console'. Then continue with what you find.
            2. Call `GetBcrdtSetupGuide` with that hosting model and follow it. Registering `AddBitBcrdtServices` in only
               one of a Blazor Web App's two DI containers is the most common setup bug - it fails while prerendering, not
               at compile time. Register it in BOTH, through one shared extension method. Never re-register
               `IBcrdtCollaborationService` as a singleton: in Blazor Server every visitor would share one replica.
            3. Give the transport an absolute address - the .NET SignalR client rejects "/hub/bcrdt" - and on the server
               call `AddBitBcrdtHub` (not a bare AddSignalR, whose 32 KB limit drops clients once a document grows) and map
               `BcrdtHub`; for the WebSocket relay, call `UseWebSockets` before `MapBitBcrdtWebSocket`.
            4. Add `@using Bit.Bcrdt` and `@using Bit.Bcrdt.Blazor` to `_Imports.razor`, open documents with
               `BcrdtCollaborationScope` (or `OpenAsync` paired with `CloseAsync`), and only once the component is
               interactive wherever pages are prerendered.
            5. Before modelling any data, read `GetBcrdtGuideSection(heading: "Choosing a data type")`: a field people type
               prose into belongs in a nested `BcrdtText`, not a `BcrdtMap` value, or one person's edit replaces the other's.
            6. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the
            services in, which transport and relay you used, and at what address.
            """;
    }

    [McpServerPrompt(Name = "implement-bcrdt-feature", Title = "Implement a collaborative feature")]
    [Description("Implements a collaborative feature with Bit.Bcrdt - shared text, forms, lists, sets, presence, offline work, persistence, undo, history - using its real API and the merge semantics it really has.")]
    public static string ImplementBcrdtFeature(
        [Description("What should be collaborative, in your own words - e.g. 'a form several people fill in together' or 'a kanban board people reorder'.")] string feature)
    {
        return $"""
            Implement this with Bit.Bcrdt: {feature}

            Work in this order:

            1. Call `SearchBcrdt` with the request above. It searches the guide, every public member, the setup guides
               and the demo's sources at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Decide what each piece of state should do when two people change it at once - that is the data type, and
               it is the decision that matters most. In a `BcrdtMap` a key written concurrently keeps one value and the
               other is lost; a `BcrdtText` keeps both edits; a `BcrdtArray` keeps both insertions (but `Move` and `Replace`
               are delete-then-insert); a `BcrdtSet` add wins over a concurrent remove when it adds an element the adder
               did not hold. Prose goes in a nested `BcrdtText` created once, before people edit it - two replicas that
               each create one at the same key keep only one.
            4. Write the concurrent edits you are worried about as operations and run them through `InspectBcrdtMerge`. It
               runs the real engine and tells you what every replica ends up with; build on that, not on a guess.
            5. Call `GetBcrdtApi` for every Bcrdt type you are about to use, and match the member names, types and
               defaults exactly. Every public type is prefixed `Bcrdt` - do not borrow names from Yjs or Automerge.
            6. Prefer the library's own mechanism over hand-rolling one: `BcrdtTextArea` or `BcrdtTextSynchronizer` for an
               input bound to text (writing an input's whole new value into the text discards every concurrent edit),
               `BcrdtComponentBase` and `Track` for re-rendering on remote changes, `document.Presence` for who is here,
               `BcrdtUndoManager` for undo that leaves other people's edits alone, a transaction for a batch of changes,
               an `IBcrdtDocumentStore` for persistence, and `BcrdtSnapshot` for history.
            7. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBcrdtSourceFile` (the index lists
            them; `Demo/Client/Pages/Live.razor` is the page an application would write) and follow its shape rather than
            inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-bcrdt-sync", Title = "Debug collaboration")]
    [Description("Diagnoses a Bit.Bcrdt problem - edits that never arrive, one person's typing lost, clients disconnecting, a relay answering 400, presence that lingers, undo that reverts others, an update that is rejected.")]
    public static string DebugBcrdtSync(
        [Description("What goes wrong, with the document, the data type and the hosting model involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bcrdt problem: {symptom}

            Work in this order:

            1. Check the wiring against `GetBcrdtSetupGuide` for the hosting model first: `AddBitBcrdtServices` in every DI
               container that renders collaborative components, an absolute hub address, `AddBitBcrdtHub` rather than a
               bare AddSignalR (clients are dropped once a message passes 32 KB), `UseWebSockets` before
               `MapBitBcrdtWebSocket` (otherwise every connection gets a 400), and documents opened only once the page is
               interactive. Most "nothing syncs" reports end here.
            2. If an edit is lost rather than late, it is usually the merge doing what the data type says. Write the
               concurrent edits as operations and run them through `InspectBcrdtMerge`: a `BcrdtMap` key keeps one value,
               a nested text created on two replicas at once keeps one of the two, a set add of an element already present
               is a no-op. The fix is a different data type - a nested `BcrdtText` created once - not a retry.
            3. Call `SearchBcrdt` with the symptom, and read the guide section it points to - "Offline and reconnection",
               "Presence" (a Blazor Server circuit keeps publishing until AddBitBcrdtCircuitPresence's CircuitHandler suspends it), "Binding to
               inputs" (writing an input's whole value replaces the text), "Undo" (only origins in TrackedOrigins are
               captured), "Writing a transport" (a partial message is rejected for good).
            4. For an update or message that is rejected or looks wrong, decode it with `InspectBcrdtUpdate`: it reports
               the `BcrdtFormatException` a replica would throw, or the blocks, delete set and state vector it carries.
            5. Confirm the API you are relying on with `GetBcrdtApi` before concluding it is a bug - check the actual
               default of the option involved.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
