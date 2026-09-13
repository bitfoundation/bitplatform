using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

/// <summary>
/// The accent feature is split across three implementations of the same contract: BitAccentColorNames
/// / BitAccentColorPersistence in C#, the inline first-paint script BitAccentColorSsr emits, and the
/// BitAccentColor.ts runtime client. Only the first two share their literals through the C# constants;
/// the TypeScript restates them, and a rename on either side compiles and ships silently - the
/// visitor just loses the persisted accent, or the client stops finding the style element the server
/// emitted. These tests read the TypeScript source itself so that drift fails the build instead.
/// </summary>
[TestClass]
public sealed class BitAccentColorNamesContractTests
{
    private static readonly Regex ConstDeclaration = new(@"^\s*const\s+(?<name>[A-Z0-9_]+)\s*=\s*(?<value>'[^']*'|\d+)\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

    [TestMethod]
    public void AccentColorTypeScriptConstantsMatchTheirCSharpDefinitions()
    {
        var constants = ReadConstants("BitAccentColor.ts");

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ATTRIBUTE"] = BitAccentColorNames.Attribute,
            ["STORAGE_KEY"] = BitAccentColorNames.StorageKey,
            ["COOKIE_NAME"] = BitAccentColorNames.CookieName,
            ["CSS_STORAGE_KEY"] = BitAccentColorNames.CssStorageKey,
            ["STYLE_ELEMENT_ID"] = BitAccentColorNames.StyleElementId,
            // The persistence flags cross the interop boundary as the enum's numeric value, so the
            // client's bit masks have to track any renumbering of BitAccentColorPersistence.
            ["PERSIST_LOCAL_STORAGE"] = ((int)BitAccentColorPersistence.LocalStorage).ToString(CultureInfo.InvariantCulture),
            ["PERSIST_COOKIE"] = ((int)BitAccentColorPersistence.Cookie).ToString(CultureInfo.InvariantCulture),
        };

        foreach (var (name, value) in expected)
        {
            Assert.IsTrue(constants.ContainsKey(name),
                $"BitAccentColor.ts no longer declares {name}; the C# side still depends on it (see BitAccentColorNames / BitAccentColorPersistence).");
            Assert.AreEqual(value, constants[name],
                $"BitAccentColor.ts declares {name} = '{constants[name]}', but C# says '{value}'. The two read and write the same attribute/store, so they must stay identical.");
        }
    }

    [TestMethod]
    public void AccentCookieMaxAgeMatchesTheThemeCookies()
    {
        // Not a C# constant on either side - the accent cookie's lifetime is documented to match what
        // the core library writes for its own theme-preference cookie, so the theme client is the
        // reference. Both are the ~400-day cap browsers clamp persistent cookies to.
        var accent = ReadConstants("BitAccentColor.ts");
        var theme = ReadConstants("BitTheme.ts");

        Assert.IsTrue(accent.ContainsKey("COOKIE_MAX_AGE_SECONDS") && theme.ContainsKey("COOKIE_MAX_AGE_SECONDS"),
            "Both clients must keep declaring COOKIE_MAX_AGE_SECONDS for this contract to be checkable.");
        Assert.AreEqual(theme["COOKIE_MAX_AGE_SECONDS"], accent["COOKIE_MAX_AGE_SECONDS"],
            "The accent cookie and the theme cookie are one preference pair; letting one expire before the other leaves the server prerendering a half-restored appearance.");
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
