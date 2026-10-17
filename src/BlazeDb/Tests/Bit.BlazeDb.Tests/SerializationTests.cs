using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class SerializationTests
{
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(-1L)]
    [DataRow(long.MaxValue)]
    [DataRow(long.MinValue)]
    [DataRow(300L)]
    [DataRow(-300L)]
    public void VarInt_Roundtrips(long value)
    {
        var writer = new BlazeDbBufferWriter();
        writer.WriteVarInt(value);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.AreEqual(value, reader.ReadVarInt());
        Assert.AreEqual(0, reader.Remaining);
    }

    [TestMethod]
    [DataRow(0UL)]
    [DataRow(127UL)]
    [DataRow(128UL)]
    [DataRow(ulong.MaxValue)]
    public void VarUInt_Roundtrips(ulong value)
    {
        var writer = new BlazeDbBufferWriter();
        writer.WriteVarUInt(value);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.AreEqual(value, reader.ReadVarUInt());
    }

    [TestMethod]
    public void All_Primitive_Types_Roundtrip()
    {
        var guid = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var local = DateTime.Now;
        var dto = DateTimeOffset.Now;
        var ts = TimeSpan.FromMilliseconds(123456.789);

        var writer = new BlazeDbBufferWriter();
        writer.WriteBool(true);
        writer.WriteSingle(3.14f);
        writer.WriteDouble(Math.E);
        writer.WriteString("hello \u00e9\u00fc\u4e16\u754c");
        writer.WriteBytes([1, 2, 3]);
        writer.WriteGuid(guid);
        writer.WriteDecimal(1234567.891m);
        writer.WriteDateTime(now);
        writer.WriteDateTime(local);
        writer.WriteDateTimeOffset(dto);
        writer.WriteTimeSpan(ts);

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.IsTrue(reader.ReadBool());
        Assert.AreEqual(3.14f, reader.ReadSingle());
        Assert.AreEqual(Math.E, reader.ReadDouble());
        Assert.AreEqual("hello \u00e9\u00fc\u4e16\u754c", reader.ReadString());
        Assert.AreSequenceEqual(new byte[] { 1, 2, 3 }, reader.ReadBytes().ToArray());
        Assert.AreEqual(guid, reader.ReadGuid());
        Assert.AreEqual(1234567.891m, reader.ReadDecimal());
        Assert.AreEqual(now, reader.ReadDateTime());
        Assert.AreEqual(local, reader.ReadDateTime());
        Assert.AreEqual(DateTimeKind.Local, local.Kind);
        Assert.AreEqual(dto, reader.ReadDateTimeOffset());
        Assert.AreEqual(ts, reader.ReadTimeSpan());
        Assert.AreEqual(0, reader.Remaining);
    }

    [TestMethod]
    public void Unknown_Fields_Can_Be_Skipped()
    {
        var writer = new BlazeDbBufferWriter();
        writer.WriteTag(1, BlazeDbWireType.VarInt);
        writer.WriteVarInt(42);
        writer.WriteTag(99, BlazeDbWireType.LengthDelimited);
        writer.WriteString("future field");
        writer.WriteTag(100, BlazeDbWireType.Fixed64);
        writer.WriteFixed64(123);
        writer.WriteTag(101, BlazeDbWireType.Fixed32);
        writer.WriteFixed32(7);
        writer.WriteTag(2, BlazeDbWireType.VarInt);
        writer.WriteVarInt(-7);

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        long? first = null;
        long? second = null;
        while (reader.Remaining > 0)
        {
            var (field, wireType) = reader.ReadTag();
            switch (field)
            {
                case 1:
                    first = reader.ReadVarInt();
                    break;
                case 2:
                    second = reader.ReadVarInt();
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        Assert.AreEqual(42, first);
        Assert.AreEqual(-7, second);
    }

    [TestMethod]
    public void Row_Serialization_Roundtrips_Through_Descriptor()
    {
        var person = new Person(7, "Ada", 36);
        var writer = new BlazeDbBufferWriter();
        PersonTable.Descriptor.RowWriter(writer, person);

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        var decoded = PersonTable.Descriptor.RowReader(ref reader);

        Assert.AreEqual(person, decoded);
    }

    [TestMethod]
    public void Truncated_Buffer_Throws_InvalidData()
    {
        var writer = new BlazeDbBufferWriter();
        writer.WriteString("hello");
        var bytes = writer.ToArray().AsSpan(0, 3);

        var thrown = false;
        try
        {
            var reader = new BlazeDbBufferReader(bytes);
            reader.ReadString();
        }
        catch (InvalidDataException)
        {
            thrown = true;
        }
        Assert.IsTrue(thrown);
    }

    [TestMethod]
    public void Fixed_Size_Blocks_With_The_Wrong_Length_Are_Reported_As_Invalid_Data()
    {
        // A Guid, decimal or DateTimeOffset is a 16-byte length-delimited block. A block of any
        // other size is corruption and must surface as the decode error every other path throws,
        // not as an argument error out of the value's constructor.
        var writer = new BlazeDbBufferWriter();
        writer.WriteBytes(new byte[7]);
        var bytes = writer.ToArray();

        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            var reader = new BlazeDbBufferReader(bytes);
            reader.ReadGuid();
        });
        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            var reader = new BlazeDbBufferReader(bytes);
            reader.ReadDecimal();
        });
        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            var reader = new BlazeDbBufferReader(bytes);
            reader.ReadDateTimeOffset();
        });
    }
}
