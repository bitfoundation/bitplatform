namespace Bit.BlazeDb.Samples.Lazy.Client.Data;

/// <summary>
/// The sample's one table. The source generator turns it into <c>TodoItem.Table</c> and <c>TodoItem.Indexes</c>, and
/// keeps the engine's types out of its fields: the layout every page shares creates one before Bit.BlazeDb.wasm is
/// downloaded and reads its properties. Only <c>Table</c> and <c>Indexes</c> need the assembly, so only the todos page
/// touches them.
/// </summary>
[BlazeDbTable("todos")]
public partial class TodoItem
{
    [BlazeDbKey] public Guid Id { get; set; }

    public string Title { get; set; } = "";

    [BlazeDbIndex] public bool Done { get; set; }

    [BlazeDbOrderedIndex] public DateTime CreatedAt { get; set; }
}
