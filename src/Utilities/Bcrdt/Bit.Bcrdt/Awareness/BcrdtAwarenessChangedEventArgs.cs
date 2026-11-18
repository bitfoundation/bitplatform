namespace Bit.Bcrdt;

/// <summary>Which clients appeared, changed or disappeared.</summary>
/// <param name="Added">Clients seen for the first time.</param>
/// <param name="Updated">Clients that published new state.</param>
/// <param name="Removed">Clients that left or timed out.</param>
/// <param name="Origin">What caused the change: <see langword="null"/> for local, otherwise the source.</param>
public readonly record struct BcrdtAwarenessChangedEventArgs(
    IReadOnlyList<ulong> Added,
    IReadOnlyList<ulong> Updated,
    IReadOnlyList<ulong> Removed,
    object? Origin);
