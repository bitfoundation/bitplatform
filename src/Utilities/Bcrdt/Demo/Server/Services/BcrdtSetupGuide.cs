using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one hosting model: the checklist, the files of a working setup, the guide's
/// own sections on that model, and - for the Blazor Web App this demo is - the demo's real hosting files.
/// <para>
/// Collaboration fails in ways no compiler catches, and each hosting model has its own: a Blazor Web App
/// that registers the services in only one of its two containers fails while prerendering; a relative hub
/// URL is rejected when the SignalR connection is built; a bare <c>AddSignalR</c> disconnects every client
/// once a document outgrows 32 KB; a WebSocket relay mapped without <c>UseWebSockets</c> answers a flat 400;
/// a Blazor Server page that opens a document while prerendering opens it for a request that is already over;
/// and a worker that edits a document from two threads corrupts it. Handing over files that work instead of
/// prose removes the guesswork about which project each line belongs in; the guide's sections, quoted
/// verbatim, say the rest in the library's own words.
/// </para>
/// </summary>
public static partial class BcrdtSetupGuide
{
    public static readonly string[] HostingModels = ["web-app", "standalone-wasm", "server", "hybrid", "websocket", "console"];

    /// <summary>The demo files that make up its wiring - the demo is a Blazor Web App using InteractiveWebAssembly.</summary>
    private static readonly string[] _hostingFiles =
    [
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Client/Collaboration/LiveRelay.cs",
        "Demo/Server/Program.cs",
        "Demo/Client/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Pages/Live.razor",
    ];

    private sealed record Mode(string Key, string Title, string Summary, string Checklist, string[] GuideSections, string? Files, bool IncludeDemo);

    /// <summary>
    /// The wiring for one hosting model as Markdown, the index of hosting models when none is named, or null
    /// when the model is not one of <see cref="HostingModels"/>.
    /// </summary>
    public static string? Get(string? hostingModel)
    {
        if (string.IsNullOrWhiteSpace(hostingModel)) return RenderIndex();

        return Describe(hostingModel) is { } mode ? Compose(mode) : null;
    }

    /// <summary>What sets one hosting model apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string hostingModel) => Describe(hostingModel) is { } mode ? $"{mode.Title}. {mode.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    /// <summary>The hosting models, each with what sets it apart, as GetBcrdtSetupGuide answers without one.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Setting Bit.Bcrdt up").AppendLine();
        builder.AppendLine("One guide per hosting model. Call `GetBcrdtSetupGuide(hostingModel: \"...\")` for the one the app uses - ")
               .AppendLine("each comes with the checklist, the files of a working setup and the guide's own words on that model.")
               .AppendLine();

        foreach (var key in HostingModels)
        {
            var mode = Describe(key)!;
            var summary = mode.Summary.ReplaceLineEndings(" ");
            var stop = summary.IndexOf(". ", StringComparison.Ordinal);

            builder.AppendLine($"- `{key}` - {mode.Title}: {(stop > 0 ? summary[..(stop + 1)] : summary)}");
        }

