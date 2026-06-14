using System;
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
        // An invalid default is ignored; a valid concrete preference is still emitted safely.
        Assert.AreEqual("bit-theme=\"dark\"",
            BitThemeSsr.BuildRootThemeAttributes("dark", defaultTheme: "\"><b>"));
    }
}
