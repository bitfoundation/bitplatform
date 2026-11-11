using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Tests.@event;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class GeneratorEdgeCaseTests
{
    private static T Roundtrip<T>(BlazeDbTableDescriptor<int, T> table, T row)
    {
        var writer = new BlazeDbBufferWriter();
        table.RowWriter(writer, row);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        return table.RowReader(ref reader);
    }

    private static Task<BlazeDbDatabase> OpenPersistentAsync() =>
        BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = new Bit.BlazeDb.Storage.BlazeDbInMemoryStorage(),
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(KeywordRow.Table)).AsTask();

    [TestMethod]
    public async Task Keyword_Names_And_Tuple_Member_Names_Are_Stored_And_Indexed()
    {
        var row = new KeywordRow { @int = 1, @class = "a", @event = 7, Item1 = "x", Item2 = 2, Rest = "r", Maybe = null, Blob = [1, 2] };
        ((IHasTenant)row).Tenant = "not stored";

        var decoded = Roundtrip(KeywordRow.Table, row);
        Assert.AreEqual(1, decoded.@int);
        Assert.AreEqual("a", decoded.@class);
        Assert.AreEqual(7, decoded.@event);
        Assert.AreEqual("x", decoded.Item1);
        Assert.AreEqual(2, decoded.Item2);
        Assert.AreEqual("r", decoded.Rest);
        // The explicit interface implementation is not a column.
        Assert.AreEqual("", ((IHasTenant)decoded).Tenant);

        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(KeywordRow.Table), TestContext.CancellationToken);
        var rows = db.GetTable(KeywordRow.Table);
        rows.Insert(row);
        rows.Insert(new KeywordRow { @int = 2, @class = "b", @event = 3, Item1 = "y", Item2 = 1, Rest = "r" });

        Assert.AreEqual(1, rows.Lookup(KeywordRow.Indexes.@class, "a").Single().@int);
        Assert.AreEqual(1, rows.Lookup(KeywordRow.Indexes.ByItems, (2, "x")).Single().@int);
        Assert.AreSequenceEqual(new[] { 1, 2 }, rows.OrderBy(KeywordRow.Indexes.ByRest).Select(r => r.@int).ToArray());
        Assert.AreSequenceEqual(new[] { 2, 1 }, rows.OrderBy(KeywordRow.Indexes.@event).Select(r => r.@int).ToArray());
    }

    [TestMethod]
    public void A_Null_In_A_Non_Nullable_String_Is_Refused_Rather_Than_Read_Back_Empty()
    {
        var ex = Assert.ThrowsExactly<BlazeDbException>(() =>
            KeywordRow.Table.RowWriter(new BlazeDbBufferWriter(), new KeywordRow { @int = 1, @class = null! }));
        Assert.Contains("'class'", ex.Message);
        Assert.Contains("'keyword_rows'", ex.Message);

        Assert.ThrowsExactly<BlazeDbException>(() =>
            KeywordRow.Table.RowWriter(new BlazeDbBufferWriter(), new KeywordRow { @int = 1, Blob = null! }));

        // A nullable one stores its null as before.
        Assert.IsNull(Roundtrip(KeywordRow.Table, new KeywordRow { @int = 1, Maybe = null }).Maybe);
    }

    [TestMethod]
    public async Task A_Persisted_Write_With_A_Null_In_A_Non_Nullable_String_Fails_And_Leaves_Nothing_Behind()
    {
        await using var db = await OpenPersistentAsync();
        var rows = db.GetTable(KeywordRow.Table);

        Assert.ThrowsExactly<BlazeDbException>(() => rows.Insert(new KeywordRow { @int = 1, @class = null! }));
        Assert.AreEqual(0, rows.Count);
        Assert.IsEmpty(db.VerifyIntegrity());
    }

    [TestMethod]
    public void A_Struct_Keeps_Storing_A_Null_String_As_Empty()
    {
        var writer = new BlazeDbBufferWriter();
        StructRow.Table.RowWriter(writer, new StructRow { Id = 1 });
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.AreEqual("", StructRow.Table.RowReader(ref reader).Name);
    }

    [TestMethod]
    public async Task A_Row_Changed_In_Place_And_Never_Saved_Neither_Stops_Checkpoints_Nor_Reaches_Them()
    {
        var storage = new Bit.BlazeDb.Storage.BlazeDbInMemoryStorage();
        var options = new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(KeywordRow.Table);
        await using (var db = await BlazeDbDatabase.OpenAsync(options, TestContext.CancellationToken))
        {
            var row = new KeywordRow { @int = 1, @class = "a", Item1 = "x" };
            db.GetTable(KeywordRow.Table).Insert(row);

            // What an EF Core entity whose save was refused leaves behind: the table's own row object, changed in place.
            row.Item1 = null!;
            row.Rest = "b" + (char)0xD800;
            await db.CheckpointAsync(TestContext.CancellationToken);
            using var export = new MemoryStream();
            await db.ExportAsync(export, TestContext.CancellationToken);
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(
            new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(KeywordRow.Table), TestContext.CancellationToken);
        // Checkpoints and exports are built from what the log committed, so the edits that were never written are
        // not in them: the row comes back as it was inserted.
        var stored = reopened.GetTable(KeywordRow.Table).Get(1)!;
        Assert.AreEqual("x", stored.Item1);
        Assert.AreEqual("", stored.Rest);
    }

    [TestMethod]
    public async Task A_String_With_An_Unpaired_Surrogate_Is_Refused_Rather_Than_Stored_Altered()
    {
        var highAlone = "a" + (char)0xD800;
        var lowAlone = (char)0xDC00 + "b";
        var pair = "a" + char.ConvertFromUtf32(0x1F600);

        Assert.ThrowsExactly<BlazeDbException>(() => new BlazeDbBufferWriter().WriteString(highAlone));
        Assert.ThrowsExactly<BlazeDbException>(() => new BlazeDbBufferWriter().WriteString(lowAlone));
        // A proper pair is text like any other.
        var writer = new BlazeDbBufferWriter();
        writer.WriteString(pair);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.AreEqual(pair, reader.ReadString());

        // Two values that differ only in the broken character would have become one on the next open.
        await using var db = await OpenPersistentAsync();
        var rows = db.GetTable(KeywordRow.Table);
        var refused = Assert.ThrowsExactly<BlazeDbException>(() => rows.Insert(new KeywordRow { @int = 1, @class = highAlone }));
        Assert.AreEqual(0, rows.Count);
        // The message names the column, as a refused null does, and where in the value the broken character is.
        Assert.Contains("'class'", refused.Message);
        Assert.Contains("'keyword_rows'", refused.Message);
        Assert.Contains("index 1", refused.Message);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
