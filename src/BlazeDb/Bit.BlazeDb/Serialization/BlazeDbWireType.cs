namespace Bit.BlazeDb.Serialization;

/// <summary>
/// Wire type encoded in the low 3 bits of a field tag, protobuf-style. Allows readers to
/// skip fields they do not know about, which is what makes schema evolution possible.
/// </summary>
public enum BlazeDbWireType
{
    VarInt = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    Fixed32 = 5,
}
