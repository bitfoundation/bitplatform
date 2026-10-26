using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.Bcrdt.Samples.Server;

/// <summary>
/// Withdraws a circuit's collaborators from presence while its browser connection is down, and restores them when it
/// comes back.
/// </summary>
/// <remarks>
/// A closed or crashed tab leaves its circuit - and so its open documents and their presence heartbeat - alive for
/// the circuit retention period, which would keep the person listed as present for minutes. Bit.Bcrdt cannot
/// register this itself, because it also runs in WebAssembly, where circuits do not exist.
/// </remarks>
public sealed class PresenceCircuitHandler(IBcrdtCollaborationService collaboration) : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments)
        {
            document.Presence.Suspend();
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        foreach (BcrdtCollaborativeDocument document in collaboration.OpenDocuments)
        {
            document.Presence.Resume();
        }

        return Task.CompletedTask;
    }
}
