using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Bit.BlazorUI.Tests.Components.AI.GenerativeUI;

[TestClass]
public class BitAiGenerativeSchemaTests
{
    public enum Tone { Calm, Urgent }

    public sealed record Line(string Text, double Amount)
    {
        [Description("Whether the line is taxed.")]
        public bool Taxed { get; init; }
    }

    [Description("A folder that can hold folders.")]
    public sealed class Folder
    {
        [Required] public string Name { get; set; } = string.Empty;
        public List<Folder> Children { get; set; } = [];
        public Folder? Parent { get; set; }
    }

    public sealed class Trap
    {
        public string Label { get; set; } = string.Empty;
        public Action? OnFire { get; set; }
    }

    public sealed class Reflective
    {
        public Type? Kind { get; set; }
    }

    [Description("Shows an invoice.")]
    private sealed class InvoiceCard : ComponentBase
    {
        [Parameter, Required] public string Customer { get; set; } = string.Empty;
        [Parameter] public Line[] Lines { get; set; } = [];
        [Parameter] public Folder? Attachments { get; set; }
        [Parameter] public Dictionary<string, decimal>? Totals { get; set; }
        [Parameter] public DateOnly Issued { get; set; }
        [Parameter] public DateTimeOffset? Paid { get; set; }
        [Parameter] public Guid Id { get; set; }
        [Parameter] public Uri? Link { get; set; }
        [Parameter] public TimeSpan Terms { get; set; }
        [Parameter] public Tone? Tone { get; set; }
        [Parameter, BitAiGenerativeIgnore] public string TenantKey { get; set; } = string.Empty;
        [Parameter] public Trap? Trap { get; set; }
        [Parameter] public Reflective? Reflective { get; set; }
        [Parameter] public IReadOnlyList<Tone>? Tones { get; set; }
    }

    private static JsonElement Props(BitAiGenerativeComponent component) => component.Schema.GetProperty("properties");

    [TestMethod]
    public void ArraysDescribeTheirItems()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var lines = Props(component).GetProperty("Lines");
        Assert.AreEqual("array", lines.GetProperty("type").GetString());
        var items = lines.GetProperty("items");
        Assert.AreEqual("object", items.GetProperty("type").GetString());
        Assert.AreEqual("string", items.GetProperty("properties").GetProperty("Text").GetProperty("type").GetString());
        Assert.AreEqual("number", items.GetProperty("properties").GetProperty("Amount").GetProperty("type").GetString());
        Assert.AreEqual("Whether the line is taxed.", items.GetProperty("properties").GetProperty("Taxed").GetProperty("description").GetString());
        Assert.IsFalse(items.GetProperty("additionalProperties").GetBoolean());

        var tones = Props(component).GetProperty("Tones");
        CollectionAssert.AreEqual(new[] { "Calm", "Urgent" }, tones.GetProperty("items").GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    [TestMethod]
    public void NestedTypesAreDescribedRecursivelyAndCyclesAreCutOff()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var folder = Props(component).GetProperty("Attachments");
        Assert.AreEqual("object", folder.GetProperty("type").GetString());
        Assert.AreEqual("A folder that can hold folders.", folder.GetProperty("description").GetString());
        CollectionAssert.AreEqual(new[] { "Name" }, folder.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray());

        var children = folder.GetProperty("properties").GetProperty("Children");
        Assert.AreEqual("array", children.GetProperty("type").GetString());
        Assert.AreEqual("object", children.GetProperty("items").GetProperty("type").GetString());
        Assert.IsFalse(children.GetProperty("items").TryGetProperty("properties", out _));
        Assert.IsFalse(folder.GetProperty("properties").GetProperty("Parent").TryGetProperty("properties", out _));
    }

