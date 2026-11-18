namespace Bit.Bcrdt;

/// <summary>Damage a store found in a document's log, and what it did about it.</summary>
/// <param name="DocumentId">The document whose log was damaged.</param>
/// <param name="QuarantinePath">
/// Where an untouched copy of the damaged log was put before anything was removed from it, so nothing
/// that might still be recoverable by hand is lost.
/// </param>
/// <param name="RecordsKept">How many intact records the log holds once repaired.</param>
/// <param name="BytesDiscarded">How many bytes of the log could not be read as intact records.</param>
public sealed record BcrdtStoreCorruption(string DocumentId, string QuarantinePath, int RecordsKept, long BytesDiscarded);
