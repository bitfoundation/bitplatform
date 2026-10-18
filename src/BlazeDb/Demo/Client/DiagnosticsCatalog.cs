namespace Bit.BlazeDb.Demo.Client;

/// <summary>One compile-time diagnostic the BlazeDb source generator can report, as the docs explain it.</summary>
/// <param name="Code">The diagnostic id, e.g. <c>BLZ001</c>.</param>
/// <param name="Title">What the diagnostic means, in a few words.</param>
/// <param name="IsError">True for an error, false for a warning.</param>
/// <param name="Trigger">The shortest piece of code that triggers it, for the schema page's table.</param>
/// <param name="Cause">Why the generator rejects the shape (HTML).</param>
/// <param name="Bad">A table type the generator rejects with this diagnostic.</param>
/// <param name="Good">The same table type, fixed.</param>
public sealed record BlazeDbDiagnosticDoc(
    string Code,
    string Title,
    bool IsError,
    string Trigger,
    string Cause,
    string Bad,
    string Good);

/// <summary>
/// Every diagnostic of the BlazeDb source generator, with a rejected and a fixed sample. The schema page
/// renders it and the demo's MCP server hands the same entries out, so the two cannot disagree.
/// </summary>
public static class DiagnosticsCatalog
{
    /// <summary>Every diagnostic, in id order.</summary>
    public static IReadOnlyList<BlazeDbDiagnosticDoc> All { get; } =
    [
        new("BLZ001", "Table type must be partial", true,
            "[BlazeDbTable] class Foo",
            "The generator adds the <code>Table</code> descriptor and the <code>Indexes</code> class through a second partial declaration, so the type has to be <code>partial</code>.",
            """
            [BlazeDbTable("todos")]
            public class TodoItem   // BLZ001
            {
                [BlazeDbKey] public Guid Id { get; set; }
            }
            """,
            """
            [BlazeDbTable("todos")]
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
            }
            """),

        new("BLZ002", "Exactly one [BlazeDbKey] property required", true,
            "no [BlazeDbKey], or two of them",
            "Every row is stored and found by one primary key, so a table type needs exactly one serializable property marked <code>[BlazeDbKey]</code>. A composite key is one property too: combine the parts into a string, or give the type a surrogate key and put a unique compound index over the parts.",
            """
            [BlazeDbTable("orders")]
            public partial class Order   // BLZ002 - no key
            {
                public int CustomerId { get; set; }
                public int Number { get; set; }
            }
            """,
            """
            [BlazeDbTable("orders")]
            [BlazeDbCompoundIndex("CustomerNumber", nameof(CustomerId), nameof(Number), Unique = true)]
            public partial class Order
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public int CustomerId { get; set; }
                public int Number { get; set; }
            }
            """),

        new("BLZ003", "Unsupported property type", true,
            "Dictionary<string, string> Map { get; set; }",
            "The binary format encodes scalars - <code>bool</code>, the integer types, <code>char</code>, <code>float</code>, <code>double</code>, <code>decimal</code>, <code>string</code>, <code>byte[]</code>, enums, <code>Guid</code>, <code>DateTime</code>, <code>DateTimeOffset</code>, <code>TimeSpan</code> and their nullable forms - plus <code>List&lt;T&gt;</code> and <code>T[]</code> of a non-nullable scalar. Anything else has no encoding: mark it <code>[BlazeDbIgnore]</code>, or store it in a shape the format has.",
            """
            [BlazeDbTable("profiles")]
            public partial class Profile
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public Dictionary<string, string> Settings { get; set; } = [];   // BLZ003
            }
            """,
            """
            [BlazeDbTable("profiles")]
            public partial class Profile
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public string SettingsJson { get; set; } = "{}";

                [BlazeDbIgnore]
                public Dictionary<string, string> Settings
                {
                    get => JsonSerializer.Deserialize<Dictionary<string, string>>(SettingsJson)!;
                    set => SettingsJson = JsonSerializer.Serialize(value);
                }
            }
            """),

        new("BLZ004", "Needs a parameterless constructor", true,
            "record Foo(int Id) - positional only",
            "The generated reader creates a row with <code>new T { ... }</code> and sets each property it decodes, so a class needs a parameterless constructor and settable properties. A positional record has neither; write the record with ordinary properties instead.",
            """
            [BlazeDbTable("tags")]
            public partial record Tag([property: BlazeDbKey] string Slug, string Label);   // BLZ004
            """,
            """
            [BlazeDbTable("tags")]
            public partial record Tag
            {
                [BlazeDbKey] public string Slug { get; set; } = "";
                public string Label { get; set; } = "";
            }
            """),

        new("BLZ005", "Duplicate field number", true,
            "two properties with [BlazeDbField(1)]",
            "A field number is how a property is found in the stored bytes, so two properties cannot share one: the second would decode the first one's value.",
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                [BlazeDbField(2)] public string Name { get; set; } = "";
                [BlazeDbField(2)] public string Email { get; set; } = "";   // BLZ005
            }
            """,
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                [BlazeDbField(2)] public string Name { get; set; } = "";
                [BlazeDbField(3)] public string Email { get; set; } = "";
            }
            """),

