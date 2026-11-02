using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Table shapes beyond a class with settable properties: positional records and constructor-bound
/// get-only properties, the date/time-only, Half and 128-bit integer types, set and interface-typed
/// collections, and index-only computed properties.
/// </summary>
[TestClass]
public class DataModelingTests
{
    private static T Roundtrip<TKey, T>(BlazeDbTableDescriptor<TKey, T> descriptor, T row)
        where TKey : notnull
    {
        var writer = new BlazeDbBufferWriter();
        descriptor.RowWriter(writer, row);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        return descriptor.RowReader(ref reader);
    }

    private static BlazeDbDatabaseOptions Options(IBlazeDbStorage? storage = null) => new BlazeDbDatabaseOptions
    {
        Storage = storage,
        FlushInterval = TimeSpan.FromHours(1),
    }.AddTable(Contact.Table).AddTable(Measurement.Table).AddTable(EmailAddress.Table);

    private static Measurement SampleMeasurement(int i) => new(new Guid(i, 0, 0, new byte[8]), new TimeOnly(i % 24, 30, 15, 250), (Half)(i * 0.5))
    {
        Big = Int128.MaxValue - i,
        Huge = i % 2 == 0 ? UInt128.MaxValue : null,
        Day = i % 3 == 0 ? null : new DateOnly(2026, 1, 1).AddDays(i),
        Bins = [i, i + 1, i + 2],
        Labels = i % 2 == 0 ? new HashSet<string> { "even", $"n{i}" } : null,
        Samples = [i, i / 3.0, double.NegativeInfinity],
        Links = [Guid.Empty, new Guid(i, 1, 1, new byte[8])],
    };

    [TestMethod]
    public void A_Positional_Record_Roundtrips_Through_Its_Constructor()
    {
        var contact = new Contact(7, "Ada", new DateOnly(1815, 12, 10)) { Nickname = "Countess" };

        var decoded = Roundtrip(Contact.Table, contact);

        Assert.AreEqual(contact, decoded);
        Assert.AreEqual(nameof(Contact.Id), Contact.Table.KeyMember);
        Assert.AreEqual(7, Contact.Table.KeySelector(decoded));
    }

    [TestMethod]
    public void Get_Only_Properties_Set_By_The_Constructor_Are_Stored()
    {
        var measurement = SampleMeasurement(4);

        var decoded = Roundtrip(Measurement.Table, measurement);

        Assert.AreEqual(measurement.Id, decoded.Id);
        Assert.AreEqual(measurement.At, decoded.At);
        Assert.AreEqual(measurement.Value, decoded.Value);
        Assert.AreEqual(measurement.Big, decoded.Big);
        Assert.AreEqual(measurement.Huge, decoded.Huge);
        Assert.AreEqual(measurement.Day, decoded.Day);
        CollectionAssert.AreEquivalent(measurement.Bins.ToList(), decoded.Bins.ToList());
        Assert.IsInstanceOfType<HashSet<int>>(decoded.Bins);
        CollectionAssert.AreEquivalent(measurement.Labels!.ToList(), decoded.Labels!.ToList());
        CollectionAssert.AreEqual(measurement.Samples.ToList(), decoded.Samples.ToList());
        CollectionAssert.AreEqual(measurement.Links.ToList(), decoded.Links.ToList());
    }

    [TestMethod]
    public void Absent_And_Null_Values_Of_The_New_Types_Roundtrip()
    {
        var odd = SampleMeasurement(3); // Huge, Day and Labels are null

        var decoded = Roundtrip(Measurement.Table, odd);

        Assert.IsNull(decoded.Huge);
        Assert.IsNull(decoded.Day);
        Assert.IsNull(decoded.Labels);
        Assert.IsNotNull(Roundtrip(Measurement.Table, new Measurement(Guid.Empty, default, default)).Bins, "A non-nullable set reads back empty, not null.");
    }

    [TestMethod]
    [DataRow(-1.5f)]
    [DataRow(65504f)]
    [DataRow(float.NaN)]
    [DataRow(float.NegativeInfinity)]
    public void Half_Keeps_Every_Bit(float value)
    {
        var measurement = new Measurement(Guid.NewGuid(), TimeOnly.MaxValue, (Half)value);

        var decoded = Roundtrip(Measurement.Table, measurement);

        Assert.AreEqual(BitConverter.HalfToUInt16Bits(measurement.Value), BitConverter.HalfToUInt16Bits(decoded.Value));
        Assert.AreEqual(TimeOnly.MaxValue, decoded.At);
    }

