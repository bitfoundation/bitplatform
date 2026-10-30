using System.Reflection;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// What an app that lazy loads Bit.BlazeDb on WebAssembly relies on from the generated code. Code that merely names a
/// table type before the assembly has loaded - <c>typeof(TodoItem)</c>, a <c>new TodoItem()</c> in a layout every page
/// shares - makes the runtime lay the type out, and on WebAssembly a field, base type or interface of a type it cannot
/// load yet fails the type for the rest of the session. So the generator leaves the engine's types out of all three,
/// on the table type and on its Indexes class, and keeps the descriptor and the index definitions in a nested holder
/// that is only laid out once they are used.
/// </summary>
[TestClass]
public class LazyLoadingTests
{
    private static readonly Assembly _engine = typeof(BlazeDbDatabase).Assembly;

    private static List<Type> TableTypes() =>
        [.. typeof(LazyLoadingTests).Assembly.GetTypes().Where(type => type.IsDefined(typeof(BlazeDbTableAttribute), inherit: false))];

    /// <summary>Whether <paramref name="type"/> is one of the engine's types, or is built from one.</summary>
    private static bool Mentions(Type type) =>
        type.Assembly == _engine
        || (type.HasElementType && Mentions(type.GetElementType()!))
        || (type.IsGenericType && type.GetGenericArguments().Any(Mentions));

    [TestMethod]
    public void Laying_out_a_table_type_needs_none_of_the_engine_s_types()
    {
        var tables = TableTypes();
        Assert.IsTrue(tables.Contains(typeof(TodoItem)) && tables.Contains(typeof(GeneratedNames)), "the test assembly's table types should be found");

        foreach (var table in tables)
        {
            foreach (var type in new[] { table, table.GetNestedType("Indexes") }.OfType<Type>())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    Assert.IsFalse(Mentions(field.FieldType), $"{type}.{field.Name} is a {field.FieldType}: laying {type.Name} out would load Bit.BlazeDb.");
                }

                Assert.IsFalse(type.BaseType is { } baseType && Mentions(baseType), $"{type} derives from {type.BaseType}.");

                foreach (var implemented in type.GetInterfaces())
                {
                    Assert.IsFalse(Mentions(implemented), $"{type} implements {implemented}.");
                }
            }
        }
    }

    [TestMethod]
    public void Table_and_Indexes_hand_out_the_descriptor_s_own_instances()
    {
        // The engine finds an index's store by its definition, so every read of a property has to return the one
        // instance the descriptor was built with - as the static readonly fields they replace did.
        foreach (var table in TableTypes())
        {
            // A type the generator rejects - one declared to test a diagnostic, say - has no Table to read.
            if (table.GetProperty("Table", BindingFlags.Public | BindingFlags.Static) is not { } tableProperty) continue;

            var descriptor = tableProperty.GetValue(null)!;
            Assert.AreSame(descriptor, tableProperty.GetValue(null), $"{table.Name}.Table");

            var described = ((System.Collections.IEnumerable)descriptor.GetType().GetProperty("Indexes")!.GetValue(descriptor)!).Cast<object>().ToList();
            var exposed = table.GetNestedType("Indexes")?.GetProperties(BindingFlags.Public | BindingFlags.Static) ?? [];

            Assert.HasCount(described.Count, exposed, $"{table.Name}.Indexes");
            foreach (var property in exposed)
            {
                var definition = property.GetValue(null);
                Assert.AreSame(definition, property.GetValue(null), $"{table.Name}.Indexes.{property.Name}");
                Assert.IsTrue(described.Any(index => ReferenceEquals(index, definition)), $"{table.Name}.Indexes.{property.Name} is not the descriptor's.");
            }
        }
    }

    [TestMethod]
    public async Task An_index_may_be_named_after_the_generated_members()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(GeneratedNames.Table));
        var rows = db.GetTable(GeneratedNames.Table);

        rows.Insert(new GeneratedNames { Id = 1, Kind = "a", Rank = 2, Flag = true, Priority = 5 });
        rows.Insert(new GeneratedNames { Id = 2, Kind = "b", Rank = 1, Flag = false, Priority = 7 });

        Assert.AreEqual(1, rows.Lookup(GeneratedNames.Indexes.Descriptor, "a").Single().Id);
        CollectionAssert.AreEqual(new[] { 2, 1 }, rows.OrderBy(GeneratedNames.Indexes.Table).Select(row => row.Id).ToArray());
        Assert.AreEqual(1, rows.Lookup(GeneratedNames.Indexes.__BlazeTable, true).Single().Id);
        Assert.AreEqual(2, rows.Lookup(GeneratedNames.Indexes.@event, 7).Single().Id);
    }
}

/// <summary>
/// Indexes named after what the generator emits next to them, none of which may collide with it, and after a keyword,
/// which the generator has to escape.
/// </summary>
[BlazeDbTable("generated-names")]
public partial class GeneratedNames
{
    [BlazeDbKey] public int Id { get; set; }

    [BlazeDbIndex(Name = "Descriptor")] public string Kind { get; set; } = "";

    [BlazeDbOrderedIndex(Name = "Table")] public int Rank { get; set; }

    [BlazeDbIndex(Name = "__BlazeTable")] public bool Flag { get; set; }

    [BlazeDbIndex(Name = "event")] public int Priority { get; set; }
}
