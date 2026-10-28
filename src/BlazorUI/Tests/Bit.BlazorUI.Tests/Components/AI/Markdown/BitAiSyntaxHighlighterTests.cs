using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiSyntaxHighlighterTests
{
    [TestMethod]
    public void AnUnknownLanguageProducesNoTokens() =>
        Assert.IsEmpty(BitAiSyntaxHighlighter.Highlight("some text", "brainfuck"));

    [TestMethod]
    public void KeywordsTypesStringsAndCommentsAreClassified()
    {
        const string code = "// note\npublic string Name = \"Ada\";";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "csharp");

        Assert.Contains(t => t.Kind is BitAiTokenKind.Comment && t.Slice(code).ToString() == "// note", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Keyword && t.Slice(code).ToString() == "public", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Type && t.Slice(code).ToString() == "string", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.String && t.Slice(code).ToString() == "\"Ada\"", tokens);
    }

    [TestMethod]
    public void TokensNeverOverlapAndStayInsideTheSource()
    {
        const string code = """
            using System;
            /* block */
            var items = new List<int> { 1, 2, 3 };
            Console.WriteLine($"{items.Count} items");
            """;

        var tokens = BitAiSyntaxHighlighter.Highlight(code, "csharp");
        var position = 0;

        foreach (var token in tokens)
        {
            Assert.IsTrue(token.Start >= position, "Tokens must be ordered and must not overlap.");
            Assert.IsTrue(token.Start + token.Length <= code.Length, "Tokens must stay inside the source.");
            position = token.Start + token.Length;
        }
    }

    [TestMethod]
    public void JsonKeysAreHighlightedAsProperties()
    {
        const string code = """{"name":"Ada","age":36}""";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "json");

        // A quoted key is a property, quotes and all; a quoted value is a string.
        CollectionAssert.AreEqual(new[] { "\"name\"", "\"age\"" }, Texts(code, tokens, BitAiTokenKind.Property));
        CollectionAssert.AreEqual(new[] { "\"Ada\"" }, Texts(code, tokens, BitAiTokenKind.String));
        Assert.Contains(t => t.Kind is BitAiTokenKind.Number, tokens);
    }

    [TestMethod]
    public void ANestedJsonObjectPaintsEveryKeyAsAPropertyWhateverTheSpacing()
    {
        const string code = "{\n  \"name\" : \"bit\",\n  \"count\":\t3,\n  \"flags\": { \"on\": true, \"list\": [\"a\", \"b\"] }\n}";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "json");

        CollectionAssert.AreEqual(new[] { "\"name\"", "\"count\"", "\"flags\"", "\"on\"", "\"list\"" }, Texts(code, tokens, BitAiTokenKind.Property));
        CollectionAssert.AreEqual(new[] { "\"bit\"", "\"a\"", "\"b\"" }, Texts(code, tokens, BitAiTokenKind.String));
    }

    [TestMethod]
    [DataRow("json", "{\"a\\\"b\": 1}", "\"a\\\"b\"")]
    [DataRow("yaml", "\"quoted key\": value", "\"quoted key\"")]
    [DataRow("yaml", "- 'single': 1", "'single'")]
    public void AQuotedKeyFollowedByAColonIsAProperty(string language, string code, string key)
    {
        var tokens = BitAiSyntaxHighlighter.Highlight(code, language);

        CollectionAssert.Contains(Texts(code, tokens, BitAiTokenKind.Property), key);
        CollectionAssert.DoesNotContain(Texts(code, tokens, BitAiTokenKind.String), key);
    }

    [TestMethod]
    [DataRow("json", "{\"key\"\n: 1}", "\"key\"")]
    [DataRow("json", "[\"a\", \"b\"]", "\"a\"")]
    [DataRow("json", "{\"unterminated", "\"unterminated")]
    [DataRow("yaml", "key: \"a value\"", "\"a value\"")]
    [DataRow("css", "a[title=\"x\"]:hover { content: \"y\"; }", "\"x\"")]
    [DataRow("css", "a::before { content: \"y\"; }", "\"y\"")]
    [DataRow("javascript", "const o = { \"a\": 1 };", "\"a\"")]
    public void AQuotedValueStaysAString(string language, string code, string value)
    {
        var tokens = BitAiSyntaxHighlighter.Highlight(code, language);

        CollectionAssert.Contains(Texts(code, tokens, BitAiTokenKind.String), value);
        Assert.IsEmpty(Texts(code, tokens, BitAiTokenKind.Property).Where(t => t.StartsWith('"')).ToArray());
    }

    [TestMethod]
    public void YamlAndCssBareKeysAreStillProperties()
    {
        const string yaml = "name: bit\ncount: 3";
        CollectionAssert.AreEqual(new[] { "name", "count" }, Texts(yaml, BitAiSyntaxHighlighter.Highlight(yaml, "yaml"), BitAiTokenKind.Property));

        const string css = ".a { color: red; margin-top: 0 }";
        CollectionAssert.IsSubsetOf(new[] { "color", "top" }, Texts(css, BitAiSyntaxHighlighter.Highlight(css, "css"), BitAiTokenKind.Property));
    }

    private static string[] Texts(string code, IReadOnlyList<BitAiCodeToken> tokens, BitAiTokenKind kind) =>
        tokens.Where(t => t.Kind == kind).Select(t => t.Slice(code).ToString()).ToArray();

    [TestMethod]
    public void MarkupTagsAndAttributesAreClassified()
    {
        const string code = """<a href="https://x.dev" class="link">go</a>""";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "html");

        Assert.Contains(t => t.Kind is BitAiTokenKind.Tag && t.Slice(code).ToString() == "a", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Attribute && t.Slice(code).ToString() == "href", tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.String, tokens);
    }

    [TestMethod]
    public void DiffLinesAreClassifiedByTheirMarker()
    {
        const string code = "@@ -1,3 +1,3 @@\n-old line\n+new line\n unchanged";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "diff");

        Assert.Contains(t => t.Kind is BitAiTokenKind.Meta, tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Deleted, tokens);
        Assert.Contains(t => t.Kind is BitAiTokenKind.Inserted, tokens);
    }

    [TestMethod]
    public void AnUnterminatedStringDoesNotThrow()
    {
        var tokens = BitAiSyntaxHighlighter.Highlight("var s = \"still typing", "csharp");
        Assert.Contains(t => t.Kind is BitAiTokenKind.String, tokens);
    }

    [TestMethod]
    public void LanguagesResolveThroughTheirAliases()
    {
        Assert.IsTrue(BitAiLanguageRegistry.IsSupported("cs"));
        Assert.IsTrue(BitAiLanguageRegistry.IsSupported("TS"));
        Assert.IsTrue(BitAiLanguageRegistry.IsSupported("yml"));
        Assert.IsFalse(BitAiLanguageRegistry.IsSupported("nonexistent"));
    }

    [TestMethod]
    public void ACustomLanguageCanBeRegistered()
    {
        BitAiLanguageRegistry.Register(new BitAiLanguageDefinition("toylang")
        {
            Keywords = new HashSet<string>(["begin", "end"], StringComparer.Ordinal),
        });

        const string code = "begin work end";
        var tokens = BitAiSyntaxHighlighter.Highlight(code, "toylang");

        Assert.AreEqual(2, tokens.Count(t => t.Kind is BitAiTokenKind.Keyword));
    }
}
