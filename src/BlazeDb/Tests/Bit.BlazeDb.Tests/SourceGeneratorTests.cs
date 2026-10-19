using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class SourceGeneratorTests
{
    private static TodoItem SampleTodo() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Write tests",
        Notes = "some notes",
        Done = true,
        CreatedAt = new DateTime(2026, 8, 17, 10, 30, 0, DateTimeKind.Utc),
        Priority = Priority.High,
        Rating = 4,
        Cost = 12.34m,
        Weight = 1.75,
        Duration = TimeSpan.FromMinutes(90),
        DueAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.FromHours(3.5)),
        Tags = ["work", "urgent"],
        Scores = [1, 2, 3],
        Payload = [0xDE, 0xAD, 0xBE, 0xEF],
        Transient = "should not survive",
    };

    private static TodoItem Roundtrip(TodoItem item)
    {
        var writer = new BlazeDbBufferWriter();
        TodoItem.Table.RowWriter(writer, item);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        return TodoItem.Table.RowReader(ref reader);
    }

    [TestMethod]
    public void Generated_Descriptor_Has_Table_Name_And_Key_Selector()
    {
        Assert.AreEqual("todos", TodoItem.Table.Name);
        var item = SampleTodo();
        Assert.AreEqual(item.Id, TodoItem.Table.KeySelector(item));
        Assert.AreEqual(nameof(TodoItem.Id), TodoItem.Table.KeyMember);
        Assert.AreEqual(nameof(Setting.Name), Setting.Table.KeyMember);
    }

    [TestMethod]
    public void Generated_Serializer_Roundtrips_All_Supported_Types()
    {
        var item = SampleTodo();
        var decoded = Roundtrip(item);

        Assert.AreEqual(item.Id, decoded.Id);
        Assert.AreEqual(item.Title, decoded.Title);
        Assert.AreEqual(item.Notes, decoded.Notes);
        Assert.AreEqual(item.Done, decoded.Done);
        Assert.AreEqual(item.CreatedAt, decoded.CreatedAt);
        Assert.AreEqual(item.Priority, decoded.Priority);
        Assert.AreEqual(item.Rating, decoded.Rating);
        Assert.AreEqual(item.Cost, decoded.Cost);
        Assert.AreEqual(item.Weight, decoded.Weight);
        Assert.AreEqual(item.Duration, decoded.Duration);
        Assert.AreEqual(item.DueAt, decoded.DueAt);
        Assert.AreSequenceEqual(item.Tags, decoded.Tags);
        Assert.AreSequenceEqual(item.Scores, decoded.Scores);
        Assert.AreSequenceEqual(item.Payload, decoded.Payload);
    }

    [TestMethod]
    public void Ignored_Property_Is_Not_Serialized()
    {
        var decoded = Roundtrip(SampleTodo());
        Assert.AreEqual("not persisted", decoded.Transient);
    }

    [TestMethod]
    public void Null_Optionals_And_Empty_Collections_Roundtrip()
    {
        var item = new TodoItem { Id = Guid.NewGuid(), Title = "minimal" };
        var decoded = Roundtrip(item);

        Assert.IsNull(decoded.Notes);
        Assert.IsNull(decoded.Rating);
        Assert.IsNull(decoded.DueAt);
        Assert.IsEmpty(decoded.Tags);
        Assert.IsEmpty(decoded.Scores);
        Assert.IsEmpty(decoded.Payload);
    }

    [TestMethod]
    public void Generated_Key_Serializer_Roundtrips()
    {
        var id = Guid.NewGuid();
        var writer = new BlazeDbBufferWriter();
        TodoItem.Table.KeyWriter(writer, id);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        Assert.AreEqual(id, TodoItem.Table.KeyReader(ref reader));
    }

    [TestMethod]
    public void Generated_Index_Definitions_Are_Wired_Into_Descriptor()
    {
        Assert.AreEqual(3, TodoItem.Table.Indexes.Count);
        Assert.Contains(TodoItem.Indexes.Done, TodoItem.Table.Indexes);
        Assert.Contains(TodoItem.Indexes.Category, TodoItem.Table.Indexes);
        Assert.Contains(TodoItem.Indexes.CreatedAt, TodoItem.Table.Indexes);
        Assert.IsExactInstanceOfType<BlazeDbHashIndexDefinition<TodoItem, bool>>(TodoItem.Indexes.Done);
        Assert.IsExactInstanceOfType<BlazeDbHashIndexDefinition<TodoItem, string?>>(TodoItem.Indexes.Category);
        Assert.IsExactInstanceOfType<BlazeDbOrderedIndexDefinition<TodoItem, DateTime>>(TodoItem.Indexes.CreatedAt);
    }

    [TestMethod]
    public async Task Generated_Table_Works_End_To_End_In_A_Database()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(TodoItem.Table));
        var todos = db.GetTable(TodoItem.Table);
        var item = SampleTodo();

        todos.Insert(item);

        Assert.AreSame(item, todos.Get(item.Id));
    }

    [TestMethod]
    public void Explicit_Field_Numbers_And_String_Key_Work()
    {
        Assert.AreEqual("Setting", Setting.Table.Name);

        var setting = new Setting { Name = "theme", Value = "dark" };
        var writer = new BlazeDbBufferWriter();
        Setting.Table.RowWriter(writer, setting);

        // Verify the explicit field numbers are on the wire.
        var raw = new BlazeDbBufferReader(writer.WrittenSpan);
        var (field1, _) = raw.ReadTag();
        Assert.AreEqual(5, field1);
        raw.SkipField(BlazeDbWireType.LengthDelimited);
        var (field2, _) = raw.ReadTag();
        Assert.AreEqual(9, field2);

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        var decoded = Setting.Table.RowReader(ref reader);
        Assert.AreEqual("theme", decoded.Name);
        Assert.AreEqual("dark", decoded.Value);
    }

    [TestMethod]
    public void Old_Readers_Skip_Fields_Added_By_Newer_Writers()
    {
        // Simulate a newer schema writing an extra unknown field before known data.
        var writer = new BlazeDbBufferWriter();
        writer.WriteTag(200, BlazeDbWireType.LengthDelimited);
        writer.WriteString("from-the-future");
        var setting = new Setting { Name = "a", Value = "b" };
        Setting.Table.RowWriter(writer, setting);

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        var decoded = Setting.Table.RowReader(ref reader);
        Assert.AreEqual("a", decoded.Name);
        Assert.AreEqual("b", decoded.Value);
    }

    [TestMethod]
    public void A_Known_Field_Written_With_Another_Wire_Type_Is_Skipped_Not_Misread()
    {
        // Simulate a schema in which Setting.Value (field 9, a string) used to be an integer. The
        // reader must not try to decode a varint as a length-prefixed string - which would either
        // throw or desynchronize every field after it - but treat it as unknown and move on.
        var writer = new BlazeDbBufferWriter();
        writer.WriteTag(9, BlazeDbWireType.VarInt);
        writer.WriteVarInt(12345);
        writer.WriteTag(5, BlazeDbWireType.LengthDelimited);
        writer.WriteString("name-after-the-changed-field");

        var reader = new BlazeDbBufferReader(writer.WrittenSpan);
        var decoded = Setting.Table.RowReader(ref reader);

        Assert.AreEqual("name-after-the-changed-field", decoded.Name);
        Assert.AreEqual("", decoded.Value);
    }
}