        new("BLZ006", "Unsupported key type", true,
            "[BlazeDbKey] string? Id { get; set; }",
            "The key is the row's identity in the primary-key dictionary and in every log record, so it has to be a non-nullable scalar. A nullable key, a collection or a <code>byte[]</code> (which compares by reference) cannot be one.",
            """
            [BlazeDbTable("files")]
            public partial class StoredFile
            {
                [BlazeDbKey] public string? Path { get; set; }   // BLZ006
            }
            """,
            """
            [BlazeDbTable("files")]
            public partial class StoredFile
            {
                [BlazeDbKey] public string Path { get; set; } = "";
            }
            """),

        new("BLZ007", "Generic or nested table type", true,
            "[BlazeDbTable] partial class Foo<T>",
            "The descriptor is a static member of the type, so the type has to be one closed, top-level type the generator can name. A generic type has one table per type argument and a nested one is reached through its container; declare a top-level type per table.",
            """
            public partial class Store
            {
                [BlazeDbTable("items")]
                public partial class Item   // BLZ007 - nested
                {
                    [BlazeDbKey] public int Id { get; set; }
                }
            }
            """,
            """
            [BlazeDbTable("items")]
            public partial class StoreItem
            {
                [BlazeDbKey] public int Id { get; set; }
            }
            """),

        new("BLZ008", "Compound index names an unknown property", true,
            "[BlazeDbCompoundIndex(\"X\", \"Nope\")]",
            "Every name a compound index lists must be a serializable property of the type - use <code>nameof</code>, so a rename cannot leave the index pointing at nothing.",
            """
            [BlazeDbTable("todos")]
            [BlazeDbCompoundIndex("CategoryDone", "Categry", "Done")]   // BLZ008 - typo
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public string? Category { get; set; }
                public bool Done { get; set; }
            }
            """,
            """
            [BlazeDbTable("todos")]
            [BlazeDbCompoundIndex("CategoryDone", nameof(Category), nameof(Done))]
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public string? Category { get; set; }
                public bool Done { get; set; }
            }
            """),

        new("BLZ009", "Compound index needs at least two properties", true,
            "[BlazeDbCompoundIndex(\"X\", \"Only\")]",
            "A compound index keys on a tuple of its properties; over one property it is an ordinary index, which <code>[BlazeDbIndex]</code> or <code>[BlazeDbOrderedIndex]</code> on the property declares.",
            """
            [BlazeDbTable("todos")]
            [BlazeDbCompoundIndex("ByDone", nameof(Done))]   // BLZ009
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public bool Done { get; set; }
            }
            """,
            """
            [BlazeDbTable("todos")]
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                [BlazeDbIndex] public bool Done { get; set; }
            }
            """),

        new("BLZ010", "Duplicate index name", true,
            "[BlazeDbIndex(Name = \"A\")] twice",
            "Every index becomes a member of the generated <code>Indexes</code> class under its name, so two indexes cannot share one. An index without a <code>Name</code> is named after its property - which is what collides when a property has both a hash and an ordered index.",
            """
            [BlazeDbTable("events")]
            public partial class Event
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex, BlazeDbOrderedIndex] public DateTime At { get; set; }   // BLZ010 - both named "At"
            }
            """,
            """
            [BlazeDbTable("events")]
            public partial class Event
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex(Name = "AtExact"), BlazeDbOrderedIndex] public DateTime At { get; set; }
            }
            """),

