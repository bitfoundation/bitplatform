using System.Reflection;

namespace Bit.BlazeDb.Demo.Server.Services;

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
public static class BlazeDbServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the engine assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BlazeDbVersion = (
        typeof(BlazeDbDatabase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BlazeDbDatabase).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.BlazeDb is a memory-first, SQL-free embedded database for .NET, built for Blazor WebAssembly: rows live in
        managed memory as the objects you declared, reads are method calls into hash and ordered indexes, and commits are
        appended to a write-ahead log flushed in the background to OPFS, IndexedDB or files. A source generator (the
        Bit.BlazeDb.Generators package) turns a `partial` class marked [BlazeDbTable] into its table descriptor, and
        Bit.BlazeDb.EntityFrameworkCore puts EF Core and LINQ on top.

        Every answer here is read out of Bit.BlazeDb {BlazeDbVersion} as it is loaded in this server - its compiled XML
        documentation, its README, the generator itself and the demo's source files. Where what you remember and what
        these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBlazeDb` unless you already know the section or type name you want. It covers the guide, every
          public member, the generator's diagnostics, the setup guides and the demo sources at once, and every hit names
          the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, diagnostics, source files.
        - There is no SQL. Do not write a BlazeDb type or member from memory: every public type is prefixed `BlazeDb`
          (`BlazeDbDatabase`, `BlazeDbTable<TKey, TRow>`, `BlazeDbOpfsStorage`...) and `GetBlazeDbApi` gives the real
          names, types and defaults.
        - Check every table type you write with `InspectBlazeDbTable` before building on it. It runs the real generator and
          returns its diagnostics plus the key, the field numbers and the indexes it generates, so you use
          `TodoItem.Table` and `TodoItem.Indexes.Done` rather than guessed names. `GetBlazeDbDiagnostics` explains any BLZ
          error it reports.
        - Call `GetBlazeDbSetupGuide` before writing wiring. The browser backends exist only in WebAssembly, a prerendered
          or server-rendered component must not open them, and the engine assumes one writer with no concurrent readers -
          where the database may live differs per hosting model.
        - Rows are live objects: `Update` takes a new instance, and a row changed in place goes through `UpdateInPlace`
          with its previous values - or through EF Core's `SaveChanges`, which does exactly that.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
