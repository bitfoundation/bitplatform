using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>Writes a primary key into the binary wire format.</summary>
public delegate void BlazeDbKeyWriter<TKey>(BlazeDbBufferWriter writer, TKey key);
