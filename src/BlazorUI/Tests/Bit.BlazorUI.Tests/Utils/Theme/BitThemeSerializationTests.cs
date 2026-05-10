using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeSerializationTests
{
    // ── Serialize ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void SerializeDefaultThemeProducesEmptyJsonObject()
    {
        var json = BitThemeSerialization.Serialize(new BitTheme());

        Assert.AreEqual("{}", json.Trim());
    }

    [TestMethod]
    public void SerializeNullThemeProducesEmptyJsonObject()
    {
        var json = BitThemeSerialization.Serialize(null!);

        Assert.AreEqual("{}", json.Trim());
    }

    [TestMethod]
    public void SerializeSingleTokenSetContainsOnlyThatToken()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#FF0000";

        var json = BitThemeSerialization.Serialize(theme);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Only "color" branch should appear at root
        var rootKeyCount = 0;
        foreach (var _ in root.EnumerateObject()) rootKeyCount++;
        Assert.AreEqual(1, rootKeyCount, "Expected exactly one top-level key.");
        Assert.IsTrue(root.TryGetProperty("color", out var colorEl), "Expected 'color' key.");
        Assert.IsFalse(root.TryGetProperty("boxShadow", out _), "'boxShadow' should be absent.");
        Assert.IsFalse(root.TryGetProperty("typography", out _), "'typography' should be absent.");
        Assert.IsFalse(root.TryGetProperty("spacing", out _), "'spacing' should be absent.");

        // Only "primary" inside color
        Assert.IsTrue(colorEl.TryGetProperty("primary", out var primaryEl), "Expected 'primary' key.");
        Assert.IsFalse(colorEl.TryGetProperty("secondary", out _), "'secondary' should be absent.");
        Assert.AreEqual("#FF0000", primaryEl.GetProperty("main").GetString());
    }

    [TestMethod]
    public void SerializeUnsetSiblingVariantsOmitted()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#111";
        theme.Color.Secondary.Main = "#222";

        var json = BitThemeSerialization.Serialize(theme);
        using var doc = JsonDocument.Parse(json);
        var colorEl = doc.RootElement.GetProperty("color");

        Assert.IsTrue(colorEl.TryGetProperty("primary", out _));
        Assert.IsTrue(colorEl.TryGetProperty("secondary", out _));
        Assert.IsFalse(colorEl.TryGetProperty("tertiary", out _), "'tertiary' should be absent.");
        Assert.IsFalse(colorEl.TryGetProperty("error", out _), "'error' should be absent.");
    }

    [TestMethod]
    public void SerializeTypographyTokenSetEmitsOnlyThatVariant()
    {
        var theme = new BitTheme();
        theme.Typography.H1.FontSize = "2rem";

        var json = BitThemeSerialization.Serialize(theme);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("typography", out var typoEl));
        Assert.IsFalse(root.TryGetProperty("color", out _));
        Assert.IsTrue(typoEl.TryGetProperty("h1", out var h1El));
        Assert.IsFalse(typoEl.TryGetProperty("h2", out _));
        Assert.AreEqual("2rem", h1El.GetProperty("fontSize").GetString());
    }

    [TestMethod]
    public void SerializeUsesCamelCase()
    {
        var theme = new BitTheme();
        theme.Color.Primary.MainHover = "#AAA";

        var json = BitThemeSerialization.Serialize(theme);
        using var doc = JsonDocument.Parse(json);

        Assert.IsTrue(doc.RootElement
                         .GetProperty("color")
                         .GetProperty("primary")
                         .TryGetProperty("mainHover", out _),
                     "Expected camelCase 'mainHover'.");
    }

    [TestMethod]
    public void SerializeBoxShadowTokenSetEmitsBoxShadow()
    {
        var theme = new BitTheme();
        theme.BoxShadow.Sm = "0 1px 3px rgba(0,0,0,.12)";

        var json = BitThemeSerialization.Serialize(theme);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("boxShadow", out var bsEl));
        Assert.IsFalse(root.TryGetProperty("color", out _));
        Assert.AreEqual("0 1px 3px rgba(0,0,0,.12)", bsEl.GetProperty("sm").GetString());
    }

    // ── Deserialize ────────────────────────────────────────────────────────────

    [TestMethod]
    public void DeserializeEmptyJsonReturnsDefaultTheme()
    {
        var theme = BitThemeSerialization.Deserialize("{}");

        Assert.IsNotNull(theme);
        Assert.IsNull(theme.Color.Primary.Main);
    }

    [TestMethod]
    public void DeserializeNullOrWhitespaceReturnsDefaultTheme()
    {
        Assert.IsNotNull(BitThemeSerialization.Deserialize(null!));
        Assert.IsNotNull(BitThemeSerialization.Deserialize(""));
        Assert.IsNotNull(BitThemeSerialization.Deserialize("   "));
    }

    [TestMethod]
    public void DeserializeValidJsonRestoresTokenValue()
    {
        const string json = """{"color":{"primary":{"main":"#ABCDEF"}}}""";

        var theme = BitThemeSerialization.Deserialize(json);

        Assert.AreEqual("#ABCDEF", theme.Color.Primary.Main);
    }

    // ── Round-trip ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void RoundTripSingleTokenPreservesValue()
    {
        var original = new BitTheme();
        original.Color.Primary.Main = "#ABCDEF";

        var roundTrip = BitThemeSerialization.Deserialize(BitThemeSerialization.Serialize(original));

        Assert.AreEqual("#ABCDEF", roundTrip.Color.Primary.Main);
    }

    [TestMethod]
    public void RoundTripMultipleTokensAcrossSectionsAllPreserved()
    {
        var original = new BitTheme();
        original.Color.Primary.Main = "#111";
        original.Color.Error.Main = "#F00";
        original.Typography.H1.FontSize = "2rem";
        original.BoxShadow.Sm = "0 1px 2px #000";

        var roundTrip = BitThemeSerialization.Deserialize(BitThemeSerialization.Serialize(original));

        Assert.AreEqual("#111", roundTrip.Color.Primary.Main);
        Assert.AreEqual("#F00", roundTrip.Color.Error.Main);
        Assert.AreEqual("2rem", roundTrip.Typography.H1.FontSize);
        Assert.AreEqual("0 1px 2px #000", roundTrip.BoxShadow.Sm);
    }

    [TestMethod]
    public void RoundTripDefaultThemeProducesEmptyJsonAndRestoresDefaults()
    {
        var json = BitThemeSerialization.Serialize(new BitTheme());
        var roundTrip = BitThemeSerialization.Deserialize(json);

        Assert.AreEqual("{}", json.Trim());
        Assert.IsNotNull(roundTrip);
        Assert.IsNull(roundTrip.Color.Primary.Main);
        Assert.IsNull(roundTrip.Typography.H1.FontSize);
        Assert.IsNull(roundTrip.BoxShadow.Sm);
    }
}
