using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// BitThemePresetRegistry is how a package or an app adds a preset to the theming APIs rather than
/// standing up a parallel set of its own: Bit.BlazorUI.Extras declares Fluent 2 / Material / Cupertino
/// through it, and an app declares its own the same way. These cover the contract that makes that
/// safe - registered presets reach the one surfaces table, a bad token never enters it, and a name
/// can be re-declared.
/// </summary>
[TestClass]
public sealed class BitThemePresetRegistryTests
{
    /// <summary>
    /// The registry is process-global, and BitThemeSurfaces - a live view over it - is the map every
    /// BitThemeHead render defaults to. A preset left behind here would be in another test's markup,
    /// and in a lookup table the tests above assert the contents of, so every name these tests add is
    /// taken back out. They all share the acme- prefix for that.
    /// </summary>
    [TestCleanup]
    public void RemoveThePresetsTheseTestsRegistered()
    {
        foreach (var preset in BitThemePresetRegistry.All.Where(preset => preset.Name.StartsWith("acme-", StringComparison.Ordinal)))
        {
            BitThemePresetRegistry.Remove(preset.Name);
        }
    }

    [TestMethod]
    public void AnAppRegisteredPresetReachesTheSurfacesTable()
    {
        BitThemePresetRegistry.Register(new BitThemePreset
        {
            Name = "acme-registry-dark",
            BackgroundPrimary = "#101010",
            BackgroundSecondary = "#202020",
        });

        Assert.AreEqual("#101010", BitThemeSurfaces.BackgroundPrimary["acme-registry-dark"]);
        Assert.AreEqual("#202020", BitThemeSurfaces.BackgroundSecondary["acme-registry-dark"]);
        Assert.IsTrue(BitThemePresetRegistry.Contains("acme-registry-dark"));
    }

    [TestMethod]
    public void TheSurfacesTableIsALiveViewRatherThanACopy()
    {
        // BitThemeHead is handed the map itself, often from a host page rendered before the app has
        // finished registering: a copy taken at class-init time would silently miss whatever came after.
        var map = BitThemeSurfaces.BackgroundPrimary;

        Assert.IsFalse(map.ContainsKey("acme-live-view-light"));

        BitThemePresetRegistry.Register(new BitThemePreset { Name = "acme-live-view-light", BackgroundPrimary = "#FEFEFE" });

        Assert.IsTrue(map.ContainsKey("acme-live-view-light"));
        Assert.AreEqual("#FEFEFE", map["acme-live-view-light"]);
        CollectionAssert.Contains(map.Keys.ToArray(), "acme-live-view-light");
    }

    [TestMethod]
    public void APresetWithNoColorForASurfaceStaysOutOfThatMap()
    {
        BitThemePresetRegistry.Register(new BitThemePreset { Name = "acme-primary-only-light", BackgroundPrimary = "#FDFDFD" });

        Assert.IsTrue(BitThemeSurfaces.BackgroundPrimary.ContainsKey("acme-primary-only-light"));
        Assert.IsFalse(BitThemeSurfaces.BackgroundSecondary.ContainsKey("acme-primary-only-light"),
            "A preset that declares no secondary surface must not appear in that map, or BitThemeHead's same-scheme fallback would pick a color the app never gave it.");
    }

    [TestMethod]
    public void RegisteringANameAgainReplacesIt()
    {
        BitThemePresetRegistry.Register(new BitThemePreset { Name = "acme-replaced-dark", BackgroundPrimary = "#111111" });
        BitThemePresetRegistry.Register(new BitThemePreset { Name = "acme-replaced-dark", BackgroundPrimary = "#222222" });

        Assert.AreEqual("#222222", BitThemeSurfaces.BackgroundPrimary["acme-replaced-dark"]);
        Assert.AreEqual(1, BitThemePresetRegistry.All.Count(preset => preset.Name == "acme-replaced-dark"));
    }

    [TestMethod]
    public void NamesAreNormalizedAsBitThemeNameNormalizesThem()
    {
        BitThemePresetRegistry.Register(new BitThemePreset { Name = "  Acme-Normalized-Dark  ", BackgroundPrimary = "#0A0A0A" });

        Assert.AreEqual("#0A0A0A", BitThemeSurfaces.BackgroundPrimary["acme-normalized-dark"]);
        Assert.IsTrue(BitThemePresetRegistry.Contains("ACME-NORMALIZED-DARK"));
        Assert.AreEqual(BitThemeName.Custom("acme-normalized-dark"), BitThemePresetRegistry.Find("acme-normalized-dark")!.ThemeName);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("acme theme")]
    [DataRow("acme_theme")]
    [DataRow("acme:theme")]
    public void ATokenTheFirstPaintParserWouldRejectNeverEntersTheTable(string name)
    {
        // The SSR script and the cookie parse the same [a-z0-9-] token, so a preset registered under
        // anything else would be reachable from C# and invisible at first paint.
        Assert.ThrowsExactly<ArgumentException>(() => BitThemePresetRegistry.Register(new BitThemePreset { Name = name }));
    }

