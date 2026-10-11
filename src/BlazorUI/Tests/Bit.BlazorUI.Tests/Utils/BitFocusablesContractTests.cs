using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the library's one definition of what can hold the focus - Utils.focusableSelector with isFocusable and the
/// helpers built on it - which the initial focus, every focus trap and every script that hands the focus on asks.
/// Bit.BlazorUI.Extras calls the core members through the ambient declaration of its Scripts/BitBlazorUI.d.ts, which
/// nothing compiles against the core script; these tests fail when a member declared there is no longer in Utils, and
/// when a script declares a focusable selector of its own again, which a fix to the shared one would not reach.
/// </summary>
[TestClass]
public sealed class BitFocusablesContractTests
{
    [TestMethod]
    public void EveryMemberTheExtrasDeclareShouldBeInTheCoreUtils()
    {
        var core = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts");
        var declaration = SourceFiles.Read("Bit.BlazorUI.Extras", "Scripts", "BitBlazorUI.d.ts");

        var utils = Regex.Match(declaration, @"class Utils \{(?<members>.*?)\n\s*\}", RegexOptions.Singleline);

        Assert.IsTrue(utils.Success, "The Utils class was not found in BitBlazorUI.d.ts.");

        var declared = Regex.Matches(utils.Groups["members"].Value, @"\bstatic\s+(?:readonly\s+)?(?<name>\w+)")
                            .Select(m => m.Groups["name"].Value)
                            .ToList();

        Assert.AreNotEqual(0, declared.Count, "BitBlazorUI.d.ts declares no member of Utils.");

        var missing = declared.Where(name => Regex.IsMatch(core, $@"\bpublic\s+static\s+(?:readonly\s+)?{name}\b") is false).ToList();

        Assert.AreEqual(0, missing.Count,
            $"BitBlazorUI.d.ts declares members the core Utils has no public static member for: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void NoOtherScriptShouldDeclareAFocusableSelectorOfItsOwn()
    {
        var shared = SourceFiles.GetPath("Bit.BlazorUI", "Scripts", "Utils.ts");

        // A list of what can hold the focus names the elements a tabindex makes focusable; no other selector does.
        var copies = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project), "*.ts", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".d.ts", StringComparison.Ordinal) is false)
            .Where(file => file.Split(Path.DirectorySeparatorChar).Any(part => part is "node_modules" or "wwwroot" or "bin" or "obj") is false)
            .Where(file => string.Equals(Path.GetFullPath(file), Path.GetFullPath(shared), StringComparison.OrdinalIgnoreCase) is false)
            .Where(file => Regex.IsMatch(SourceFiles.ReadFullPath(file), @"['""`][^'""`\n]*\[tabindex\][^'""`\n]*['""`]"))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToList();

        Assert.AreEqual(0, copies.Count,
            $"These scripts declare a focusable selector of their own; ask BitBlazorUI.Utils instead: {string.Join(", ", copies)}");
    }
}
