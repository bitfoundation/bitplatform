using System.Reflection;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Demo.Server.Services;

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
public static class BsqliteServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BsqliteVersion = (
        typeof(BsqliteConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BsqliteConnection).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bsqlite is SQLite in the browser for Blazor, with EF Core on top: the stock EF Core SQLite provider over a
        connection whose engine (wa-sqlite) runs in a dedicated worker, storage backends (OPFS, IndexedDB, Cache Storage,
        in-memory) chosen and remembered per database, live queries, and a complete async ADO.NET surface.

        Every answer here is read out of Bit.Bsqlite {BsqliteVersion} as it is loaded in this server - its compiled XML
        documentation, its README and guides, the storage providers' declared capabilities, its own selection and SQL
        guards, and the demo's source files. Where what you remember and what these tools say disagree, the tools are
        describing the build in front of you.

        Working rules:

        - Start with `SearchBsqlite` unless you already know the document or type you want. Every hit names the exact
          follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - documents, public types, storage backends, source files.
        - Do not write a Bsqlite type or namespace from memory. They are spread over `Bit.Bsqlite`, `Bit.Bsqlite.Data`,
          `Bit.Bsqlite.Interop`, `Bit.Bsqlite.EntityFrameworkCore` and `Bit.Bsqlite.Storage.*`, while
          `AddBitBsqliteServices` and `AddBitBsqliteDbContext` live in `Microsoft.Extensions.DependencyInjection` and
          `UseBitBsqlite` in `Microsoft.EntityFrameworkCore`. `GetBsqliteApi` gives the real names and namespaces.
        - Call `GetBsqliteSetupGuide` before writing wiring. In a Blazor Web App the services are registered in the
          client AND the server project, and a page opens its database only once `RendererInfo.IsInteractive` is true;
          Interactive Server is not supported.
        - Use the async API throughout (`ToListAsync`, `SaveChangesAsync`, `MigrateAsync`): synchronous members throw
          `BsqliteSynchronousApiNotSupportedException`.
        - Storage selection never substitutes silently: existing data outranks preference, and a non-persistent fallback
          needs `AllowNonPersistentFallback()`. `PlanBsqliteStorageSelection` runs the library's own selection on a
          described browser; `CheckBsqliteSql` says which statements the guards refuse on which backend.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, and none executes
        SQL, so there is no reason to ration them or to ask before calling one.
        """;
}
