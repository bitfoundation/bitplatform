namespace Bit.Bcrdt;

/// <summary>
/// A clock that only moves when the simulation says so.
/// </summary>
/// <remarks>
/// Starts at a fixed instant rather than now, so that two runs with the same seed see the same times.
/// </remarks>
public sealed class BcrdtSimulatedClock : TimeProvider
{
    private DateTimeOffset _now = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>Moves the clock forward.</summary>
    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        _now += delta;
    }
}
