namespace Bit.Bcrdt;

/// <summary>
/// A provider that always returns the same identifier.
/// </summary>
/// <remarks>
/// Intended for tests and benchmarks, where reproducibility matters more than uniqueness. Using it
/// in an application would give two concurrent sessions the same identity and break convergence.
/// </remarks>
/// <param name="clientId">The identifier to hand out.</param>
public sealed class BcrdtFixedClientIdProvider(ulong clientId) : IBcrdtClientIdProvider
{
    /// <inheritdoc/>
    public ulong CreateClientId() => clientId;
}
