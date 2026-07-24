using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract for the semantic (intent) alias tier: <c>Styles/semantic-tokens.scss</c> and
/// <c>BitTheme.Color.Semantic</c> must describe the same <c>--bit-sem-*</c> vocabulary, and every
/// alias must reference a primitive the theme system can actually set. This is what makes the
/// tier supported API instead of a decorative token list - adding/removing a token on either side
/// without the other fails here.
/// </summary>
[TestClass]
public sealed class BitThemeSemanticTokensContractTests
{
    private static readonly Regex SemDeclaration = new(
        @"(--bit-sem-[a-z0-9-]+)\s*:\s*([^;]+);",
        RegexOptions.Compiled);

    private static string ReadSemanticTokensScss()
    {
        var scssPath = Path.Combine(AppContext.BaseDirectory, "theme-styles", "semantic-tokens.scss");
        Assert.IsTrue(File.Exists(scssPath), $"Missing {scssPath}; ensure the library Styles folder is copied to output.");
        return File.ReadAllText(scssPath);
    }

    private static string[] MapperEmittedKeys()
    {
        // Populate every leaf so the mapper emits its complete vocabulary.
        var theme = new BitTheme();
        BitThemeTestGraph.FillStringLeavesWithSentinels(theme);
        return BitThemeUtilities.ToCssVariables(theme).Keys.ToArray();
    }

    [TestMethod]
    public void StylesheetAndThemeModelAgreeOnTheSemanticVocabulary()
    {
        var declared = SemDeclaration.Matches(ReadSemanticTokensScss())
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        var emitted = MapperEmittedKeys()
            .Where(k => k.StartsWith("--bit-sem-", StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(declared.Length > 0, "semantic-tokens.scss declares no --bit-sem-* tokens.");
        CollectionAssert.AreEqual(declared, emitted,
            "Styles/semantic-tokens.scss and BitTheme.Color.Semantic have drifted apart. " +
            $"Declared: [{string.Join(", ", declared)}] Emitted: [{string.Join(", ", emitted)}]");
    }

    [TestMethod]
    public void EverySemanticAliasReferencesAThemableToken()
    {
        // Each default value must be a single var() reference to a token the mapper can emit, so
        // the alias tracks the active palette AND stays overridable through the C# theme model.
        var emitted = MapperEmittedKeys();

        foreach (Match declaration in SemDeclaration.Matches(ReadSemanticTokensScss()))
        {
            var name = declaration.Groups[1].Value;
            var value = declaration.Groups[2].Value.Trim();

            var reference = Regex.Match(value, @"^var\((--bit-[a-z0-9-]+)\)$");
            Assert.IsTrue(reference.Success,
                $"{name} default '{value}' must be a plain var(--bit-...) reference to a primitive token.");

            var target = reference.Groups[1].Value;
            Assert.IsTrue(emitted.Contains(target, StringComparer.Ordinal),
                $"{name} references {target}, which the theme mapper never emits - the alias would not respond to theming.");
        }
    }
}
