using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>Reconstructs a primary key from the binary wire format.</summary>
public delegate TKey BlazeDbKeyReader<TKey>(ref BlazeDbBufferReader reader);
