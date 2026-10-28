namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// The text the server returns as <c>instructions</c> from the <c>initialize</c> handshake.
/// <para>
/// A client puts this in front of the model once, before any tool has been called, which makes it
/// the only place where the server can say something the model will read whether or not it decides
/// to use the tools at all. So it stays short - every word is permanently in someone's context
/// window - and spends itself on the working rules rather than on describing the library, which the
/// tools do far better.
/// </para>
/// </summary>
public static class BcrdtServerInstructions
{
    /// <summary>
    /// The release the answers come from, so the text names it rather than claiming to be timeless:
    /// the version the four packages ship as on NuGet. It is written down here because no assembly
    /// carries it - a Debug build stamps 1.0.0, a Release build the four-part 1.2.0.03 for 1.2.0-pre-03 -
    /// and the build checks it against ReleaseVersion in Bit.Build.props (see the ReleaseVersionLiteral
    /// item in the .csproj), so a release cannot forget it.
    /// </summary>
    public const string BcrdtVersion = "11.0.0-pre-15";

    public static readonly string Text = $"""
        Bit.Bcrdt is real-time collaborative editing for .NET and Blazor, written natively in C#: a YATA-based CRDT engine
        (BcrdtText, BcrdtArray, BcrdtMap, BcrdtSet), a symmetric sync protocol over pluggable transports (SignalR, WebSocket,
        in-memory), presence, offline-first persistence, selective undo and Blazor components - no JavaScript CRDT library.

        Every answer here is read out of Bit.Bcrdt {BcrdtVersion} as it is loaded in this server - the compiled XML
        documentation of its four packages, its README, its real engine and decoders, and the demo's source files. Where
        what you remember (of Yjs, say) and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBcrdt` unless you already know the section or type name you want. It covers the guide, every
          public member, the setup guides and the demo sources at once, and every hit names the exact follow-up call.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - hosting models, guide sections, public types, source files.
        - Do not write a Bcrdt type or member from memory. Every public type is prefixed `Bcrdt` (`BcrdtDocument`,
          `IBcrdtCollaborationService`, `BcrdtCollaborativeDocument`...); `GetBcrdtApi` gives the real names and defaults.
        - Call `GetBcrdtSetupGuide` before writing wiring. A Blazor Web App registers `AddBitBcrdtServices` in both projects;
          the .NET SignalR client needs an absolute hub URL; the server calls `AddBitBcrdtHub` (a bare AddSignalR's 32 KB
          limit drops clients) and `UseWebSockets` before `MapBitBcrdtWebSocket`. Each mistake compiles and fails at runtime.
        - Choose the data type for what a concurrent edit should do: in a `BcrdtMap` one value wins and the other is lost, a
          `BcrdtText` keeps both. Prose belongs in a nested text, created once before people edit it. Run the edits through
          `InspectBcrdtMerge` - it runs the real engine - before building on what you think the merge decides.
        - A document is not thread-safe, and pairs every `OpenAsync` with a `CloseAsync`. `InspectBcrdtUpdate` decodes an
          update or protocol message, and says why a malformed one is rejected.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
