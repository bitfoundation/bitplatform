using System.Reflection;
using Bit.BlazeDb.Demo.Server.Dtos;
using Bit.BlazeDb.Demo.Server.Services;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Tests.Mcp.Models;

namespace Bit.BlazeDb.Tests.Mcp;

/// <summary>
/// InspectBlazeDbTable held to the generator it claims to speak for. The models in Models\ are compiled
/// into this assembly by the real generator, so their descriptors are what a build produces; the same
/// file is handed to the inspector as text, and the two have to agree - on the table name, the key, the
/// indexes, and on the field number and wire type of every property, read off a row the generated writer
/// actually encoded. The diagnostics catalog is put to the generator the same way: every rejected sample
/// has to be rejected with its own id, and every fix has to generate.
/// </summary>
[TestClass]
public class McpInspectorTests
{
    private static readonly Lazy<string> _modelsSource = new(() =>
    {
        using var stream = typeof(McpInspectorTests).Assembly.GetManifestResourceStream("InspectedModels.cs")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    });

    internal static Task<BlazeDbTableInspectionDto> InspectAsync(string source, bool includeGeneratedCode = false)
        => McpCall.StructuredAsync<BlazeDbTableInspectionDto>("InspectBlazeDbTable", new() { ["source"] = source, ["includeGeneratedCode"] = includeGeneratedCode });

    [TestMethod]
    public async Task The_report_agrees_with_the_descriptors_the_generator_built()
    {
        var report = await InspectAsync(_modelsSource.Value);

        Assert.IsTrue(report.IsValid, string.Join(Environment.NewLine, report.Diagnostics.Select(d => $"{d.Id}: {d.Message}")));
        Assert.HasCount(2, report.Tables);

        AssertAgrees(report.Tables.Single(table => table.TypeName == nameof(InspectedItem)), InspectedItem.Table, FullItem());
        AssertAgrees(report.Tables.Single(table => table.TypeName == nameof(NumberedItem)), NumberedItem.Table, new NumberedItem { Id = 1, Name = "n", Score = 2 });
    }