        new("BLZ011", "Index name is not a valid identifier", true,
            "[BlazeDbIndex(Name = \"by name\")]",
            "An index name is emitted as a member of the generated <code>Indexes</code> class, so it has to be a C# identifier - and not <code>Indexes</code> itself, which a member of that class cannot be called.",
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex(Name = "by name")] public string Name { get; set; } = "";   // BLZ011
            }
            """,
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex(Name = "ByName")] public string Name { get; set; } = "";
            }
            """),

        new("BLZ012", "Table type inherits properties", true,
            "partial class Foo : HasProps",
            "The generator serializes a table type's own properties only, so a property inherited from a base class would silently never be persisted. Declare it on the table type, or mark the base property <code>[BlazeDbIgnore]</code> when leaving it out is intended.",
            """
            public abstract class Entity
            {
                public DateTime CreatedAt { get; set; }
            }

            [BlazeDbTable("notes")]
            public partial class Note : Entity   // BLZ012 - CreatedAt would never be stored
            {
                [BlazeDbKey] public Guid Id { get; set; }
            }
            """,
            """
            [BlazeDbTable("notes")]
            public partial class Note
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }
            """),

        new("BLZ013", "Mixes explicit and automatic field numbers", false,
            "[BlazeDbField(1)] on some properties only",
            "A property without <code>[BlazeDbField]</code> is numbered by declaration order, skipping the numbers already taken. Next to explicit numbers that automatic number moves whenever a property is added, removed or numbered explicitly - and rows already on disk then decode into the wrong properties without an error. Number every property once the type persists real data.",
            """
            [BlazeDbTable("people")]
            public partial class Person   // BLZ013
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Email { get; set; } = "";
            }
            """,
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                [BlazeDbField(2)] public string Name { get; set; } = "";
                [BlazeDbField(3)] public string Email { get; set; } = "";
            }
            """),

        new("BLZ014", "Field number out of range", true,
            "[BlazeDbField(0)]",
            "A field number shares a varint tag with the three-bit wire type, exactly as in protobuf, so it must be between 1 and 536,870,911.",
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                [BlazeDbField(0)] public string Name { get; set; } = "";   // BLZ014
            }
            """,
            """
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey, BlazeDbField(1)] public int Id { get; set; }
                [BlazeDbField(2)] public string Name { get; set; } = "";
            }
            """),

        new("BLZ015", "Property cannot be indexed", true,
            "[BlazeDbIndex] List<string> Tags { get; set; }",
            "An index keys on one value with equality (hash) or ordering (ordered). A collection has no single value to key on, and a <code>byte[]</code> compares by reference - an index over either would never find its row again or throw on the first insert. Collections and <code>byte[]</code> can be stored, just not indexed: index a scalar derived from them, or model the collection as rows of a table of its own.",
            """
            [BlazeDbTable("posts")]
            public partial class Post
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex] public List<string> Tags { get; set; } = [];   // BLZ015
            }
            """,
            """
            [BlazeDbTable("post-tags")]
            [BlazeDbCompoundIndex("PostTag", nameof(PostId), nameof(Tag), Unique = true)]
            public partial class PostTag
            {
                [BlazeDbKey] public Guid Id { get; set; }
                [BlazeDbIndex] public int PostId { get; set; }
                [BlazeDbIndex] public string Tag { get; set; } = "";
            }
            """),

        new("BLZ016", "Compound index lists a property twice", true,
            "[BlazeDbCompoundIndex(\"X\", \"A\", \"A\")]",
            "A compound index keys on a value tuple whose elements are named after its properties, and a tuple cannot name two elements the same. Listing a property twice adds nothing to the key anyway.",
            """
            [BlazeDbTable("todos")]
            [BlazeDbCompoundIndex("DoneDone", nameof(Done), nameof(Done))]   // BLZ016
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                public bool Done { get; set; }
            }
            """,
            """
            [BlazeDbTable("todos")]
            public partial class TodoItem
            {
                [BlazeDbKey] public Guid Id { get; set; }
                [BlazeDbIndex] public bool Done { get; set; }
            }
            """),
    ];

    /// <summary>The entry for one id, or null.</summary>
    public static BlazeDbDiagnosticDoc? Find(string code) =>
        All.FirstOrDefault(entry => string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase));
}