    [TestMethod]
    public void RemovingAPresetTakesItOutOfTheSurfacesTable()
    {
        BitThemePresetRegistry.Register(new BitThemePreset { Name = "acme-removed-dark", BackgroundPrimary = "#0B0B0B" });

        // By whatever spelling registered it, since that is how Register itself reads a name.
        Assert.IsTrue(BitThemePresetRegistry.Remove("  ACME-Removed-Dark  "));
        Assert.IsFalse(BitThemePresetRegistry.Contains("acme-removed-dark"));
        Assert.IsFalse(BitThemeSurfaces.BackgroundPrimary.ContainsKey("acme-removed-dark"));

        // A name that was never there, and one no token could be made of, are both answered rather
        // than thrown at - removal is how a caller ensures absence, not how it asserts presence.
        Assert.IsFalse(BitThemePresetRegistry.Remove("acme-removed-dark"));
        Assert.IsFalse(BitThemePresetRegistry.Remove("acme theme"));
        Assert.IsFalse(BitThemePresetRegistry.Remove(null));
    }

    [TestMethod]
    public void IsDarkFollowsTheSameEndsWithDarkRuleAsEveryOtherLayer()
    {
        Assert.IsTrue(new BitThemePreset { Name = "acme-dark" }.IsDark);
        Assert.IsFalse(new BitThemePreset { Name = "acme-light" }.IsDark);
        // An instance that has not been through the registry carries the name as written, and
        // registering it would lower-case that to one every other layer calls dark.
        Assert.IsTrue(new BitThemePreset { Name = "Acme-Dark" }.IsDark);
        Assert.IsTrue(BitThemePresetRegistry.Find(BitThemePresets.MaterialDark)!.IsDark);
        Assert.IsFalse(BitThemePresetRegistry.Find(BitThemePresets.MaterialLight)!.IsDark);
    }

    [TestMethod]
    public void TheCorePresetsAreRegisteredWithoutAnyPackageLoading()
    {
        foreach (var preset in new[] { BitThemePresets.Light, BitThemePresets.Dark, BitThemePresets.FluentLight, BitThemePresets.FluentDark })
        {
            Assert.IsTrue(BitThemePresetRegistry.Contains(preset), $"The core package must register {preset} itself.");
        }
    }

    [TestMethod]
    public void NamingAnExtrasPresetIsWhatRegistersThatPackage()
    {
        // The extension members live in Bit.BlazorUI.Extras, so reading one loads the assembly, and
        // loading it runs BitExtraThemeRegistration - which is why an app that names its presets
        // rather than writing the raw token gets their first-paint colors without any setup call.
        var material = BitThemePresets.MaterialDark;

        Assert.AreEqual("material-dark", material);
        Assert.IsTrue(BitThemePresetRegistry.Contains(material));
        Assert.AreEqual(material, BitThemeName.MaterialDark.Value);
    }

    [TestMethod]
    public void TheExtrasPresetsAreAlsoReachableWithoutExtensionMembers()
    {
        // Extension members are a C# 14 feature on the reading side too, and Bit.BlazorUI.Extras
        // still targets net8.0 and net9.0, whose consumers compile at C# 12 / 13 by default and are
        // handed CS9202 for one. BitExtraThemePresets / BitExtraThemeName / BitExtraThemeSurfaces are
        // what those consumers name, so they have to say exactly what the extension members say - and
        // BitExtraThemePresets has to stay const, which is the other thing a property cannot be.
        Assert.AreEqual(BitThemePresets.MaterialDark, BitExtraThemePresets.MaterialDark);
        Assert.AreEqual(BitThemeName.MaterialDark, BitExtraThemeName.MaterialDark);
        Assert.AreEqual(BitThemeSurfaces.BackgroundPrimary[BitThemePresets.MaterialDark], BitExtraThemeSurfaces.BackgroundPrimary[BitExtraThemePresets.MaterialDark]);
        Assert.AreEqual(BitThemeSurfaces.BackgroundSecondary[BitThemePresets.MaterialDark], BitExtraThemeSurfaces.BackgroundSecondary[BitExtraThemePresets.MaterialDark]);

        // const, not static readonly: a case label is the use the extension members cannot serve.
        const string materialDark = BitExtraThemePresets.MaterialDark;
        Assert.AreEqual("material-dark", materialDark);
    }

    [TestMethod]
    public void RegisterIsIdempotentSoAnAppMayCallItExplicitly()
    {
        // The escape hatch for an app that links an Extras stylesheet but never names the package in
        // C#, so nothing has loaded the assembly its module initializer lives in.
        BitExtraThemeRegistration.Register();
        BitExtraThemeRegistration.Register();

        Assert.AreEqual(1, BitThemePresetRegistry.All.Count(preset => preset.Name == BitThemePresets.MaterialDark));
        Assert.AreEqual("#0C131B", BitThemeSurfaces.BackgroundPrimary[BitThemePresets.MaterialDark]);
    }
}
