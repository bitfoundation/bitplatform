namespace Bit.Bcrdt;

/// <summary>Reports a transport state change.</summary>
/// <param name="State">The new state.</param>
/// <param name="Error">What went wrong, when the change was caused by a failure.</param>
public readonly record struct BcrdtTransportStateChange(BcrdtTransportState State, Exception? Error);
