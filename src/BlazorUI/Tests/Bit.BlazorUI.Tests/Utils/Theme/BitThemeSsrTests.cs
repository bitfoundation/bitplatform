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
}