    [TestMethod]
    public void Corrupt_Values_Of_The_New_Types_Fail_As_Decode_Errors()
    {
        var writer = new BlazeDbBufferWriter();
        writer.WriteVarInt(DateOnly.MaxValue.DayNumber + 1L);
        writer.WriteVarInt(TimeOnly.MaxValue.Ticks + 1);
        writer.WriteVarUInt(ushort.MaxValue + 1UL);
        writer.WriteBytes(new byte[15]);
        var bytes = writer.ToArray();

        Assert.ThrowsExactly<InvalidDataException>(() => new BlazeDbBufferReader(bytes).ReadDateOnly());
        var reader = new BlazeDbBufferReader(bytes);
        reader.ReadVarInt();
        var afterDate = reader.Position;
        Assert.ThrowsExactly<InvalidDataException>(() => new BlazeDbBufferReader(bytes.AsSpan(afterDate)).ReadTimeOnly());
        reader.ReadVarInt();
        var afterTime = reader.Position;
        Assert.ThrowsExactly<InvalidDataException>(() => new BlazeDbBufferReader(bytes.AsSpan(afterTime)).ReadHalf());
        reader.ReadVarUInt();
        var afterHalf = reader.Position;
        Assert.ThrowsExactly<InvalidDataException>(() => new BlazeDbBufferReader(bytes.AsSpan(afterHalf)).ReadInt128());
    }

    [TestMethod]
    public async Task Records_And_Constructor_Bound_Rows_Survive_Recovery_And_Keep_Their_Indexes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            var contacts = db.GetTable(Contact.Table);
            contacts.InsertMany([
                new Contact(1, "Ada", new DateOnly(1815, 12, 10)),
                new Contact(2, "Alan", new DateOnly(1912, 6, 23)) { Nickname = "Prof" },
                new Contact(3, "Grace", new DateOnly(1906, 12, 9)),
            ]);
            contacts.Update(contacts.Get(3)! with { Name = "Grace Hopper" });
            await db.CheckpointAsync();
            db.GetTable(Measurement.Table).InsertMany(Enumerable.Range(1, 6).Select(SampleMeasurement));
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        var recovered = reopened.GetTable(Contact.Table);
        Assert.AreEqual(new Contact(2, "Alan", new DateOnly(1912, 6, 23)) { Nickname = "Prof" }, recovered.Get(2));
        Assert.AreEqual("Grace Hopper", recovered.Get(3)!.Name);
        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, recovered.OrderBy(Contact.Indexes.Birthday).Select(c => c.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, recovered.Lookup(Contact.Indexes.Initial, "A").Select(c => c.Id).ToArray());

        var measurements = reopened.GetTable(Measurement.Table);
        Assert.AreEqual(6, measurements.Count);
        Assert.AreEqual(1, measurements.CountBy(Measurement.Indexes.Big, Int128.MaxValue - 4));
        CollectionAssert.AreEqual(
            measurements.Scan().Where(m => m.Day is not null).OrderBy(m => m.Day).Select(m => m.Id).ToList(),
            measurements.Range(Measurement.Indexes.Day, new DateOnly(2000, 1, 1), DateOnly.MaxValue).Select(m => m.Id).ToList());
        Assert.AreEqual(0, reopened.VerifyIntegrity().Count);
    }

    [TestMethod]
    public void An_Index_Only_Property_Is_Indexed_But_Not_Stored()
    {
        var withoutDomain = Roundtrip(EmailAddress.Table, new EmailAddress { Id = 1, Address = "ada@example.com" });
        Assert.AreEqual("example.com", withoutDomain.Domain, "Computed again from the stored address.");

        // Only the key and the address are written: two fields.
        var writer = new BlazeDbBufferWriter();
        EmailAddress.Table.RowWriter(writer, new EmailAddress { Id = 1, Address = "ada@example.com" });
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        var fields = 0;
        while (reader.Remaining > 0)
        {
            reader.SkipField(reader.ReadTag().WireType);
            fields++;
        }
        Assert.AreEqual(2, fields);
    }

    [TestMethod]
    public async Task Index_Only_Properties_Serve_Lookups_Ranges_And_Compound_Indexes()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(Options());
        var emails = db.GetTable(EmailAddress.Table);
        emails.InsertMany([
            new EmailAddress { Id = 1, Address = "ada@b.org" },
            new EmailAddress { Id = 2, Address = "alan@a.org" },
            new EmailAddress { Id = 3, Address = "grace@b.org" },
        ]);

        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, emails.OrderBy(EmailAddress.Indexes.Domain).Select(e => e.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, emails.Lookup(EmailAddress.Indexes.DomainUser, ("b.org", "grace")).Select(e => e.Id).ToArray());

        // Changing the stored property the computed one depends on moves the entry, through a new row.
        emails.Update(new EmailAddress { Id = 3, Address = "grace@a.org" });
        CollectionAssert.AreEquivalent(new[] { 2, 3 }, emails.StartsWith(EmailAddress.Indexes.Domain, "a.").Select(e => e.Id).ToArray());
        Assert.AreEqual(0, db.VerifyIntegrity().Count);
    }
}
