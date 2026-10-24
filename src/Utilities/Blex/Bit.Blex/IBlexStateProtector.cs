namespace Bit.Blex;

/// <summary>
/// Protects persisted state on its way to and from storage: encryption, and the tamper check that comes with it.
/// Set it as <see cref="BlexPersistenceOptions.Protector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Browser storage is readable by anyone with the device, and by every script on the origin. A protector whose key
/// the browser never sees -- ASP.NET Core Data Protection on a Blazor Server circuit, a key held by the server -- keeps
/// a stored cart or draft unreadable and unforgeable there. One whose key lives in the same browser only protects
/// against reading the storage at rest, not against script on the page.
/// </para>
/// <para>
/// Both methods may be asynchronous (Web Crypto is). Writes stay in dispatch order: a payload is protected inside the
/// same ordered chain that writes it, and one that fails to protect is reported and never written in the clear.
/// </para>
/// </remarks>
/// <example><code>
/// // Blazor Server: ASP.NET Core Data Protection, the key never leaves the server.
/// sealed class DataProtectionProtector(IDataProtectionProvider provider) : IBlexStateProtector
/// {
///     readonly IDataProtector _protector = provider.CreateProtector("Bit.Blex.State");
///
///     public ValueTask&lt;string&gt; ProtectAsync(string store, string payload, CancellationToken ct = default)
///         => new(_protector.Protect(payload));
///
///     public ValueTask&lt;string?&gt; UnprotectAsync(string store, string payload, CancellationToken ct = default)
///     {
///         // Data Protection payloads start with its magic header ("CfDJ8"); anything else was saved in the clear.
///         if (!payload.StartsWith("CfDJ8", StringComparison.Ordinal))
///             return new((string?)null);
///
///         try
///         {
///             return new(_protector.Unprotect(payload));
///         }
///         catch (CryptographicException ex) when (ex.InnerException is null)
///         {
///             // The key is gone or the payload was tampered with: it will never read. Data Protection wraps a failure
///             // that may pass (its key store out of reach) instead; that one propagates, and the value is kept.
///             return new((string?)null);
///         }
///     }
/// }
/// </code></example>
public interface IBlexStateProtector
{
    /// <summary>Turns <paramref name="payload"/>, the serialized state of <paramref name="storeName"/>, into what is stored.</summary>
    ValueTask<string> ProtectAsync(string storeName, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns a stored value back into the payload <see cref="ProtectAsync"/> was given, or returns <c>null</c> when it will
    /// never read -- saved before protection was turned on, written by something else, protected with a key that is gone,
    /// or tampered with. A <c>null</c> payload is reported, discarded and removed from storage, so the store starts from
    /// its defaults. Throw for a failure that may pass (a key service that is unreachable or timed out): the failure is
    /// reported, the stored value is kept, the other stores hydrate, and this store does not save -- so its defaults
    /// cannot overwrite what could not be read -- until <see cref="BlexStatePersistor.RehydrateAsync"/> gets through.
    /// </summary>
    ValueTask<string?> UnprotectAsync(string storeName, string protectedPayload, CancellationToken cancellationToken = default);
}
