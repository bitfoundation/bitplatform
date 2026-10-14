using Google.Protobuf;

namespace Bit.Belemetry.Tests.Otlp;

[TestClass]
public class ProtobufWriterTests
{
    [TestMethod]
    public void Primitives_AreReadableByCodedInputStream()
    {
        var w = new ProtobufWriter(16);
        w.WriteVarintField(1, 300);
        w.WriteInt64(2, -1);
        w.WriteBool(3, true);
        w.WriteFixed64(4, 0x0102030405060708);
        w.WriteSFixed64(5, -42);
        w.WriteFixed32(6, 0xDEADBEEF);
        w.WriteDouble(7, Math.PI);
        w.WriteString(8, "héllo");
        w.WriteBytes(9, new byte[] { 9, 8, 7 });
        w.WriteHexId(10, "0102030405060708", 8);
        w.WriteStringIfNotEmpty(11, null);
        w.WriteStringIfNotEmpty(12, string.Empty);
        w.WriteHexId(13, "xyz", 8);

        var input = new CodedInputStream(w.ToArray());
        Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.Varint), input.ReadTag());
        Assert.AreEqual(300UL, input.ReadUInt64());
        Assert.AreEqual(WireFormat.MakeTag(2, WireFormat.WireType.Varint), input.ReadTag());
        Assert.AreEqual(-1L, input.ReadInt64());
        Assert.AreEqual(WireFormat.MakeTag(3, WireFormat.WireType.Varint), input.ReadTag());
        Assert.IsTrue(input.ReadBool());
        Assert.AreEqual(WireFormat.MakeTag(4, WireFormat.WireType.Fixed64), input.ReadTag());
        Assert.AreEqual(0x0102030405060708UL, input.ReadFixed64());
        Assert.AreEqual(WireFormat.MakeTag(5, WireFormat.WireType.Fixed64), input.ReadTag());
        Assert.AreEqual(-42L, input.ReadSFixed64());
        Assert.AreEqual(WireFormat.MakeTag(6, WireFormat.WireType.Fixed32), input.ReadTag());
        Assert.AreEqual(0xDEADBEEFu, input.ReadFixed32());
        Assert.AreEqual(WireFormat.MakeTag(7, WireFormat.WireType.Fixed64), input.ReadTag());
        Assert.AreEqual(Math.PI, input.ReadDouble());
        Assert.AreEqual(WireFormat.MakeTag(8, WireFormat.WireType.LengthDelimited), input.ReadTag());
        Assert.AreEqual("héllo", input.ReadString());
        Assert.AreEqual(WireFormat.MakeTag(9, WireFormat.WireType.LengthDelimited), input.ReadTag());
        CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, input.ReadBytes().ToByteArray());
        Assert.AreEqual(WireFormat.MakeTag(10, WireFormat.WireType.LengthDelimited), input.ReadTag());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, input.ReadBytes().ToByteArray());

        // Empty strings and malformed ids are omitted.
        Assert.IsTrue(input.IsAtEnd);
    }

    [TestMethod]
    public void NegativeInt64_IsTenBytes()
    {
        var w = new ProtobufWriter();
        w.WriteInt64(1, -1);
        Assert.AreEqual(11, w.Length); // 1 tag byte + 10 varint bytes, per the spec for negative int64
    }

    [TestMethod]
    public void PackedRepeated_AreReadable()
    {
        var w = new ProtobufWriter();
        w.WritePackedFixed64(1, new long[] { 1, 2, long.MaxValue });
        w.WritePackedDouble(2, new[] { 0.5, -1.0 });
        w.WritePackedFixed64(3, ReadOnlySpan<long>.Empty);

        var input = new CodedInputStream(w.ToArray());
        Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), input.ReadTag());
        var length = input.ReadLength();
        Assert.AreEqual(24, length);
        Assert.AreEqual(1UL, input.ReadFixed64());
        Assert.AreEqual(2UL, input.ReadFixed64());
        Assert.AreEqual((ulong)long.MaxValue, input.ReadFixed64());
        Assert.AreEqual(WireFormat.MakeTag(2, WireFormat.WireType.LengthDelimited), input.ReadTag());
        Assert.AreEqual(16, input.ReadLength());
        Assert.AreEqual(0.5, input.ReadDouble());
        Assert.AreEqual(-1.0, input.ReadDouble());
        Assert.IsTrue(input.IsAtEnd);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(127)]
    [DataRow(128)]
    [DataRow(200)]
    [DataRow(16_383)]
    [DataRow(16_384)]
    [DataRow(20_000)]
    [DataRow(2_100_000)]
    public void NestedMessage_LengthPrefixIsMinimal(int bodyLength)
    {
        var w = new ProtobufWriter(64);
        var outer = w.BeginMessage(1);
        var inner = w.BeginMessage(2);
        w.WriteBytes(3, new byte[bodyLength]);
        w.EndMessage(inner);
        w.EndMessage(outer);
        w.WriteVarintField(4, 7); // a field after the nested message must land right after it

        var bytes = w.ToArray();
        var input = new CodedInputStream(bytes);

        Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), input.ReadTag());
        var outerLength = input.ReadLength();
        Assert.AreEqual(WireFormat.MakeTag(2, WireFormat.WireType.LengthDelimited), input.ReadTag());
        var innerLength = input.ReadLength();
        Assert.AreEqual(WireFormat.MakeTag(3, WireFormat.WireType.LengthDelimited), input.ReadTag());
        var payload = input.ReadBytes();
        Assert.AreEqual(bodyLength, payload.Length);
        Assert.AreEqual(innerLength, 1 + CodedOutputStream.ComputeLengthSize(bodyLength) + bodyLength);
        Assert.AreEqual(outerLength, 1 + CodedOutputStream.ComputeLengthSize(innerLength) + innerLength);
        Assert.AreEqual(WireFormat.MakeTag(4, WireFormat.WireType.Varint), input.ReadTag());
        Assert.AreEqual(7UL, input.ReadUInt64());
        Assert.IsTrue(input.IsAtEnd);

        // No slack: the total equals the minimal protobuf encoding.
        Assert.AreEqual(1 + CodedOutputStream.ComputeLengthSize(outerLength) + outerLength + 2, bytes.Length);
    }

    [TestMethod]
    public void BufferGrows_FromTinyInitialCapacity()
    {
        var w = new ProtobufWriter(1);
        var text = new string('x', 10_000);
        w.WriteString(1, text);

        var input = new CodedInputStream(w.ToArray());
        input.ReadTag();
        Assert.AreEqual(text, input.ReadString());
    }
}
