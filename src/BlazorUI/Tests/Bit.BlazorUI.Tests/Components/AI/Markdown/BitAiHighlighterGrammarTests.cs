using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 25: highlighter grammars.</summary>
[TestClass]
public class BitAiHighlighterGrammarTests
{
    private static string[] Texts(string code, string language, BitAiTokenKind kind) =>
        BitAiSyntaxHighlighter.Highlight(code, language).Where(t => t.Kind == kind).Select(t => t.Slice(code).ToString()).ToArray();

    [TestMethod]
    public void PowerShellHasItsOwnGrammar()
    {
        const string code = "Get-ChildItem -Path $env:HOME -Recurse | Where-Object { $_.Length -gt 100 } # list\n<# block\ncomment #>\n$Name = \"a`\"b\"";

        Assert.AreSequenceEqual(new[] { "Get-ChildItem", "Where-Object" }, Texts(code, "powershell", BitAiTokenKind.Function).ToArray());
        Assert.AreSequenceEqual(new[] { "-Path", "-Recurse" }, Texts(code, "powershell", BitAiTokenKind.Attribute).ToArray());
        Assert.AreSequenceEqual(new[] { "$env:HOME", "$_", "$Name" }, Texts(code, "powershell", BitAiTokenKind.Variable).ToArray());
        Assert.Contains("-gt", Texts(code, "powershell", BitAiTokenKind.Keyword));
        Assert.AreSequenceEqual(new[] { "# list", "<# block\ncomment #>" }, Texts(code, "powershell", BitAiTokenKind.Comment).ToArray());
        Assert.AreSequenceEqual(new[] { "\"a`\"b\"" }, Texts(code, "powershell", BitAiTokenKind.String).ToArray());
    }

    [TestMethod]
    [DataRow("ps1")]
    [DataRow("pwsh")]
    [DataRow("PowerShell")]
    public void PowerShellAliasesResolveToPowerShellNotBash(string alias)
    {
        var actualValue = BitAiLanguageRegistry.Find(alias)?.Id;
        Assert.AreEqual("powershell", actualValue);
    }
    [TestMethod]
    public void BashStillHighlightsItsOwnKeywords() =>
        Assert.Contains("echo", Texts("if true; then echo hi; fi", "bash", BitAiTokenKind.Keyword));

    [TestMethod]
    public void JavaScriptTemplateLiteralsSpanLines()
    {
        const string code = "const s = `line one\n${x} line two`;\nlet y = 1;";
        Assert.AreSequenceEqual(new[] { "`line one\n${x} line two`" }, Texts(code, "typescript", BitAiTokenKind.String).ToArray());
        Assert.Contains("let", Texts(code, "typescript", BitAiTokenKind.Keyword));
    }

    [TestMethod]
    public void PythonTripleQuotedStringsSpanLinesButSingleQuotedOnesDoNot()
    {
        const string code = "s = '''a\nb'''\nt = 'c\nd = 1";
        var strings = Texts(code, "python", BitAiTokenKind.String);

        Assert.Contains("'''a\nb'''", strings);
        Assert.Contains("'c", strings);
        Assert.Contains("1", Texts(code, "python", BitAiTokenKind.Number));
    }
}
