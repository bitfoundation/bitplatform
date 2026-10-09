using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the library's one definition of what can hold the focus - Utils.focusableSelector with isFocusable,
/// getFocusables and firstFocusable - which the initial focus, every focus trap and every script that hands the focus
/// on asks. Bit.BlazorUI.Extras is compiled and loaded on its own, so it mirrors the definition in its Extras class
/// rather than calling it; these tests fail when the mirror differs from the original, and when a script declares a
/// focusable selector of its own again, which a fix to the shared one would not reach.
/// </summary>
[TestClass]
public sealed class BitFocusablesContractTests
{
    [TestMethod]
    public void TheExtrasSelectorShouldBeTheCoreOne()
    {
        var core = GetSelector(ReadCore(), "Utils.ts");
        var extras = GetSelector(ReadExtras(), "Extras.ts");

        Assert.AreEqual(core, extras, "Extras.focusableSelector no longer says what Utils.focusableSelector says.");
    }

    [TestMethod,
        DataRow("isFocusable"),
        DataRow("getFocusables"),
        DataRow("firstFocusable")]
    public void TheExtrasMemberShouldBeTheCoreOne(string name)
    {
        var core = GetBody(ReadCore(), "Utils", name);
        var extras = GetBody(ReadExtras(), "Extras", name);

        Assert.AreEqual(core, extras, $"Extras.{name} no longer does what Utils.{name} does.");
    }

    [TestMethod]
    public void NoOtherScriptShouldDeclareAFocusableSelectorOfItsOwn()
    {
        var shared = new[]
        {
            SourceFiles.GetPath("Bit.BlazorUI", "Scripts", "Utils.ts"),
            SourceFiles.GetPath("Bit.BlazorUI.Extras", "Scripts", "Extras.ts"),
        };

        // A list of what can hold the focus names the elements a tabindex makes focusable; no other selector does.
        var copies = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project), "*.ts", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".d.ts", StringComparison.Ordinal) is false)
            .Where(file => file.Split(Path.DirectorySeparatorChar).Any(part => part is "node_modules" or "wwwroot" or "bin" or "obj") is false)
            .Where(file => shared.Contains(Path.GetFullPath(file), StringComparer.OrdinalIgnoreCase) is false)
            .Where(file => Regex.IsMatch(SourceFiles.ReadFullPath(file), @"['""`][^'""`\n]*\[tabindex\][^'""`\n]*['""`]"))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToList();

        Assert.AreEqual(0, copies.Count,
            $"These scripts declare a focusable selector of their own; ask Utils (or, in Extras, its Extras mirror) instead: {string.Join(", ", copies)}");
    }

    private static string ReadCore() => SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts");

    private static string ReadExtras() => SourceFiles.Read("Bit.BlazorUI.Extras", "Scripts", "Extras.ts");

    // The concatenated string literals the selector is written as.
    private static string GetSelector(string script, string file)
    {
        var match = Regex.Match(script, @"public static readonly focusableSelector =(?<value>[^;]*);");

        Assert.IsTrue(match.Success, $"focusableSelector was not found in {file}.");

        return string.Concat(Regex.Matches(match.Groups["value"].Value, "'(?<part>[^']*)'").Select(m => m.Groups["part"].Value));
    }

    // From the member's signature to its closing brace, without its comments, with the class it is declared in left
    // out of its references to its siblings, and with the whitespace collapsed.
    private static string GetBody(string script, string className, string name)
    {
        var match = Regex.Match(script, $@"\n        public static {name}\(.*?\n        }}", RegexOptions.Singleline);

        Assert.IsTrue(match.Success, $"{className}.{name} was not found in {className}.ts.");

        var body = Regex.Replace(match.Value, @"//[^\n]*", string.Empty);
        body = body.Replace($"{className}.", string.Empty, StringComparison.Ordinal);

        return Regex.Replace(body, @"\s+", " ").Trim();
    }
}
