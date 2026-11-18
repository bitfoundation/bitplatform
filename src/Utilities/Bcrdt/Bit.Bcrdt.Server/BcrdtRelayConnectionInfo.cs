namespace Bit.Bcrdt;

/// <summary>A connection a relay carries, as <see cref="BcrdtRelayConnections"/> lists it.</summary>
/// <remarks>A snapshot: it holds nothing of the document, and does not change when the connection does.</remarks>
/// <param name="DocumentId">The document the connection works on.</param>
/// <param name="ConnectionId">
/// The relay's identifier for the connection: SignalR's connection identifier, or the HTTP connection's for a WebSocket.
/// </param>
/// <param name="UserId">The authenticated user behind the connection, or <see langword="null"/> for an anonymous one.</param>
/// <param name="Access">What the connection may do with the document when the snapshot was taken.</param>
/// <param name="Relay">Which relay carries it. See <see cref="IBcrdtHostConnection.Relay"/>.</param>
public sealed record BcrdtRelayConnectionInfo(string DocumentId, string ConnectionId, string? UserId, BcrdtDocumentAccess Access, string Relay);
