namespace Bit.Bcrdt.Server;

/// <summary>
/// What both relays - <see cref="BcrdtHub"/> and the WebSocket endpoint - refuse a join over, and the reason each
/// refusal gives the client, kept in one place so the two never tell a client different things.
/// </summary>
internal static class BcrdtRelayRefusals
{
    /// <summary>The reason given a client its authorization refused.</summary>
    /// <remarks>
    /// Deliberately vague: a message that explains whether a document exists, or why exactly access was denied,
    /// tells an unauthorized caller more than it tells a legitimate one.
    /// </remarks>
    public const string AccessDenied = "Not authorized to join this document.";

    /// <summary>The reason given a client that named a document identifier longer than the relay accepts.</summary>
    public const string DocumentIdTooLong = "The document identifier is too long.";

    /// <summary>The reason given a client that asked for more documents on one connection than the relay accepts.</summary>
    public const string TooManyDocuments = "Too many documents on one connection.";

    /// <summary>Whether a document identifier is longer than <paramref name="maxLength"/> characters.</summary>
    public static bool IsTooLong(string documentId, int maxLength) => documentId.Length > maxLength;
}
