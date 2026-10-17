using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>Reconstructs a row from the binary wire format.</summary>
public delegate TRow BlazeDbRowReader<TRow>(ref BlazeDbBufferReader reader);
