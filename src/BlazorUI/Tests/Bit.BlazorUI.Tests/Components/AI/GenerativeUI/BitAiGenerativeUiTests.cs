using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.GenerativeUI;

[TestClass]
public class BitAiGenerativeUiTests
{
    private sealed class WeatherCard : ComponentBase
    {
        [Parameter] public string City { get; set; } = string.Empty;
        [Parameter] public int Days { get; set; }
        [Parameter] public EventCallback OnClick { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? Extra { get; set; }
    }

    [TestMethod]
    public void RegistrationExposesOnlyBindableParameters()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", "Shows a forecast.", "City");

        var names = component.Properties.Select(p => p.Name).ToArray();
        Assert.Contains("City", names);
        Assert.Contains("Days", names);
        Assert.DoesNotContain("OnClick", names);
        Assert.DoesNotContain("ChildContent", names);
        Assert.DoesNotContain("Extra", names);
    }

    [TestMethod]
    public void UnregisteredNamesResolveToNothing()
    {
        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<WeatherCard>("weather_card");

        Assert.IsNotNull(registry.Find("WEATHER_CARD"));
        Assert.IsNull(registry.Find("System.Diagnostics.Process"));
    }

    [TestMethod]
    public void BindingConvertsKnownPropertiesAndIgnoresUnknownOnes()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card");
        var props = JsonDocument.Parse("""{"city":"Oslo","days":3,"onClick":"evil","nope":1}""").RootElement;

        var result = BitAiGenerativePropsBinder.Bind(component, props);

        Assert.AreEqual("Oslo", result.Parameters["City"]);
        Assert.AreEqual(3, result.Parameters["Days"]);
        Assert.IsFalse(result.Parameters.ContainsKey("OnClick"));
        Assert.AreEqual(2, result.Parameters.Count);

