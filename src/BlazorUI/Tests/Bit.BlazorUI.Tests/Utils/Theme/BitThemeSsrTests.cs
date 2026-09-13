using System;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeSsrTests
{
    [TestMethod]
    public void InlineHeadScriptIsWrappedInScriptTag()
    {
        var script = BitThemeSsr.InlineHeadScript;

        StringAssert.StartsWith(script, "<script>", StringComparison.Ordinal);
        StringAssert.EndsWith(script, "</script>", StringComparison.Ordinal);
        Assert.IsTrue(script.Contains(BitThemeSsr.InlineHeadScriptBody, StringComparison.Ordinal));
    }

    [TestMethod]
    public void InlineHeadScriptBodyReferencesAttributesFromBitThemeAttributeNames()
    {
        var body = BitThemeSsr.InlineHeadScriptBody;

        // Attribute / storage names should be sourced from BitThemeAttributeNames so renames
        // propagate; this guards against regressions that hard-code names again.
        Assert.IsTrue(body.Contains($"'{BitThemeAttributeNames.Theme}'", StringComparison.Ordinal),
            $"Inline script must reference '{BitThemeAttributeNames.Theme}' attribute.");
        Assert.IsTrue(body.Contains($"'{BitThemeAttributeNames.ThemeSystem}'", StringComparison.Ordinal),
            $"Inline script must reference '{BitThemeAttributeNames.ThemeSystem}' attribute.");
        Assert.IsTrue(body.Contains($"'{BitThemeAttributeNames.ThemePersist}'", StringComparison.Ordinal),
            $"Inline script must reference '{BitThemeAttributeNames.ThemePersist}' attribute.");
        Assert.IsTrue(body.Contains($"'{BitThemeAttributeNames.ThemeStorageKey}'", StringComparison.Ordinal),
            $"Inline script must reference '{BitThemeAttributeNames.ThemeStorageKey}' storage key.");
    }

    [TestMethod]
    public void BuildInlineHeadScriptWithoutNonceMatchesDefaultProperty()
    {
        Assert.AreEqual(BitThemeSsr.InlineHeadScript, BitThemeSsr.BuildInlineHeadScript(nonce: null));
        Assert.AreEqual(BitThemeSsr.InlineHeadScript, BitThemeSsr.BuildInlineHeadScript(nonce: ""));
        Assert.AreEqual(BitThemeSsr.InlineHeadScript, BitThemeSsr.BuildInlineHeadScript(nonce: "   "));
    }

    [TestMethod]
    public void BuildInlineHeadScriptWithNonceEmitsNonceAttribute()
    {
        var script = BitThemeSsr.BuildInlineHeadScript("abc123");

        StringAssert.StartsWith(script, "<script nonce=\"abc123\">", StringComparison.Ordinal);
        StringAssert.EndsWith(script, "</script>", StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildInlineHeadScriptHtmlEncodesNonce()
    {
        // A tampered nonce can't break out of the attribute. Realistic CSP nonces are
        // base64url so the encoded path is rare in practice, but defense-in-depth is cheap.
        var script = BitThemeSsr.BuildInlineHeadScript("abc\"<&>");

        Assert.IsFalse(script.Contains("\"<&>", StringComparison.Ordinal),
            "Special characters in the nonce must be HTML-attribute-encoded, not emitted raw.");
        Assert.IsTrue(script.Contains("nonce=\"abc&quot;&lt;&amp;&gt;\"", StringComparison.Ordinal),
            $"Encoded nonce attribute missing in: {script}");
    }

    [TestMethod]
    public void InlineHeadScriptBodyEncodesSharedThemeResolutionPrecedence()
    {
        // The first-paint precedence encoded here MUST stay in lockstep with Theme.init in
        // BitTheme.ts. The order is: a `bit-theme-system` opt-in resolves the OS theme and wins
        // over an explicit bit-theme / bit-theme-default base; a persisted preference then wins
        // over that; finally a literal "system" value (explicit or persisted) resolves to the OS
        // light/dark theme. If you change any of this, change BitTheme.ts init() to match.
        var body = BitThemeSsr.InlineHeadScriptBody;

        var systemOverridesBase = body.IndexOf(
            $"if(r.hasAttribute('{BitThemeAttributeNames.ThemeSystem}')){{base=m?dk:lt;}}",
            StringComparison.Ordinal);
        Assert.IsTrue(systemOverridesBase >= 0,
            "Inline script must resolve the OS theme when bit-theme-system is present, overriding the explicit base.");

        var persistedReadsStorage = body.IndexOf(
            $"if(r.hasAttribute('{BitThemeAttributeNames.ThemePersist}'))",
            StringComparison.Ordinal);
        Assert.IsTrue(persistedReadsStorage >= 0,
            "Inline script must read the persisted preference when bit-theme-persist is present.");

        // A persisted preference is applied AFTER the system override, so it wins (highest precedence).
        Assert.IsTrue(persistedReadsStorage > systemOverridesBase,
            "Persisted preference must be resolved after (and therefore win over) the bit-theme-system override.");

        // A literal "system" value (explicit attribute or persisted) is resolved to OS light/dark last.
        var systemValueResolvesLast = body.IndexOf("if(cur==='system'){cur=m?dk:lt;}", StringComparison.Ordinal);
        Assert.IsTrue(systemValueResolvesLast > persistedReadsStorage,
            "A literal 'system' value must be resolved to the OS theme after the persisted preference is read.");
    }

    [TestMethod]
    public void InlineHeadScriptBaseFallsBackToConfiguredLightThemeNotLiteralLight()
    {
        // BitTheme.ts init resolves its base to Theme._lightTheme (= the bit-theme-light attribute
        // value), so the SSR base fallback must use `lt`, not the literal 'light'. Otherwise a page
        // with <html bit-theme-light="fluent-light"> and no bit-theme/-default/-system paints
        // bit-theme="light" on the server but bit-theme="fluent-light" on hydration - a theme flash.
        var body = BitThemeSsr.InlineHeadScriptBody;

        Assert.IsTrue(
            body.Contains($"base=r.getAttribute('{BitThemeAttributeNames.Theme}')||r.getAttribute('{BitThemeAttributeNames.ThemeDefault}')||lt;", StringComparison.Ordinal),
            "Base fallback must resolve to the configured light theme (lt), matching BitTheme.ts init precedence.");
        Assert.IsFalse(body.Contains("||'light';", StringComparison.Ordinal),
            "Base fallback must not use the literal 'light' (it diverges from the client's _lightTheme).");
    }

    [TestMethod]
    public void BuildRootThemeAttributesEmitsConcretePreferenceDirectly()
    {
        Assert.AreEqual("bit-theme=\"dark\"", BitThemeSsr.BuildRootThemeAttributes("dark"));
        Assert.AreEqual("bit-theme=\"fluent-light\"", BitThemeSsr.BuildRootThemeAttributes("fluent-light"));
    }

    [TestMethod]
    public void BuildRootThemeAttributesNormalizesPreference()
    {
        Assert.AreEqual("bit-theme=\"fluent-dark\"", BitThemeSsr.BuildRootThemeAttributes("  Fluent-DARK  "));
    }

    [TestMethod]
    public void BuildRootThemeAttributesSystemFollowsOsAndCarriesDefault()
    {
        Assert.AreEqual("bit-theme-system", BitThemeSsr.BuildRootThemeAttributes("system"));
        Assert.AreEqual("bit-theme-system bit-theme-default=\"light\"",
            BitThemeSsr.BuildRootThemeAttributes("system", defaultTheme: "light"));
    }

    [TestMethod]
    public void BuildRootThemeAttributesMissingPreferenceUsesDefaultThenSystem()
    {
        Assert.AreEqual("bit-theme=\"light\"", BitThemeSsr.BuildRootThemeAttributes(null, defaultTheme: "light"));
        Assert.AreEqual("bit-theme-system", BitThemeSsr.BuildRootThemeAttributes(null));
        Assert.AreEqual("bit-theme-system", BitThemeSsr.BuildRootThemeAttributes("   "));
    }

    [DataTestMethod]
    [DataRow("\"><script>alert(1)</script>", DisplayName = "html injection")]
    [DataRow("dark; color:red", DisplayName = "css/space characters")]
    [DataRow("light\"onload=", DisplayName = "attribute breakout")]
    public void BuildRootThemeAttributesIgnoresTamperedPreference(string tampered)
    {
        // A tampered cookie value must never reach the document. Invalid tokens are treated as
        // "no preference", so the result falls back to the safe system marker (no default here).
        var attributes = BitThemeSsr.BuildRootThemeAttributes(tampered);

        Assert.AreEqual("bit-theme-system", attributes);
    }

    [TestMethod]
    public void BuildRootThemeAttributesIgnoresTamperedDefaultButKeepsValidPreference()
    {
        // A "system" preference exercises the default-theme emission path (the branch that consults
        // the fallback). A tampered default must be rejected there too, leaving the safe system
        // marker with no injected attribute or markup - never the tampered value.
        Assert.AreEqual("bit-theme-system",
            BitThemeSsr.BuildRootThemeAttributes("system", defaultTheme: "\"><b>"));
    }

    [TestMethod]
    public void BuildRootThemeAttributeMapEmitsMarkersAsBooleansAndNamesAsStrings()
    {
        // Blazor renders a bool `true` attribute as a valueless marker and a string as name="value",
        // so the map has to distinguish the two - that is the whole point of the overload.
        var concrete = BitThemeSsr.BuildRootThemeAttributeMap("dark");
        CollectionAssert.AreEquivalent(new[] { BitThemeAttributeNames.Theme }, concrete.Keys.ToArray());
        Assert.AreEqual("dark", concrete[BitThemeAttributeNames.Theme]);

        var system = BitThemeSsr.BuildRootThemeAttributeMap("system", defaultTheme: "light");
        CollectionAssert.AreEquivalent(
            new[] { BitThemeAttributeNames.ThemeSystem, BitThemeAttributeNames.ThemeDefault },
            system.Keys.ToArray());
        Assert.AreEqual(true, system[BitThemeAttributeNames.ThemeSystem]);
        Assert.AreEqual("light", system[BitThemeAttributeNames.ThemeDefault]);
    }

    [DataTestMethod]
    [DataRow(null, null, DisplayName = "nothing stored")]
    [DataRow("   ", null, DisplayName = "blank preference")]
    [DataRow("dark", null, DisplayName = "concrete preference")]
    [DataRow("  Fluent-DARK  ", null, DisplayName = "unnormalized preference")]
    [DataRow("system", null, DisplayName = "system without default")]
    [DataRow("system", "light", DisplayName = "system with default")]
    [DataRow(null, "light", DisplayName = "default only")]
    [DataRow("\"><script>alert(1)</script>", null, DisplayName = "tampered preference")]
    [DataRow("system", "\"><b>", DisplayName = "tampered default")]
    public void BuildRootThemeAttributeMapMatchesTheStringOverload(string? preference, string? defaultTheme)
    {
        // Both overloads are formatted from one resolution; this pins them together so a change to
        // either can't silently give the Razor host a different first paint than the raw-string host.
        // Compared as sets: attribute order is meaningless to a splat (and the string overload's own
        // ordering is already pinned by BuildRootThemeAttributesSystemFollowsOsAndCarriesDefault).
        var rendered = BitThemeSsr.BuildRootThemeAttributeMap(preference, defaultTheme)
                                  .Select(a => a.Value is bool ? a.Key : $"{a.Key}=\"{a.Value}\"")
                                  .OrderBy(a => a, StringComparer.Ordinal);

        var expected = BitThemeSsr.BuildRootThemeAttributes(preference, defaultTheme)
                                  .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .OrderBy(a => a, StringComparer.Ordinal);

        CollectionAssert.AreEqual(expected.ToArray(), rendered.ToArray());
    }
}
