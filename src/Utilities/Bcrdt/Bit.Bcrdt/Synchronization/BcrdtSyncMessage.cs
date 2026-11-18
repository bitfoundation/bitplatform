namespace Bit.Bcrdt;

/// <summary>A decoded protocol message.</summary>
/// <param name="Kind">What the message is.</param>
/// <param name="Payload">Its body, which is empty for the kinds that carry none.</param>
public readonly record struct BcrdtSyncMessage(BcrdtSyncMessageKind Kind, ReadOnlyMemory<byte> Payload);
