using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// BitThemeAttributeNames is documented as the single source of truth for the theme system's
/// attributes and storage keys, but BitTheme.ts - the only code that actually reads them off the
/// document - restates every one as a local constant. A rename on either side compiles and ships:
/// the host page keeps emitting an attribute the client no longer looks for, so the document simply
/// stops following the OS, stops persisting, or stops painting the browser chrome, with nothing
/// failing anywhere. This reads the TypeScript source itself so that drift fails the build instead.
/// </summary>
[TestClass]
public sealed class BitThemeAttributeNamesContractTests
{
    private static readonly Regex ConstDeclaration = new(@"^\s*const\s+(?<name>[A-Z0-9_]+)\s*=\s*(?<value>'[^']*'|\d+)\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

    [TestMethod]
    public void ThemeTypeScriptConstantsMatchTheirCSharpDefinitions()
    {
        var constants = ReadConstants("BitTheme.ts");

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ATTR_THEME"] = BitThemeAttributeNames.Theme,
            ["ATTR_THEME_DEFAULT"] = BitThemeAttributeNames.ThemeDefault,
            ["ATTR_THEME_SYSTEM"] = BitThemeAttributeNames.ThemeSystem,
            ["ATTR_THEME_PERSIST"] = BitThemeAttributeNames.ThemePersist,
            ["ATTR_THEME_PERSIST_COOKIE"] = BitThemeAttributeNames.ThemePersistCookie,
            ["ATTR_THEME_DARK"] = BitThemeAttributeNames.ThemeDark,
            ["ATTR_THEME_LIGHT"] = BitThemeAttributeNames.ThemeLight,
            ["ATTR_THEME_VIEW_TRANSITION"] = BitThemeAttributeNames.ThemeViewTransition,
            ["ATTR_THEME_COLOR_META"] = BitThemeAttributeNames.ThemeColorMeta,
            // Not an attribute but the custom property the theme-color sync falls back to. It is
            // documented in C# (and by the demo / template host pages that name their own), so a
            // change to the client's default has to be a deliberate change to both.
            ["THEME_COLOR_VARIABLE"] = BitThemeAttributeNames.ThemeColorVariable,
            ["STORAGE_KEY"] = BitThemeAttributeNames.ThemeStorageKey,
            ["COOKIE_NAME"] = BitThemeCookie.PreferenceCookieName,
        };

        foreach (var (name, value) in expected)
        {
            Assert.IsTrue(constants.ContainsKey(name),
                $"BitTheme.ts no longer declares {name}; the C# side still publishes it (see BitThemeAttributeNames / BitThemeCookie).");
            Assert.AreEqual(value, constants[name],
                $"BitTheme.ts declares {name} = '{constants[name]}', but C# says '{value}'. The host page emits what C# names and the client reads what TypeScript names, so the two must stay identical.");
        }
    }

    private static Dictionary<string, string> ReadConstants(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ts-sources", fileName);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure {fileName} is copied to output (see Bit.BlazorUI.Tests.csproj).");

        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in ConstDeclaration.Matches(File.ReadAllText(path)))
        {
            constants[match.Groups["name"].Value] = match.Groups["value"].Value.Trim('\'');
        }

        return constants;
    }
}
