namespace Bit.Belemetry.Samples.Core;

/// <summary>
/// A snapshot provider: it describes the cart - how many items, which products - rather than handing over the cart
/// object, and leaves out what support does not need. Belemetry sanitizes what it returns anyway: the owner's e-mail
/// address below is scrubbed to <c>[email]</c>, and a key named like a secret would be <c>REDACTED</c>.
/// </summary>
public sealed class SampleCartSnapshot(SampleCart cart) : IBelemetrySnapshotProvider
{
    /// <summary>The snapshot's name in a bundle: <c>cart</c>.</summary>
    public string Name => "cart";

    /// <summary>
    /// Describes the cart now: its item count, the products in it, whether a coupon is applied (not the code) and its
    /// owner's e-mail address, which Belemetry scrubs.
    /// </summary>
    public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object?> snapshot = new Dictionary<string, object?>
        {
            ["itemCount"] = cart.Items.Count,
            ["items"] = cart.Items.ToList(),
            ["hasCoupon"] = string.IsNullOrEmpty(cart.Coupon) is false,
            ["owner"] = cart.Owner,
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(snapshot);
    }
}
