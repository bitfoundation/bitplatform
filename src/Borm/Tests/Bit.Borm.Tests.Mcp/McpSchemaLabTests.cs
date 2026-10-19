using System.Text.Json.Nodes;
using Bit.Borm.Demo.Server.Dtos;

namespace Bit.Borm.Tests.Mcp;

/// <summary>
/// InspectBormSchema on the documents an application actually meets: the sample, a repeater, a
/// conditional written in plain JSON Schema, one with every mistake the importer passes over in
/// silence, and input that is not a schema at all. The documents are built as JSON objects, so each
/// test changes exactly the one thing it is about.
/// </summary>
[TestClass]
public class McpSchemaLabTests
{
    [TestMethod]
    public async Task Without_a_schema_it_hands_out_a_sample_the_library_reads()
    {
        var report = await InspectAsync(null);

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Sample);

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(report.Sample, out var definition, out var error), error);
        Assert.HasCount(8, definition.Fields);
        Assert.HasCount(2, definition.Steps);
        Assert.IsEmpty(definition.Validate(), "The sample should be a schema the library's own checks are happy with.");
    }

    [TestMethod]
    public async Task The_sample_is_reported_field_by_field()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.AreEqual("Support request", report.Title);
        Assert.AreEqual(8, report.FieldCount);
        Assert.IsEmpty(report.Diagnostics!);
        Assert.IsTrue(report.RoundTrips);
        CollectionAssert.AreEqual(new[] { "you", "request" }, report.Steps!.Select(step => step.Id).ToArray());

        var company = Field(report, "company");

        Assert.AreEqual("you", company.Step);
        Assert.AreEqual("""{"field":"forCompany","op":"IsTrue"}""", company.VisibleWhen);
        Assert.AreEqual("""{"field":"forCompany","op":"IsTrue"}""", company.RequiredWhen);
        Assert.IsTrue(company.ClearOnHide);
        Assert.IsFalse(company.Required, "A conditional requirement is not an unconditional one.");

        var topic = Field(report, "topic");

        Assert.AreEqual("Select", topic.Type);
        Assert.IsTrue(topic.Required);
        CollectionAssert.AreEqual(new[] { "bug = Something is broken", "idea = An idea", "billing = Billing" }, topic.Options);

        var severity = Field(report, "severity");

        CollectionAssert.Contains(severity.Rules, "range >= 1 and <= 5");
        Assert.AreEqual("""{"field":"topic","op":"Equals","value":"bug"}""", severity.VisibleWhen);

        Assert.AreEqual("Tags", Field(report, "labels").Type);
    }

    [TestMethod]
    public async Task A_repeater_is_reported_row_by_row()
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["lines"] = new JsonObject
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["product"] = new JsonObject { ["type"] = "string" },
                            ["quantity"] = new JsonObject { ["type"] = "integer", ["exclusiveMinimum"] = 0 }
                        },
                        ["required"] = new JsonArray("product")
                    }
                },
                ["tags"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string", ["pattern"] = "^[a-z]+$" } }
            }
        };

        var report = await InspectAsync(schema.ToJsonString());

        Assert.IsTrue(report.Valid, report.Error);
        CollectionAssert.AreEqual(new[] { "lines", "lines[].product", "lines[].quantity", "tags", "tags[]" }, report.Fields!.Select(field => field.Path).ToArray());

        Assert.AreEqual("Array", Field(report, "lines").Type);
        CollectionAssert.Contains(Field(report, "lines").Rules, "items 1..any");
        Assert.IsTrue(Field(report, "lines[].product").Required);
        CollectionAssert.Contains(Field(report, "lines[].quantity").Rules, "range > 0", "An exclusive bound stays exclusive.");
        CollectionAssert.Contains(Field(report, "tags[]").Rules, "pattern ^[a-z]+$");
        Assert.IsTrue(report.RoundTrips);
    }

    [TestMethod]
    public async Task What_if_then_and_dependent_required_become_is_reported_in_the_extension_form()
    {
        var schema = """
            {
              "type": "object",
              "properties": {
                "country": { "type": "string", "enum": ["US", "CA"] },
                "state": { "type": "string" },
                "email": { "type": "string" },
                "confirmEmail": { "type": "string" }
              },
              "if": { "properties": { "country": { "const": "US" } } },
              "then": { "required": ["state"] },
              "dependentRequired": { "email": ["confirmEmail"] }
            }
            """;

        var report = await InspectAsync(schema);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.Contains("\"country\"", Field(report, "state").RequiredWhen!);
        Assert.Contains("\"US\"", Field(report, "state").RequiredWhen!);
        Assert.Contains("\"email\"", Field(report, "confirmEmail").RequiredWhen!);
        Assert.IsTrue(report.RoundTrips);
    }

    [TestMethod]
    public async Task What_the_schema_checks_find_is_reported_with_the_librarys_own_words()
    {
        var schema = """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "minLength": 10, "maxLength": 2 },
                "nickname": { "type": "string", "x-visibleWhen": { "field": "nowhere", "op": "IsNotEmpty" } }
              },
              "x-steps": [ { "id": "one", "fields": ["name", "ghost"] } ]
            }
            """;

        var report = await InspectAsync(schema);

        Assert.IsTrue(report.Valid, report.Error);

        // The oracle is the library itself: the same document, imported and checked here.
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(schema, out var definition, out _));
        CollectionAssert.AreEqual(definition.Validate().Select(diagnostic => diagnostic.Message).ToArray(), report.Diagnostics!.Select(diagnostic => diagnostic.Message).ToArray());

        Assert.IsTrue(report.Diagnostics!.Any(diagnostic => diagnostic.Severity == "Error" && diagnostic.Path == "ghost"));
        Assert.IsTrue(report.Diagnostics!.Any(diagnostic => diagnostic.Path == "name" && diagnostic.Message.Contains("MinLength", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("Validate() reports", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task What_the_importer_drops_without_a_word_is_pointed_out()
    {
        var schema = """
            {
              "type": "object",
              "properties": {
                "never": false,
                "code": { "type": "string", "pattern": "([" },
                "address": { "$ref": "#/$defs/Address" },
                "remote": { "$ref": "https://example.com/person.json" },
                "company": {
                  "type": "string",
                  "x-visibleWhen": { "field": "code", "op": "neq", "value": "x" },
                  "x-requiredwhen": { "field": "code" },
                  "placeholder": "ACME Ltd"
                },
                "rating": { "type": "integer", "x-widget": "custom", "x-renderer": "stars" }
              },
              "x-steps": [ { "id": "all", "fields": ["code", "address", "remote", "company", "rating"], "visibleWhen": { "field": "code", "op": "wat" } } ]
            }
            """;

        var report = await InspectAsync(schema);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.IsEmpty(report.Diagnostics!, "None of these is something Validate() reports, which is why the tool has to say them.");

        Assert.Contains("pattern ([ (does not compile - never enforced)", Field(report, "code").Rules!);
        Assert.IsNull(Field(report, "company").VisibleWhen, "The unreadable condition is dropped by the importer.");

        Note(report, "'code' (([)", "not a regular expression .NET can compile");
        Note(report, "'#/$defs/Address' at /properties/address", "point at nothing");
        Note(report, "'https://example.com/person.json' at /properties/remote", "point outside the document");
        Note(report, "x-visibleWhen at /properties/company", "could not be read");
        Note(report, "the visibleWhen of x-steps[0]", "could not be read");
        Note(report, "'x-requiredwhen' at /properties/company (did you mean 'x-requiredWhen'?)", "not ones Borm reads");
        Note(report, "'placeholder' at /properties/company (did you mean 'x-placeholder'?)", "neither JSON Schema nor anything Borm reads");
        Note(report, "'never' (false)", "became no field");
        Note(report, "'rating' ('stars')", "RegisterCustom");
    }

    [TestMethod]
    public async Task A_document_the_importer_refuses_is_answered_with_the_importers_own_message()
    {
        foreach (var schema in new[] { """{ "type": "object", "properties": { "a": { "type": "string" }, } }""", "[1, 2, 3]", "{ nope" })
        {
            var report = await InspectAsync(schema);

            Assert.IsFalse(BormJsonSchemaImporter.TryImport(schema, out _, out var error));
            Assert.IsFalse(report.Valid, schema);
            Assert.AreEqual(error, report.Error, schema);
            Assert.IsTrue(report.Notes.Any(note => note.Contains("TryImport's own message", StringComparison.Ordinal)), schema);
        }
    }

    [TestMethod]
    public async Task A_document_past_the_importers_budget_is_refused_rather_than_built()
    {
        var properties = new JsonObject();
        for (var i = 0; i <= 10_000; i++) properties[$"f{i}"] = new JsonObject { ["type"] = "string" };

        var report = await InspectAsync(new JsonObject { ["type"] = "object", ["properties"] = properties }.ToJsonString());

        Assert.IsFalse(report.Valid);
        Assert.Contains("refused", report.Error!);
    }

    [TestMethod]
    public async Task A_schema_past_the_tool_limit_is_refused_with_the_reason()
    {
        var huge = new string(' ', 2 * 1024 * 1024 + 1) + """{ "type": "object" }""";

        var report = await InspectAsync(huge);

        Assert.IsFalse(report.Valid);
        Assert.Contains("2097152 bytes", report.Error!);
    }

    [TestMethod]
    public async Task An_empty_schema_is_a_form_with_no_fields_and_says_so()
    {
        var report = await InspectAsync("{}");

        Assert.IsTrue(report.Valid, report.Error);
        Assert.AreEqual(0, report.FieldCount);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("no fields at all", StringComparison.Ordinal)));
    }

    internal static Task<BormSchemaReportDto> InspectAsync(string? schema)
    {
        var arguments = new Dictionary<string, object?>();
        if (schema is not null) arguments["schema"] = schema;

        return McpCall.StructuredAsync<BormSchemaReportDto>("InspectBormSchema", arguments);
    }

    private static BormSchemaFieldDto Field(BormSchemaReportDto report, string path)
        => report.Fields!.SingleOrDefault(field => field.Path == path) ?? throw new AssertFailedException($"The report has no field '{path}': {string.Join(", ", report.Fields!.Select(field => field.Path))}");

    private static void Note(BormSchemaReportDto report, string subject, string finding)
    {
        Assert.IsTrue(report.Notes.Any(note => note.Contains(subject, StringComparison.Ordinal) && note.Contains(finding, StringComparison.Ordinal)),
            $"No note says {subject} {finding}:{Environment.NewLine}{string.Join(Environment.NewLine, report.Notes)}");
    }
}
