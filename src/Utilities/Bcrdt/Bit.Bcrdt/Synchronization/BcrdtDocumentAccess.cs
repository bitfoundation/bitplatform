namespace Bit.Bcrdt;

/// <summary>What a connection is allowed to do with a document.</summary>
/// <remarks>
/// <para>
/// Decided by the server - a relay's authorization hook - and announced to the client in an
/// <see cref="BcrdtSyncMessageKind.Auth"/> message when it joins. The values are on the wire, which is
/// why they are not in privilege order: <see cref="ReadWrite"/> is <c>1</c> because that is what a
/// permitted <c>Auth</c> message has always carried, so a client that predates read-only access still
/// reads a read-only grant as "permitted" rather than as a refusal. Never renumber an entry.
/// </para>
/// <para>
/// Read-only access is enforced where it means something: the relay drops every
/// <see cref="BcrdtSyncMessageKind.Update"/> and <see cref="BcrdtSyncMessageKind.SyncStep2"/> a read-only
/// connection sends, so nothing it writes reaches anybody else. The client is told only so that it can
/// stop offering edits that would go nowhere; a modified client that ignores the grant gains nothing.
/// </para>
/// </remarks>
public enum BcrdtDocumentAccess : byte
{
    /// <summary>No access. The connection is refused.</summary>
    None = 0,

    /// <summary>May read the document and publish presence, and may change the document.</summary>
    ReadWrite = 1,

    /// <summary>May read the document and publish presence, but changes it makes are not accepted.</summary>
    ReadOnly = 2,
}
