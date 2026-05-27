using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pin the exact set of CSS custom-property names the mapper emits when every leaf string property
/// on <see cref="BitTheme"/> has a value. The companion <c>bit-theme-css-keys.golden.txt</c> file
/// is the source of truth — when you intentionally add or remove a token, regenerate the golden
/// (the failure message lists the diff so the regeneration is mechanical).
/// </summary>
/// <remarks>
/// This is the strict version of <see cref="BitThemeMapperContractTests.ThemeVariablesReferencedTokensAreEmittedByMapperWhenSet"/>.
/// That test only asserts SCSS-referenced tokens are present (no missing keys); this test additionally
/// catches accidental additions / typos in the mapper that wouldn't show up in SCSS, and it locks
/// the mapping for the source-generator refactor planned next.
/// </remarks>
[TestClass]
public sealed class BitThemeMapperKeysetTests
{
    [TestMethod]
    public void EmittedKeysExactlyMatchCheckedInGolden()
    {
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Utils", "Theme", "bit-theme-css-keys.golden.txt");
        if (!File.Exists(goldenPath))
        {
            // Fallback: the file may be copied flat next to the assembly depending on the SDK
            // resolving CopyToOutputDirectory paths. Tolerate either layout.
            goldenPath = Path.Combine(AppContext.BaseDirectory, "bit-theme-css-keys.golden.txt");
        }
        Assert.IsTrue(File.Exists(goldenPath), $"Golden file missing: {goldenPath}. Ensure the test project copies it to output.");

        var golden = File.ReadAllLines(goldenPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        var theme = new BitTheme();
        FillAllStringProperties(theme);

        var actual = BitThemeUtilities.ToCssVariables(theme).Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        if (!golden.SequenceEqual(actual, StringComparer.Ordinal))
        {
            var added = actual.Except(golden, StringComparer.Ordinal).ToArray();
            var removed = golden.Except(actual, StringComparer.Ordinal).ToArray();
            Assert.Fail(
                "BitThemeMapper keyset drifted from golden. " +
                $"Added ({added.Length}): [{string.Join(", ", added)}]. " +
                $"Removed ({removed.Length}): [{string.Join(", ", removed)}]. " +
                $"If intentional, regenerate {Path.GetFileName(goldenPath)}.");
        }
    }

    /// <summary>Reflectively assigns a non-null sentinel to every <see cref="string"/> property in the BitTheme graph.</summary>
    private static void FillAllStringProperties(object root)
    {
        var visited = new System.Collections.Generic.HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        Walk(root, visited);

        static void Walk(object obj, System.Collections.Generic.HashSet<object> visited)
        {
            if (!visited.Add(obj)) return;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;

                var pt = prop.PropertyType;
                if (pt == typeof(string))
                {
                    prop.SetValue(obj, "x");
                }
                else if (!pt.IsValueType)
                {
                    var val = prop.GetValue(obj);
                    if (val is null)
                    {
                        val = Activator.CreateInstance(pt);
                        if (val is null) continue;
                        prop.SetValue(obj, val);
                    }
                    Walk(val, visited);
                }
            }
        }
    }
}
