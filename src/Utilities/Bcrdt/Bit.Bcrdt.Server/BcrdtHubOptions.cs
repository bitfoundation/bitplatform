namespace Bit.Bcrdt;

/// <summary>
/// Limits <see cref="BcrdtHub"/> applies to what one connection may ask for.
/// </summary>
/// <remarks>
/// <para>
/// <c>AddBitBcrdtHub()</c> registers the defaults; register an instance of your own to change them -
/// <c>services.AddSingleton(new BcrdtHubOptions { MaxDocumentsPerConnection = 64 })</c> - or override the hub's
/// <c>MaxDocumentsPerConnection</c> and <c>MaxDocumentIdLength</c> in a hub of your own.
/// </para>
/// <para>
/// Every document a connection joins costs the server a group membership, an entry in
/// <see cref="BcrdtRelayConnections"/> and, with a <see cref="BcrdtDocumentHost"/>, a loaded replica, for as
/// long as the connection lasts. Without a limit one connection could name document after document - each a
/// fresh one, if the application lets anybody create them - and hold them all. A join past either limit is
/// refused like any unauthorized join: the client hears an <c>Auth</c> message granting nothing, and the
/// connection is aborted.
/// </para>
/// </remarks>
public sealed class BcrdtHubOptions
{
    /// <summary>The default most documents one connection may join: 16.</summary>
    /// <remarks>
    /// The SignalR transport opens a connection per document, so a client of the library joins one; the rest
    /// leaves room for clients of an application's own that share a connection between a few.
    /// </remarks>
    public const int DefaultMaxDocumentsPerConnection = 16;

    /// <summary>The default longest document identifier a connection may name: 256 characters.</summary>
    public const int DefaultMaxDocumentIdLength = 256;

    /// <summary>The most documents one connection may have joined at once.</summary>
    public int MaxDocumentsPerConnection { get; init; } = DefaultMaxDocumentsPerConnection;

    /// <summary>The longest document identifier, in characters, a connection may join.</summary>
    /// <remarks>
    /// Raise it for an application whose identifiers are long by design - a path, say. An identifier is held
    /// in memory by the relay and the host, and named in every log line about its document.
    /// </remarks>
    public int MaxDocumentIdLength { get; init; } = DefaultMaxDocumentIdLength;
}
