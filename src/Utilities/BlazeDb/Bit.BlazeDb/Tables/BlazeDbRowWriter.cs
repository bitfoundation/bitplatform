using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>Writes a row's fields into the binary wire format.</summary>
public delegate void BlazeDbRowWriter<TRow>(BlazeDbBufferWriter writer, TRow row);