    [TestMethod]
    public void DictionariesDescribeTheirValues()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var totals = Props(component).GetProperty("Totals");
        Assert.AreEqual("object", totals.GetProperty("type").GetString());
        Assert.AreEqual("number", totals.GetProperty("additionalProperties").GetProperty("type").GetString());
    }

    [TestMethod]
    [DataRow("Issued", "date")]
    [DataRow("Paid", "date-time")]
    [DataRow("Id", "uuid")]
    [DataRow("Link", "uri")]
    [DataRow("Terms", "duration")]
    public void WellKnownValueTypesCarryAFormat(string property, string format)
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var node = Props(component).GetProperty(property);
        Assert.AreEqual("string", node.GetProperty("type").GetString());
        Assert.AreEqual(format, node.GetProperty("format").GetString());
    }

    [TestMethod]
    public void EnumsAreNamesAndNullablesAreOptional()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var tone = Props(component).GetProperty("Tone");
        CollectionAssert.AreEqual(new[] { "Calm", "Urgent" }, tone.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.DoesNotContain("Tone", component.Schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
    }

    [TestMethod]
    public void RequiredAndIgnoredAttributesAreHonouredAndTheClassDescriptionIsTheFallback()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        CollectionAssert.AreEqual(new[] { "Customer" }, component.Schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.AreEqual("Shows an invoice.", component.Description);
        Assert.IsNull(component.FindProperty("TenantKey"));
        Assert.IsFalse(Props(component).TryGetProperty("TenantKey", out _));

        var described = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice", "Custom.");
        Assert.AreEqual("Custom.", described.Description);
    }

    [TestMethod]
    public void TypesWhoseGraphReachesCodeAreNotBindable()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        Assert.IsNull(component.FindProperty("Trap"));
        Assert.IsNull(component.FindProperty("Reflective"));
        Assert.IsFalse(BitAiGenerativeUiRegistry.IsBindable(typeof(Trap)));
        Assert.IsFalse(BitAiGenerativeUiRegistry.IsBindable(typeof(List<Trap>)));
        Assert.IsFalse(BitAiGenerativeUiRegistry.IsBindable(typeof(Dictionary<string, Reflective>)));
        Assert.IsFalse(BitAiGenerativeUiRegistry.IsBindable(typeof(RenderFragment<int>)));
        Assert.IsTrue(BitAiGenerativeUiRegistry.IsBindable(typeof(Folder)));
        Assert.IsTrue(BitAiGenerativeUiRegistry.IsBindable(typeof(Line[])));

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"customer":"Ada","trap":{"label":"x"}}""").RootElement);
        Assert.IsFalse(result.Parameters.ContainsKey("Trap"));
        CollectionAssert.AreEqual(new[] { "trap" }, result.IgnoredProperties.ToArray());
    }

    [TestMethod]
    public void TheRequiredCheckRunsEvenWhenNoObjectArrived()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var missing = BitAiGenerativePropsBinder.Bind(component, null);
        Assert.IsFalse(missing.IsComplete);
        Assert.Contains(e => e.Contains("Customer", StringComparison.Ordinal), missing.Errors);

        var wrongShape = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("[1]").RootElement);
        Assert.IsFalse(wrongShape.IsComplete);
        Assert.IsEmpty(wrongShape.Parameters);
    }

    [TestMethod]
    public void NestedPayloadsBindThroughTheSameShapeTheSchemaAdvertises()
    {
        var component = new BitAiGenerativeUiRegistry().Register<InvoiceCard>("invoice");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""
            {
              "customer": "Ada",
              "lines": [ { "text": "Widget", "amount": 9.5, "taxed": true } ],
              "attachments": { "name": "root", "children": [ { "name": "a" } ] },
              "issued": "2026-10-20",
              "tone": "Urgent"
            }
            """).RootElement);

        Assert.IsTrue(result.IsComplete, string.Join("; ", result.Errors));
        var line = Assert.ContainsSingle((Line[])result.Parameters["Lines"]!);
        Assert.AreEqual(("Widget", 9.5, true), (line.Text, line.Amount, line.Taxed));
        Assert.AreEqual("a", ((Folder)result.Parameters["Attachments"]!).Children[0].Name);
        Assert.AreEqual(new DateOnly(2026, 10, 20), result.Parameters["Issued"]);
        Assert.AreEqual(Tone.Urgent, result.Parameters["Tone"]);
    }
}