        // Ignored, as documented: listed for diagnostics, but not errors and not held against completeness.
        Assert.IsEmpty(result.Errors);
        Assert.IsTrue(result.IsComplete);
        CollectionAssert.AreEqual(new[] { "onClick", "nope" }, result.IgnoredProperties.ToArray());
    }

    [TestMethod]
    public void AnUnknownPropertyDoesNotHideARealError()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", null, "City");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"days":"many","nope":1}""").RootElement);

        Assert.IsFalse(result.IsComplete);
        Assert.AreEqual(2, result.Errors.Count);
        CollectionAssert.AreEqual(new[] { "nope" }, result.IgnoredProperties.ToArray());
    }

    [TestMethod]
    public void BindingReportsMissingRequiredPropertiesWithoutThrowing()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", null, "City");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"days":1}""").RootElement);

        Assert.IsFalse(result.IsComplete);
        Assert.Contains(e => e.Contains("City", StringComparison.Ordinal), result.Errors);
    }

    [TestMethod]
    public void BindingToleratesAPartialPayload()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card");
        var partial = BitAiPartialJson.TryParse("""{"city":"Osl""");

        var result = BitAiGenerativePropsBinder.Bind(component, partial);

        Assert.AreEqual("Osl", result.Parameters["City"]);
    }

    [TestMethod]
    public void TheCatalogueSchemaListsEveryRegisteredComponent()
    {
        var registry = new BitAiGenerativeUiRegistry();
        registry.Register<WeatherCard>("weather_card", "Shows a forecast.");

        var schema = registry.DescribeAll();
        var variants = schema.GetProperty("oneOf");

        Assert.AreEqual(1, variants.GetArrayLength());
        Assert.AreEqual("weather_card",
            variants[0].GetProperty("properties").GetProperty("component").GetProperty("const").GetString());
    }

    public enum Severity { Info, Warning }

    public sealed class Unconstructible(int code)
    {
        public int Value { get; } = code;
    }

    private sealed class AlertCard : ComponentBase
    {
        [Parameter] public Severity Level { get; set; }
        [Parameter] public DateTimeOffset When { get; set; }
        [Parameter] public Guid Id { get; set; }
        [Parameter] public byte Priority { get; set; }
        [Parameter] public Dictionary<string, string>? Labels { get; set; }
        [Parameter] public List<string>? Tags { get; set; }
        [Parameter] public Unconstructible? Broken { get; set; }
    }

    [TestMethod]
    public void EnumPropertiesBindFromTheNamesTheSchemaAdvertises()
    {
        var component = new BitAiGenerativeUiRegistry().Register<AlertCard>("alert");
        Assert.AreEqual("string", component.Schema.GetProperty("properties").GetProperty("Level").GetProperty("type").GetString());

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"level":"Warning"}""").RootElement);

        Assert.IsTrue(result.IsComplete, string.Join("; ", result.Errors));
        Assert.AreEqual(Severity.Warning, result.Parameters["Level"]);
    }

    [TestMethod]
    [DataRow("When", "string")]
    [DataRow("Id", "string")]
    [DataRow("Priority", "integer")]
    [DataRow("Labels", "object")]
    [DataRow("Tags", "array")]
    public void TheSchemaDescribesEachTypeTheWayTheBinderReadsIt(string property, string expected)
    {
        var component = new BitAiGenerativeUiRegistry().Register<AlertCard>("alert");

        var type = component.Schema.GetProperty("properties").GetProperty(property).GetProperty("type").GetString();

        Assert.AreEqual(expected, type);
    }

    [TestMethod]
    public void AParameterTypeTheSerializerCannotBuildIsReportedNotThrown()
    {
        var component = new BitAiGenerativeUiRegistry().Register<AlertCard>("alert");

        var result = BitAiGenerativePropsBinder.Bind(component,
            JsonDocument.Parse("""{"broken":{"value":1},"level":"Info"}""").RootElement);

        Assert.Contains(e => e.Contains("broken", StringComparison.Ordinal), result.Errors);
        Assert.AreEqual(Severity.Info, result.Parameters["Level"]);
    }

    [TestMethod]
    public void AFailureIsDescribedRatherThanWorded()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", null, "City");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"days":"many"}""").RootElement);

        Assert.HasCount(2, result.Failures);

        var unreadable = result.Failures[0];
        Assert.AreEqual(BitAiGenerativeBindErrorKind.Unreadable, unreadable.Kind);
        Assert.AreEqual("days", unreadable.PropertyName);
        Assert.AreEqual(typeof(int), unreadable.ExpectedType);
        Assert.AreEqual("weather_card", unreadable.ComponentName);
        Assert.IsFalse(string.IsNullOrEmpty(unreadable.Detail));

        var missing = result.Failures[1];
        Assert.AreEqual(BitAiGenerativeBindErrorKind.Missing, missing.Kind);
        Assert.AreEqual("City", missing.PropertyName);
        Assert.AreEqual("weather_card", missing.ComponentName);
        Assert.IsNull(missing.ExpectedType);
        Assert.IsNull(missing.Detail);
    }

    [TestMethod]
    public void TheErrorsAreTheEnglishDescriptionsOfTheFailures()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", null, "City");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"days":"many"}""").RootElement);

        CollectionAssert.AreEqual(result.Failures.Select(f => f.ToString()).ToArray(), result.Errors.ToArray());
        Assert.AreEqual($"'days' could not be read as Int32: {result.Failures[0].Detail}", result.Errors[0]);
        Assert.AreEqual("'City' is required by 'weather_card'.", result.Errors[1]);
    }

    [TestMethod]
    public void ABindingThatSucceededHasNoFailures()
    {
        var registry = new BitAiGenerativeUiRegistry();
        var component = registry.Register<WeatherCard>("weather_card", null, "City");

        var result = BitAiGenerativePropsBinder.Bind(component, JsonDocument.Parse("""{"city":"Oslo","days":3}""").RootElement);

        Assert.IsEmpty(result.Failures);
        Assert.IsTrue(result.IsComplete);
    }
}
