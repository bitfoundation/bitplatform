using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.Bcrdt.Server;

/// <summary>
/// Withdraws a Blazor Server circuit's collaborators from presence while its browser connection is down, and
/// restores them when it comes back.
/// </summary>
/// <remarks>
/// <para>
/// A closed or crashed tab leaves its circuit - and so its open documents and their presence heartbeat - alive for
/// the circuit retention period, which would keep the person listed as present for minutes. This handler calls
/// <see cref="BcrdtPresenceService.Suspend"/> on every document the circuit has open the moment the connection drops,
/// and <see cref="BcrdtPresenceService.Resume"/> if it returns.
/// </para>
/// <para>
/// It ships here rather than in <c>Bit.Bcrdt</c> because that package also runs in WebAssembly, where circuits do not
/// exist. Register it with <see cref="BitBcrdtServer.AddBitBcrdtCircuitPresence"/>. Blazor resolves it from each
/// circuit's own scope, so it sees that circuit's documents and no one else's.
/// </para>
/// </remarks>
/// <param name="collaboration">The circuit's collaboration service.</param>
public sealed class BcrdtPresenceCircuitHandler(IBcrdtCollaborationService collaboration) : CircuitHandler
{
    /// <inheritdoc/>
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments)
        {
            document.Presence.Suspend();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments)
        {
            document.Presence.Resume();
        }

        return Task.CompletedTask;
    }
}