    [TestMethod]
    public async Task Explicit_field_numbers_are_reported_as_pinned()
    {
        var report = await InspectAsync(_modelsSource.Value);

        var numbered = report.Tables.Single(table => table.TypeName == nameof(NumberedItem));
        var item = report.Tables.Single(table => table.TypeName == nameof(InspectedItem));

        CollectionAssert.AreEqual(new[] { 3, 7, 10 }, numbered.Fields.Select(field => field.FieldNumber).ToArray());
        Assert.IsTrue(numbered.Fields.All(field => field.Explicit));
        Assert.IsTrue(item.Fields.All(field => field.Explicit is false));

        // A type with only automatic numbers is fine to build, and worth a word before it stores data.
        Assert.IsTrue(report.Notes.Any(note => note.Contains($"'{nameof(InspectedItem)}' follows declaration order", StringComparison.Ordinal)));
        Assert.IsFalse(report.Notes.Any(note => note.Contains($"'{nameof(NumberedItem)}'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Indexes_are_reported_with_their_kind_key_type_and_reference()
    {
        var report = await InspectAsync(_modelsSource.Value);
        var item = report.Tables.Single(table => table.TypeName == nameof(InspectedItem));

        var ownerCreated = item.Indexes.Single(index => index.Name == "OwnerCreated");

        Assert.AreEqual("Ordered", ownerCreated.Kind);
        Assert.IsTrue(ownerCreated.Unique);
        Assert.AreEqual("(string? Owner, DateTime CreatedAt)", ownerCreated.KeyType);
        Assert.AreEqual("InspectedItem.Indexes.OwnerCreated", ownerCreated.Reference);

        var byMood = item.Indexes.Single(index => index.Name == "ByMood");

        Assert.AreEqual("Hash", byMood.Kind);
        Assert.AreEqual("Mood", byMood.KeyType);
        CollectionAssert.AreEqual(new[] { "Mood" }, byMood.Members);

        CollectionAssert.Contains(item.GeneratedMembers, "InspectedItem.Table : BlazeDbTableDescriptor<Guid, InspectedItem>");
        CollectionAssert.Contains(item.GeneratedMembers, "InspectedItem.Indexes.ByMood : BlazeDbHashIndexDefinition<InspectedItem, Mood>");
    }

    [TestMethod]
    public async Task The_generated_code_is_returned_only_when_asked_for()
    {
        var without = await InspectAsync(_modelsSource.Value);
        var with = await InspectAsync(_modelsSource.Value, includeGeneratedCode: true);

        Assert.IsNull(without.GeneratedCode);
        Assert.IsNotNull(with.GeneratedCode);
        Assert.Contains("partial class InspectedItem", with.GeneratedCode);
        Assert.Contains("__BlazeReadRow", with.GeneratedCode);
    }

    [TestMethod]
    public async Task Every_rejected_sample_of_the_catalog_is_rejected_with_its_own_id()
    {
        var misses = new List<string>();

        foreach (var entry in BlazeDbDiagnosticsReference.Entries)
        {
            Assert.IsNotNull(entry.Doc, $"{entry.Id} has no entry in the diagnostics catalog.");

            var report = await InspectAsync(entry.Doc.Bad);

            if (report.Diagnostics.Any(diagnostic => diagnostic.Id == entry.Id && diagnostic.Source == "generator")) continue;

            misses.Add($"{entry.Id}: the rejected sample reported {(report.Diagnostics.Length == 0 ? "nothing" : string.Join(", ", report.Diagnostics.Select(d => d.Id)))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task Every_fix_of_the_catalog_generates()
    {
        var misses = new List<string>();

        foreach (var entry in BlazeDbDiagnosticsReference.Entries)
        {
            var report = await InspectAsync(entry.Doc!.Good);

            if (report.IsValid && report.Tables.Length > 0 && report.Tables.All(table => table.Generated) && report.Diagnostics.Length == 0) continue;

            misses.Add($"{entry.Id}: the fixed sample reported {string.Join(", ", report.Diagnostics.Select(d => $"{d.Id} {d.Message}"))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_diagnostic_points_at_the_line_it_is_about()
    {
        var report = await InspectAsync("""
            [BlazeDbTable("people")]
            public partial class Person
            {
                [BlazeDbKey] public int Id { get; set; }
                [BlazeDbIndex] public List<string> Tags { get; set; } = [];
            }
            """);

        var diagnostic = report.Diagnostics.Single(d => d.Id == "BLZ015");

        Assert.AreEqual(5, diagnostic.Line);
        Assert.IsFalse(report.IsValid);
    }

    [TestMethod]
    public async Task Source_without_a_table_says_what_a_table_looks_like()
    {
        var report = await InspectAsync("public class Plain { public int Id { get; set; } }");

        Assert.IsEmpty(report.Tables);
        Assert.IsTrue(report.Notes.Single().Contains("No type marked [BlazeDbTable]", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Broken_csharp_around_a_valid_table_is_reported_as_the_compiler_s()
    {
        var report = await InspectAsync("""
            [BlazeDbTable("t")]
            public partial class T
            {
                [BlazeDbKey] public int Id { get; set; }
                public Missing Other { get; set; }
            }
            """);

        Assert.IsFalse(report.IsValid);
        Assert.IsTrue(report.Diagnostics.Any(d => d.Source == "compiler" && d.Id == "CS0246"));
    }

    [TestMethod]
    public async Task An_oversized_source_is_cut_and_says_so()
    {
        var source = _modelsSource.Value + new string(' ', BlazeDbTableInspector.MaxSourceLength);

        var report = await InspectAsync(source);

        Assert.IsTrue(report.Notes.Any(note => note.StartsWith("INCOMPLETE ANALYSIS", StringComparison.Ordinal)));
    }

    private static InspectedItem FullItem() => new()
    {
        Id = Guid.NewGuid(),
        Slug = "slug",
        Owner = "ada",
        Mood = Mood.Busy,
        CreatedAt = DateTime.UtcNow,
        Rating = 4,
        Weight = 1.5,
        Ratio = 0.5f,
        Price = 9.99m,
        Duration = TimeSpan.FromMinutes(3),
        DueAt = DateTimeOffset.UtcNow,
        Tags = ["a"],
        Samples = [7],
        Payload = [1, 2],
    };

    /// <summary>
    /// The inspector's table against the descriptor the generator compiled: same name, same key, same
    /// indexes in the same order, and - for every property - the field number and wire type the generated
    /// writer really puts in front of its value. <paramref name="row"/> has every property set, so every
    /// field is written.
    /// </summary>
    private static void AssertAgrees<TKey, TRow>(BlazeDbInspectedTableDto table, BlazeDbTableDescriptor<TKey, TRow> descriptor, TRow row)
        where TKey : notnull
    {
        Assert.IsTrue(table.Generated, table.TypeName);
        Assert.AreEqual(descriptor.Name, table.TableName, table.TypeName);
        Assert.AreEqual(descriptor.KeyMember, table.Key, table.TypeName);

        CollectionAssert.AreEqual(descriptor.Indexes.Select(index => index.Name).ToArray(), table.Indexes.Select(index => index.Name).ToArray(), table.TypeName);

        foreach (var index in descriptor.Indexes)
        {
            var reported = table.Indexes.Single(i => i.Name == index.Name);
            var ordered = index.GetType().GetGenericTypeDefinition() == typeof(BlazeDbOrderedIndexDefinition<,>);

            Assert.AreEqual(index.IsUnique, reported.Unique, index.Name);
            Assert.AreEqual(ordered ? "Ordered" : "Hash", reported.Kind, index.Name);
            CollectionAssert.AreEqual(index.Members.ToArray(), reported.Members, index.Name);
        }

        var writer = new BlazeDbBufferWriter();
        descriptor.RowWriter(writer, row);

        var written = ReadTags(writer.WrittenSpan.ToArray());

        CollectionAssert.AreEquivalent(written.Keys.ToArray(), table.Fields.Select(field => field.FieldNumber).ToArray(), table.TypeName);

        foreach (var field in table.Fields)
        {
            Assert.AreEqual(written[field.FieldNumber].ToString(), field.WireType, $"{table.TypeName}.{field.Property}");
        }

        // Every property the type stores is reported, and the ignored one is not.
        var stored = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(BlazeDbIgnoreAttribute)) is false)
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(stored, table.Fields.Select(field => field.Property).ToArray(), table.TypeName);
        Assert.AreEqual(descriptor.KeyMember, table.Fields.Single(field => field.IsKey).Property, table.TypeName);
    }

    /// <summary>The field number and wire type of every tag a row's bytes carry.</summary>
    private static Dictionary<int, BlazeDbWireType> ReadTags(byte[] bytes)
    {
        var tags = new Dictionary<int, BlazeDbWireType>();
        var reader = new BlazeDbBufferReader(bytes);

        while (reader.Remaining > 0)
        {
            var (number, wire) = reader.ReadTag();
            tags[number] = wire;
            reader.SkipField(wire);
        }

        return tags;
    }
}
