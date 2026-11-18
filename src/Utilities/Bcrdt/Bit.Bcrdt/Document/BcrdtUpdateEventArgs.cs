namespace Bit.Bcrdt;

/// <summary>Carries an update produced by a committed transaction.</summary>
/// <param name="Update">The encoded delta, ready to send to peers or append to storage.</param>
/// <param name="Origin">The <see cref="BcrdtTransaction.Origin"/> of the transaction that produced it.</param>
/// <param name="Transaction">The transaction itself.</param>
public readonly record struct BcrdtUpdateEventArgs(byte[] Update, object? Origin, BcrdtTransaction Transaction);
