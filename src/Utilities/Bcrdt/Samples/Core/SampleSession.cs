namespace Bit.Bcrdt.Samples.Core;

/// <summary>
/// Who this session publishes through presence: a random guest, standing in for the signed-in user an application
/// would publish.
/// </summary>
/// <remarks>
/// Registered per scope, so it lasts as long as a replica does: one per tab in WebAssembly, one per circuit in Blazor
/// Server, and one for the whole app in MAUI.
/// </remarks>
public sealed class SampleSession
{
    public SampleSession()
    {
        string id = Guid.NewGuid().ToString("N");
        User = new BcrdtCollaboratorInfo(id, $"Guest {Random.Shared.Next(100, 1000)}", BcrdtPresenceService.SuggestColor(id));
    }

    /// <summary>The identity every sample page opens its document as.</summary>
    public BcrdtCollaboratorInfo User { get; }
}
