using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeDtcgTests
{
    // ── Round-trip fidelity ───────────────────────────────────────────────────

    [TestMethod]
    public void ExportThenImportPreservesEveryToken()
    {
        // A fully-derived theme touches every color slot plus a shape override; the DTCG round-trip
        // must reproduce it byte-for-byte through the canonical serializer form.
        var theme = BitThemeFactory.CreateLightTheme(new BitThemeAccentColors
        {
            Primary = "#1A86D8",
            Success = "#107C10",
            Error = "#C50F1F",
        });
        theme.Shape.BorderRadius = "0.5rem";
        theme.Motion.Duration = "200ms";
        theme.ZIndex.Modal = "1300";
        theme.Color.Semantic.SurfaceElevated = "var(--bit-clr-bg-sec)";

        var expected = BitThemeSerialization.Serialize(theme, writeIndented: true);

        var dtcg = BitThemeDtcg.Export(theme);
        var restored = BitThemeDtcg.Import(dtcg);

        Assert.AreEqual(expected, BitThemeSerialization.Serialize(restored, writeIndented: true));
    }

    [TestMethod]
    public void EmptyThemeRoundTripsToEmpty()
    {
        var dtcg = BitThemeDtcg.Export(new BitTheme());
        var restored = BitThemeDtcg.Import(dtcg);

        // Nothing was set, so nothing should be emitted on the way back out.
        Assert.AreEqual("{}", BitThemeSerialization.Serialize(restored));
    }

    // ── Export shape ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ExportWrapsLeavesAsDtcgTokens()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#1A86D8";

        var root = JsonNode.Parse(BitThemeDtcg.Export(theme))!.AsObject();

        var token = root["color"]!["primary"]!["main"]!.AsObject();
        Assert.AreEqual("#1A86D8", token["$value"]!.GetValue<string>());
    }

    [TestMethod]
    public void ExportTypesUniformGroupsAndOverridesTheShadowInColor()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#1A86D8";
        theme.Color.Semantic.FocusRing = "0 0 0 2px #1A86D8";
        theme.Color.Semantic.AccentPrimary = "var(--bit-clr-pri)";
        theme.BoxShadow.Md = "0 2px 4px #0003";
        theme.Spacing.ScalingFactor = "0.5rem";
        theme.ZIndex.Modal = "1300";

        var root = JsonNode.Parse(BitThemeDtcg.Export(theme))!.AsObject();

        Assert.AreEqual("color", root["color"]!["$type"]!.GetValue<string>());
        Assert.AreEqual("shadow", root["boxShadow"]!["$type"]!.GetValue<string>());
        Assert.AreEqual("dimension", root["spacing"]!["$type"]!.GetValue<string>());
        Assert.AreEqual("number", root["zIndex"]!["$type"]!.GetValue<string>());

        // A color-group token inherits color and carries no own $type...
        Assert.IsFalse(root["color"]!["semantic"]!["accentPrimary"]!.AsObject().ContainsKey("$type"));
        // ...but the shadow-valued token overrides to shadow.
        Assert.AreEqual("shadow", root["color"]!["semantic"]!["focusRing"]!["$type"]!.GetValue<string>());
    }

    [TestMethod]
    public void ExportEmitsNumericValueForNumberTypedTokens()
    {
        var theme = new BitTheme();
        theme.ZIndex.Modal = "1300";

        var root = JsonNode.Parse(BitThemeDtcg.Export(theme))!.AsObject();
        var value = root["zIndex"]!["modal"]!["$value"]!;

        // DTCG requires a `number` token's $value to be a JSON number, not a string.
        Assert.AreEqual(JsonValueKind.Number, value.GetValueKind());
        Assert.AreEqual(1300, value.GetValue<int>());
    }

    [TestMethod]
    public void ExportKeepsNonNumericNumberSlotAsString()
    {
        var theme = new BitTheme();
        // A var() reference in a number slot cannot be a JSON number; it must stay a string so the
        // reference survives instead of being dropped.
        theme.ZIndex.Modal = "var(--app-modal-z)";

        var root = JsonNode.Parse(BitThemeDtcg.Export(theme))!.AsObject();
        var value = root["zIndex"]!["modal"]!["$value"]!;

        Assert.AreEqual(JsonValueKind.String, value.GetValueKind());
        Assert.AreEqual("var(--app-modal-z)", value.GetValue<string>());
    }

    [TestMethod]
    public void NumberTokenRoundTripsThroughDtcg()
    {
        var theme = new BitTheme();
        theme.ZIndex.Modal = "1300";
        theme.ZIndex.Base = "1";

        var restored = BitThemeDtcg.Import(BitThemeDtcg.Export(theme));

        Assert.AreEqual("1300", restored.ZIndex.Modal);
        Assert.AreEqual("1", restored.ZIndex.Base);
    }

    [TestMethod]
    public void ExportOmitsUnsetTokens()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#1A86D8";

        var root = JsonNode.Parse(BitThemeDtcg.Export(theme))!.AsObject();

        Assert.IsFalse(root.ContainsKey("boxShadow"), "unset boxShadow group must not appear");
        Assert.IsFalse(root["color"]!["primary"]!.AsObject().ContainsKey("mainHover"), "unset leaf must not appear");
    }

    // ── Import: aliases ───────────────────────────────────────────────────────

    [TestMethod]
    public void ImportResolvesAliasToConcreteValue()
    {
        const string json = """
        {
          "color": {
            "$type": "color",
            "primary": {
              "main": { "$value": "#1A86D8" },
              "mainHover": { "$value": "{color.primary.main}" }
            }
          }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("#1A86D8", theme.Color.Primary.Main);
        Assert.AreEqual("#1A86D8", theme.Color.Primary.MainHover);
    }

    [TestMethod]
    public void ImportResolvesAliasChain()
    {
        const string json = """
        {
          "color": {
            "primary": {
              "main": { "$value": "#1A86D8" },
              "dark": { "$value": "{color.primary.main}" },
              "darkHover": { "$value": "{color.primary.dark}" }
            }
          }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("#1A86D8", theme.Color.Primary.DarkHover);
    }

    [TestMethod]
    public void ImportDropsDanglingAndCyclicAliases()
    {
        const string json = """
        {
          "color": {
            "primary": {
              "main": { "$value": "{color.does.not.exist}" },
              "mainHover": { "$value": "{color.primary.mainActive}" },
              "mainActive": { "$value": "{color.primary.mainHover}" }
            }
          }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        // Dangling and both sides of the cycle resolve to nothing → left unset (stylesheet default),
        // and crucially it does not throw or hang.
        Assert.IsNull(theme.Color.Primary.Main);
        Assert.IsNull(theme.Color.Primary.MainHover);
        Assert.IsNull(theme.Color.Primary.MainActive);
    }

    // ── Import: 2025.10 object forms and numbers ──────────────────────────────

    [TestMethod]
    public void ImportAcceptsDimensionObjectForm()
    {
        const string json = """
        { "shape": { "borderRadius": { "$type": "dimension", "$value": { "value": 8, "unit": "px" } } } }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("8px", theme.Shape.BorderRadius);
    }

    [TestMethod]
    public void ImportAcceptsColorObjectFormViaHex()
    {
        const string json = """
        {
          "color": {
            "primary": {
              "main": { "$type": "color", "$value": { "colorSpace": "srgb", "components": [0.1, 0.52, 0.85], "hex": "#1A86D8" } }
            }
          }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("#1A86D8", theme.Color.Primary.Main);
    }

    [TestMethod]
    public void ImportDoesNotThrowOnMalformedDimensionObject()
    {
        // A dimension object whose "value" is not a number/string (here a bool) has no dimension
        // mapping. Import must drop the token gracefully per its lenient contract, not throw
        // InvalidOperationException from GetValue<string>() on a non-string node.
        const string json = """
        { "spacing": { "scalingFactor": { "$type": "dimension", "$value": { "value": true, "unit": "px" } } } }
        """;

        var theme = BitThemeDtcg.Import(json);

        // Token dropped -> falls back to default (unset), and nothing else is emitted.
        Assert.AreEqual("{}", BitThemeSerialization.Serialize(theme));
    }

    [TestMethod]
    public void ImportStringifiesNumericValue()
    {
        const string json = """
        {
          "typography": { "button": { "fontWeight": { "$type": "fontWeight", "$value": 600 } } },
          "layout": { "densityScale": { "$value": 0.9 } }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("600", theme.Typography.Button.FontWeight);
        Assert.AreEqual("0.9", theme.Layout.DensityScale);
    }

    // ── Import: robustness ────────────────────────────────────────────────────

    [TestMethod]
    public void ImportIgnoresTokensOutsideTheBitVocabulary()
    {
        const string json = """
        {
          "brand": { "logo": { "$value": "#FF00FF" } },
          "color": { "primary": { "main": { "$value": "#1A86D8" } } }
        }
        """;

        var theme = BitThemeDtcg.Import(json);

        Assert.AreEqual("#1A86D8", theme.Color.Primary.Main);
    }

    [TestMethod]
    public void ImportBlankIsEmptyTheme()
    {
        Assert.AreEqual("{}", BitThemeSerialization.Serialize(BitThemeDtcg.Import(null)));
        Assert.AreEqual("{}", BitThemeSerialization.Serialize(BitThemeDtcg.Import("   ")));
    }

    [TestMethod]
    public void ImportInvalidJsonThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeDtcg.Import("{ not valid"));
    }

    // ── End-to-end with the mapper ────────────────────────────────────────────

    [TestMethod]
    public void ImportedThemeFlowsThroughToCssVariables()
    {
        const string json = """
        {
          "color": {
            "$type": "color",
            "primary": {
              "main": { "$value": "#1A86D8" },
              "mainHover": { "$value": "{color.primary.main}" }
            }
          }
        }
        """;

        var cssVars = BitThemeUtilities.ToCssVariables(BitThemeDtcg.Import(json));

        Assert.AreEqual("#1A86D8", cssVars["--bit-clr-pri"]);
        Assert.AreEqual("#1A86D8", cssVars["--bit-clr-pri-hover"]);
    }
}
