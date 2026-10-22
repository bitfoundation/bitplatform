using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiHighlighterRegressionTests
{
    [TestMethod]
    public void TripleQuotedStringsAreOneToken()
    {
        const string code = "def f():\n    \"\"\"It's done.\n    Really.\"\"\"\n    return 1";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "python");

        Assert.Contains(t => t.Kind is BitAiTokenKind.String && t.Slice(code).ToString() == "\"\"\"It's done.\n    Really.\"\"\"", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Keyword && t.Slice(code).ToString() == "return", tokens);
    }

    [TestMethod]
    public void AnIndexerIsNotAnAttribute()
    {
        const string code = "[Fact]\nvar x = items[index];";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "csharp");

        Assert.Contains(t => t.Kind is BitAiTokenKind.Attribute && t.Slice(code).ToString() == "[Fact", tokens);
        Assert.DoesNotContain(t => t.Kind is BitAiTokenKind.Attribute && t.Slice(code).ToString() == "[index", tokens);
    }

    [TestMethod]
    public void RustLifetimesAndLabelsAreNotStrings()
    {
        const string code = "fn longest<'a>(x: &'a str) -> &'static str {\n    let c = 'a';\n    let n = '\\n';\n    'outer: loop { break 'outer; }\n}";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "rust");
        string[] Texts(BitAiTokenKind kind) => tokens.Where(t => t.Kind == kind).Select(t => t.Slice(code).ToString()).ToArray();

        CollectionAssert.AreEqual(new[] { "'a", "'a", "'static", "'outer", "'outer" }, Texts(BitAiTokenKind.Label).ToArray());
        Assert.DoesNotContain(t => t.StartsWith('\''), Texts(BitAiTokenKind.Type));
        CollectionAssert.AreEqual(new[] { "'a'", "'\\n'" }, Texts(BitAiTokenKind.String).ToArray());
        Assert.Contains("str", Texts(BitAiTokenKind.Type));
        Assert.Contains("loop", Texts(BitAiTokenKind.Keyword));
    }

    [TestMethod]
    public void RustAttributesAreAttributes()
    {
        const string code = "#![allow(dead_code)]\n#[derive(Debug, Clone)]\nstruct S;\n#[tokio::main]\nasync fn main() {}";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "rust");

        CollectionAssert.AreEqual(
            new[] { "#![allow", "#[derive", "#[tokio::main" },
            tokens.Where(t => t.Kind is BitAiTokenKind.Attribute).Select(t => t.Slice(code).ToString()).ToArray());
        Assert.Contains(t => t.Kind is BitAiTokenKind.Type && t.Slice(code).ToString() == "Debug", tokens);
    }

    [TestMethod]
    [DataRow("css", ".a { background: url(https://x.dev/a.png); color: red; }", "https://x.dev/a.png")]
    [DataRow("css", ".a { background: URL(//cdn.x.dev/a.png) }", "//cdn.x.dev/a.png")]
    [DataRow("scss", ".a { b: url(\"a).png\"); }", "\"a).png\"")]
    [DataRow("css", ".a { b: url(data:image/png;base64,iVBOR//8=) }", "data:image/png;base64,iVBOR//8=")]
    public void ACssUrlIsOneStringEvenWithSlashes(string language, string code, string argument)
    {
        var tokens = BitAiSyntaxHighlighter.Highlight(code, language);

        Assert.DoesNotContain(t => t.Kind is BitAiTokenKind.Comment, tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.String && t.Slice(code).ToString() == argument, tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Function && t.Slice(code).ToString().Equals("url", StringComparison.OrdinalIgnoreCase), tokens);
    }

    [TestMethod]
    public void ScssLineCommentsStillWorkAroundUrls()
    {
        const string code = "// header\n.a { background: url(https://x.dev/a.png); } // trailing\n.b { color: red; }";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "scss");

        CollectionAssert.AreEqual(new[] { "// header", "// trailing" }, tokens.Where(t => t.Kind is BitAiTokenKind.Comment).Select(t => t.Slice(code).ToString()).ToArray());
        Assert.Contains(t => t.Kind is BitAiTokenKind.Property && t.Slice(code).ToString() == "color", tokens);
    }

    [TestMethod]
    public void AnUnclosedUrlStillStreamsAsAString()
    {
        const string code = ".a { background: url(https://x.dev/a";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "css");

        Assert.DoesNotContain(t => t.Kind is BitAiTokenKind.Comment, tokens);
        Assert.AreEqual(BitAiTokenKind.String, tokens[^1].Kind);
        Assert.AreEqual(code.Length, tokens[^1].Start + tokens[^1].Length);
    }

    [TestMethod]
    public void ReplacingALanguageAlsoReplacesItUnderItsAliases()
    {
        var original = new BitAiLanguageDefinition("regression-lang") { Aliases = ["regression-alias"] };
        var replacement = new BitAiLanguageDefinition("regression-lang");

        BitAiLanguageRegistry.Register(original);
        BitAiLanguageRegistry.Register(replacement);

        Assert.AreSame(replacement, BitAiLanguageRegistry.Find("regression-alias"));
        Assert.AreSame(replacement, Assert.ContainsSingle(l => l.Id == "regression-lang", BitAiLanguageRegistry.All));
    }
}