        return builder.ToString();
    }

    private static Mode? Describe(string? hostingModel)
    {
        var mode = (hostingModel ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "wasm" or "webassembly" or "interactivewebassembly" or "auto" or "interactiveauto" or "webapp" or "blazor-web-app" or "signalr" => "web-app",
            "standalone" or "standalone-webassembly" or "blazor-webassembly" or "blazor-wasm" => "standalone-wasm",
            "interactiveserver" or "blazor-server" => "server",
            "maui" or "maui-hybrid" or "blazor-hybrid" or "wpf" or "winforms" => "hybrid",
            "websockets" or "web-socket" or "ws" => "websocket",
            "worker" or "background-service" or "headless" or "non-ui" or "cli" or "tests" => "console",
            _ => mode
        };

        return mode switch
        {
            "web-app" => new Mode(
                "web-app",
                "Blazor Web App - InteractiveWebAssembly, with a SignalR hub",
                """
                Two projects, two DI containers: the server prerenders the very pages the browser then runs, so
                `AddBitBcrdtServices` is called in BOTH `Program.cs` files - the demo does it through one shared extension
                method (`AddDemoServices` below), which also keeps the configuration identical. The packages and the
                components belong in the .Client project; the server project adds `Bit.Bcrdt.Server` and maps the relay.
                The hub address must be absolute, and the demo resolves it from `NavigationManager.BaseUri` when a
                transport is created (`LiveRelay`) - which works on the server too, so InteractiveAuto, whose first visit
                runs in a circuit, needs nothing else. Pages open documents only once they are interactive. This is exactly
                how the demo is built, so the files below are its own: its `Program.cs` maps both relays, and its MCP lines
                (`AddControllers`, `AddMcpServer`, `MapControllers`, `MapMcp`) serve this site's MCP server - nothing
                collaboration needs.
                """,
                UiChecklist,
                ["Getting started", "Hosting models"],
                WebAppFiles,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone-wasm",
                "standalone Blazor WebAssembly",
                """
                One project, one DI container for the lifetime of the tab, and no prerendering: a document can be opened
                from the first render. A standalone app has no server of its own to map a relay on, so the relay is a
                separate ASP.NET Core app - with CORS, because the app and the hub are on different origins - and the hub
                address is absolute. With no transport at all the document is purely local, which is the right way to build
                a feature first. `UseIndexedDbStore()` keeps documents in the browser, so they - and edits made offline -
                survive a reload; with `UseInMemoryStore()` they last as long as the tab.
                """,
                UiChecklist,
                ["Getting started", "Hosting models"],
                StandaloneFiles,
                IncludeDemo: false),

            "server" => new Mode(
                "server",
                "Blazor Server (InteractiveServer)",
                """
                One project, one DI container per circuit: `IBcrdtCollaborationService` is scoped, so every connected user
                gets their own replica and their own client id - never re-register it as a singleton, which would make every
                visitor one replica whose edits the engine cannot tell apart. The replicas of all circuits live in this one
                process, so they meet over `BcrdtInMemoryNetwork.Shared` with no relay at all; behind a load balancer, map
                `BcrdtHub` with a SignalR backplane and connect every circuit to it with `BcrdtSignalRTransportFactory` at an
                absolute address instead. Pages are prerendered first, and a document opened then belongs to a request that
                is already over: open it once `RendererInfo.IsInteractive`. A circuit outlives its browser tab by the circuit
                retention period, so a `CircuitHandler` suspends presence the moment the connection drops.
                """,
                UiChecklist,
                ["Hosting models", "The five-minute version", "Presence"],
                ServerFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "hybrid",
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                One process, one replica, no prerendering and no circuits - the WebAssembly model with the local process as
                the replica. The relay is a server you host, at an absolute address. Documents persist in the app's data
                directory through `BcrdtFileSystemDocumentStore`, so edits made offline survive a restart and go out the next
                time the hub is reachable.
                """,
                UiChecklist,
                ["Hosting models", "Persistence"],
                HybridFiles,
                IncludeDemo: false),

            "websocket" => new Mode(
                "websocket",
                "the WebSocket transport and relay, instead of SignalR",
                """
                The raw WebSocket transport is built into Bit.Bcrdt - no SignalR client, no hub - and its relay is
                `MapBitBcrdtWebSocket` in Bit.Bcrdt.Server, which needs `app.UseWebSockets()` in the pipeline before it
                (without it every connection is answered with a flat 400). One socket per document, the document id a path
                segment or a query parameter. Its rooms are process-local, so behind a load balancer it needs sticky sessions
                or a broker. It swaps in for the SignalR transport in any hosting model: only the transport factory and the
                relay change.
                """,
                UiChecklist,
                ["Transports and relays", "Map the relay"],
                WebSocketFiles,
                IncludeDemo: false),

            "console" => new Mode(
                "console",
                "a non-UI peer (console app, worker, server-side replica)",
                """
                No UI and no DI container: a `BcrdtDocument` is the replica itself, and `BcrdtSyncExchange` runs the
                handshake's decisions - the same ones `BcrdtSyncProvider` and the test simulator run. A document is not
                thread-safe while a transport raises its events on I/O threads, so a peer that edits owns the document from
                one loop, as below. A peer that only listens - an archiver, an indexer - can use `BcrdtSyncProvider`, which
                adds reconnection with backoff and presence: without a synchronization context to capture it serializes its
                own access to the document, and the application must then not edit the document concurrently with it.
                """,
                ConsoleChecklist,
                ["Working with documents", "The synchronization protocol"],
                ConsoleFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting Bcrdt up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(mode.Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is once the projects and namespaces are renamed to your own - except what is the ")
                   .AppendLine("demo's own: `DemoWorkspace` and the side-by-side replicas `AddDemoServices` registers serve the other demo pages, ")
                   .AppendLine("`ConnectionBadge` is a badge, and the room and relay switching of the live page are there to let visitors share a link.")
                   .AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BcrdtSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BcrdtSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string UiChecklist = """
        ## Checklist

        1. `dotnet add package Bit.Bcrdt` (the engine and the Blazor components) to the project with the collaborative
           components - the .Client project of a Blazor Web App - and a transport: `Bit.Bcrdt.SignalR`, or the WebSocket
           transport built into `Bit.Bcrdt`, or none at all for a purely local document. The relay is `Bit.Bcrdt.Server`,
           on the server.
        2. Call `builder.Services.AddBitBcrdtServices(...)` in EVERY DI container that renders collaborative components - a
           Blazor Web App has two - through one shared extension method so both get the same configuration. It registers
           `IBcrdtCollaborationService` scoped: one replica per circuit in Blazor Server, one per app in WebAssembly. Never
           re-register it as a singleton.
        3. Give it a transport with an ABSOLUTE address: the .NET SignalR client rejects `"/hub/bcrdt"` when the connection
           is built. Resolve it against the host - `builder.HostEnvironment.BaseAddress` in WebAssembly,
           `NavigationManager.BaseUri` where a component or a scoped service can ask.
        4. On the relay: `builder.Services.AddBitBcrdtHub()` - not a bare `AddSignalR`, whose 32 KB receive limit disconnects
           every client once a document outgrows it - and `app.MapHub<BcrdtHub>("/hub/bcrdt")`; or `app.UseWebSockets()`
           BEFORE `app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}")`. Either relay only relays; add
           `builder.Services.AddBitBcrdtDocumentHost(...)` to keep every document on the server as well.
        5. Add `@using Bit.Bcrdt` and `@using Bit.Bcrdt.Blazor` to `_Imports.razor`.
        6. Open a document with `<BcrdtCollaborationScope DocumentId="..." User="...">`, which opens it, cascades it and
           releases it - and on .NET 9 and later waits until the page is interactive by itself - or with
           `IBcrdtCollaborationService.OpenAsync` paired with `CloseAsync`, and then only once the component
           is interactive wherever pages are prerendered: `RendererInfo.IsInteractive` on .NET 9 and later,
           `OnAfterRenderAsync` on .NET 8.
        7. Derive your own collaborative components from `BcrdtComponentBase` and `Track` what they render, so a change from
           anyone re-renders them, coalesced onto a frame. There is no script tag to add: `BcrdtTextArea` imports its small
           caret module from `_content/Bit.Bcrdt/bit-bcrdt.js` itself.
        """;

    private const string ConsoleChecklist = """
        ## Checklist

        1. `dotnet add package Bit.Bcrdt` - the engine, the protocol and the in-memory and WebSocket transports, none of
           which needs a web framework (the Blazor components it also carries just go unused). Add `Bit.Bcrdt.SignalR` to
           talk to a `BcrdtHub` instead.
        2. Give every session its own `BcrdtDocument`: a client id identifies one session, never a user or a machine, and two
           live replicas sharing one corrupt each other's history.
        3. Touch the document from one thread. Queue transport events onto the loop that owns it, as below, or construct
           `BcrdtSyncProvider` on a thread with a `SynchronizationContext`, which it runs every callback on.
        4. Open the handshake on every (re)connect with `BcrdtSyncExchange.CreateAnnouncement(document)`, answer every message
           with `BcrdtSyncExchange.Handle(...)`, and broadcast each local commit from `document.Updated` as
           `BcrdtSyncProtocol.EncodeUpdate(...)` - skipping what was applied from the peer, so nothing is echoed back.
        5. Catch `BcrdtFormatException` around everything that decodes: a malformed message is rejected whole and leaves the
           document exactly as it was, so dropping it is safe.
        6. To keep the document across restarts, persist `document.EncodeStateAsUpdate()` - or attach a store with
           `BcrdtDocumentPersistence.AttachAsync` - and `ApplyUpdate` it into a fresh document on start.
        """;

    private const string WebAppFiles = """
        ## The files

        The demo below maps its relays open to anyone. Before a document is anything but a demo, decide who may join it -
        the hub consults `AuthorizeAsync` before a client joins a document, and a refusal reaches the client as a final
        `BcrdtTransportAuthorizationException` rather than a failure to retry:

        ### `CollaborationHub.cs` (server project)

        ```csharp
        using Bit.Bcrdt.Server;

        public sealed class CollaborationHub : BcrdtHub
        {
            protected override ValueTask<bool> AuthorizeAsync(string documentId) =>
                ValueTask.FromResult(Context.User?.Identity?.IsAuthenticated == true);
        }
        ```

        Register it with `builder.Services.AddBitBcrdtHub<CollaborationHub>()` and map it with
        `app.MapHub<CollaborationHub>("/hub/bcrdt")` in place of the plain `BcrdtHub`.
        """;

    private const string StandaloneFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Bit.Bcrdt;
        using Bit.Bcrdt.SignalR;
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // The relay below runs on its own origin, and the .NET SignalR client only takes an absolute address.
        // IndexedDB keeps documents - and edits made offline - across a reload.
        builder.Services.AddBitBcrdtServices(collaboration => collaboration
            .UseTransport(new BcrdtSignalRTransportFactory("https://collab.example.com/hub/bcrdt"))
            .UseIndexedDbStore());

        await builder.Build().RunAsync();
        ```

        ### `Program.cs` of the relay (an ASP.NET Core app with `Bit.Bcrdt.Server`)

        ```csharp
        using Bit.Bcrdt;
        using Bit.Bcrdt.Server;

        var builder = WebApplication.CreateBuilder(args);

        // AddSignalR with a receive limit sized for sync messages: a bare AddSignalR drops clients at 32 KB.
        builder.Services.AddBitBcrdtHub();

        // A replica of every document on the server, kept in a store, so a document outlives every tab.
        builder.Services.AddBitBcrdtDocumentHost(_ => new BcrdtDocumentHostOptions
        {
            Store = new BcrdtFileSystemDocumentStore(Path.Combine(builder.Environment.ContentRootPath, "documents")),
        });

        // The app is served from another origin, and SignalR's negotiate request is a cross-origin POST.
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins("https://app.example.com")
            .AllowAnyHeader()
            .WithMethods("GET", "POST")
            .AllowCredentials()));

        var app = builder.Build();

        app.UseCors();
        app.MapHub<BcrdtHub>("/hub/bcrdt");

        app.Run();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.Bcrdt
        @using Bit.Bcrdt.Blazor
        ```

        ### `Pages/Notes.razor`

        ```razor
        @page "/notes/{Id}"

        <BcrdtCollaborationScope DocumentId="@($"notes-{Id}")" User="_me" Context="document">
            <LoadingContent>
                <p>Opening…</p>
            </LoadingContent>
            <ChildContent>
                <BcrdtPresenceView Presence="document.Presence" />
                <BcrdtTextArea Text="@document.Text("body")" Presence="document.Presence" Field="body" />
            </ChildContent>
        </BcrdtCollaborationScope>

        @code {
            [Parameter] public string Id { get; set; } = default!;

            // An application publishes its signed-in user; SuggestColor gives that user the same colour everywhere.
            private readonly BcrdtCollaboratorInfo _me = new("u-1", "Ada", BcrdtPresenceService.SuggestColor("u-1"));
        }
        ```
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Bit.Bcrdt;
        using Microsoft.AspNetCore.Components.Server.Circuits;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // One replica per circuit (the service is scoped), all in this process, meeting over the in-memory network.
        // The file store serializes the appends of every replica of a document, so they can share it.
        builder.Services.AddBitBcrdtServices(collaboration => collaboration
            .UseTransport(documentId => BcrdtInMemoryNetwork.Shared.CreateTransport(documentId))
            .UseStore(new BcrdtFileSystemDocumentStore(Path.Combine(builder.Environment.ContentRootPath, "documents"))));

        // Resolved from each circuit's own scope, so it sees that circuit's documents.
        builder.Services.AddScoped<CircuitHandler, PresenceCircuitHandler>();

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `PresenceCircuitHandler.cs`

        ```csharp
        using Bit.Bcrdt;
        using Microsoft.AspNetCore.Components.Server.Circuits;

        // A circuit outlives its tab by the retention period, and would keep the user listed until then. This
        // withdraws them the moment the connection drops, and publishes them again if it comes back.
        public sealed class PresenceCircuitHandler(IBcrdtCollaborationService collaboration) : CircuitHandler
        {
            public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
            {
                foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments) document.Presence.Suspend();
                return Task.CompletedTask;
            }

            public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
            {
                foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments) document.Presence.Resume();
                return Task.CompletedTask;
            }
        }
        ```

        ### `Components/Pages/Notes.razor`

        ```razor
        @page "/notes/{Id}"
        @rendermode InteractiveServer

        @if (!RendererInfo.IsInteractive)
        {
            <p>Connecting…</p>
        }
        else
        {
            <BcrdtCollaborationScope DocumentId="@($"notes-{Id}")" User="_me" Context="document">
                <BcrdtPresenceView Presence="document.Presence" />
                <BcrdtTextArea Text="@document.Text("body")" Presence="document.Presence" Field="body" />
            </BcrdtCollaborationScope>
        }

        @code {
            [Parameter] public string Id { get; set; } = default!;

            private readonly BcrdtCollaboratorInfo _me = new("u-1", "Ada", BcrdtPresenceService.SuggestColor("u-1"));
        }
        ```

        ### `Components/_Imports.razor`

        ```razor
        @using Bit.Bcrdt
        @using Bit.Bcrdt.Blazor
        ```

        Behind a load balancer the circuits of one document are in different processes. Keep the same wiring, but map the
        hub (`AddBitBcrdtHub().AddStackExchangeRedis(...)` or another backplane, `MapHub<BcrdtHub>`) and replace the
        in-memory transport with `new BcrdtSignalRTransportFactory("https://collab.example.com/hub/bcrdt")`.
        """;

    private const string HybridFiles = """
        ## The files

        ### `MauiProgram.cs`

        ```csharp
        using Bit.Bcrdt;
        using Bit.Bcrdt.SignalR;

        public static class MauiProgram
        {
            public static MauiApp CreateMauiApp()
            {
                var builder = MauiApp.CreateBuilder();
                builder.UseMauiApp<App>();

                builder.Services.AddMauiBlazorWebView();

                // Documents survive restarts in the app's data directory; edits made offline go out on reconnect.
                builder.Services.AddBitBcrdtServices(collaboration => collaboration
                    .UseTransport(new BcrdtSignalRTransportFactory("https://collab.example.com/hub/bcrdt"))
                    .UseStore(new BcrdtFileSystemDocumentStore(Path.Combine(FileSystem.AppDataDirectory, "bcrdt"))));

                return builder.Build();
            }
        }
        ```

        WPF and Windows Forms are the same registration after `AddWpfBlazorWebView()` or `AddWindowsFormsBlazorWebView()`,
        with `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` for the store.

        ### `_Imports.razor`

        ```razor
        @using Bit.Bcrdt
        @using Bit.Bcrdt.Blazor
        ```

        The pages are the ones a WebAssembly app would write - `GetBcrdtSetupGuide(hostingModel: "standalone-wasm")` has one -
        and need no `RendererInfo.IsInteractive` check: nothing in a BlazorWebView is prerendered.
        """;

    private const string WebSocketFiles = """
        ## The files

        ### `Program.cs` of the client (the WebAssembly half of a Blazor Web App)

        ```csharp
        using Bit.Bcrdt;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // {documentId} is filled in for each document, and an http(s) address is rewritten to ws(s).
        string endpoint = $"{builder.HostEnvironment.BaseAddress}ws/bcrdt/{BcrdtWebSocketTransportFactory.DocumentIdPlaceholder}";

        builder.Services.AddBitBcrdtServices(collaboration => collaboration
            .UseTransport(new BcrdtWebSocketTransportFactory(endpoint)));

        await builder.Build().RunAsync();
        ```

        ### `Program.cs` of the server (with `Bit.Bcrdt.Server`)

        ```csharp
        using Bit.Bcrdt.Server;

        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        // Before the mapping: without it every connection is answered with a flat 400.
        app.UseWebSockets();

        // A refusal is answered with 403, which the client reports as a final BcrdtTransportAuthorizationException.
        // Leave authorize out only for a demo: then anyone may join any document.
        app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}", authorize: (context, documentId) =>
            ValueTask.FromResult(context.User.Identity?.IsAuthenticated == true));

        app.Run();
        ```

        A peer that is not a browser takes the address directly:
        `new BcrdtWebSocketTransportFactory("wss://collab.example.com/ws/bcrdt/{documentId}")`. Every client gets its own
        bounded outbound queue on the relay, so one slow connection does not hold up the others.
        """;

    private const string ConsoleFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using System.Threading.Channels;
        using Bit.Bcrdt;

        // One loop owns the document: incoming messages and this program's own edits are queued and run here, one at a
        // time, because the transport raises its events on I/O threads and a document is not thread-safe.
        using var document = new BcrdtDocument();
        await using var transport = new BcrdtWebSocketTransport(new Uri("wss://collab.example.com/ws/bcrdt/board-7"));

        var inbox = Channel.CreateUnbounded<Func<Task>>();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; inbox.Writer.TryComplete(); };

        transport.MessageReceived += message => inbox.Writer.TryWrite(() => ReceiveAsync(message));

        // Every local commit goes out as one Update message; what was applied from the peer is not echoed back.
        document.Updated += update =>
        {
            if (ReferenceEquals(update.Origin, transport) is false)
            {
                byte[] message = BcrdtSyncProtocol.EncodeUpdate(update.Update);
                inbox.Writer.TryWrite(() => transport.SendAsync(message).AsTask());
            }
        };

        await transport.ConnectAsync();

        // SyncStep1: "here is what I have" - the peers answer with what this replica is missing, and ask for the rest.
        await transport.SendAsync(BcrdtSyncExchange.CreateAnnouncement(document));

        // The program's own work is queued like everything else.
        inbox.Writer.TryWrite(() =>
        {
            document.GetMap("board").Set("updatedAt", DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        });

        await foreach (Func<Task> work in inbox.Reader.ReadAllAsync())
        {
            await work();
        }

        async Task ReceiveAsync(ReadOnlyMemory<byte> message)
        {
            var replies = new List<byte[]>();

            try
            {
                BcrdtSyncExchange.Handle(document, BcrdtSyncProtocol.Decode(message), transport, replies);
            }
            catch (BcrdtFormatException)
            {
                // Malformed, or a protocol version this build does not speak: rejected whole, so dropping it is safe.
                return;
            }

            foreach (byte[] reply in replies) await transport.SendAsync(reply);
        }
        ```

        What this leaves out is what `BcrdtSyncProvider` adds: reconnecting with backoff when the transport drops (announce
        again after every reconnect), the handshake timeout, and presence. Outside Blazor, `IBcrdtCollaborationService` works
        too - `new BcrdtCollaborationService(options, transportFactory, store)` - on a thread with a synchronization context.
        """;

    private const string Next = """
        What a merge decides is the choice that matters most after the wiring - call `GetBcrdtGuideSection(heading: "Choosing a
        data type")`, and run concurrent edits through `InspectBcrdtMerge` before building on one.
        `GetBcrdtSourceFile(path: "Demo/Client/Pages/Live.razor")` is the page an application would write, and
        `GetBcrdtSourceFile()` lists the demo's working pages.
        """;

    /// <summary>
    /// Pushes a quoted guide section one heading level down, so it nests under "From the guide"
    /// instead of competing with the headings of this document.
    /// </summary>
    private static string Demote(string section)
    {
        var lines = section.Split('\n');
        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced is false && lines[i].StartsWith('#')) lines[i] = $"#{lines[i]}";
        }

        return string.Join('\n', lines);
    }

    private static void AppendFile(StringBuilder builder, string path, string content)
    {
        content = content.TrimEnd();
        var fence = Fence(content);

        builder.AppendLine($"### `{path}`").AppendLine()
               .AppendLine($"{fence}{Language(path)}")
               .AppendLine(content)
               .AppendLine(fence)
               .AppendLine();
    }

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a file that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of the file would render as prose.
    /// </summary>
    private static string Fence(string content)
    {
        var longest = BacktickRunRegex().Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();

        return new string('`', Math.Max(3, longest + 1));
    }

    private static string Language(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
