using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

[TestClass]
public class BitIconInfoTests
{
    [TestMethod]
    public void Constructor_ShouldSetAllProperties()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        Assert.AreEqual("home", iconInfo.Name);
        Assert.AreEqual("fa", iconInfo.BaseClass);
        Assert.AreEqual("fa-", iconInfo.Prefix);
    }

    [TestMethod]
    public void Constructor_WithOnlyName_ShouldSetNameOnly()
    {
        var iconInfo = new BitIconInfo("home");

        Assert.AreEqual("home", iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
    }

    [TestMethod]
    public void GetCssClasses_WithBaseClassAndPrefix_ShouldReturnCorrectClasses()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("fa fa-home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithBaseClassOnly_ShouldReturnBaseClassAndName()
    {
        var iconInfo = new BitIconInfo("home", "material-icons", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("material-icons home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithPrefixOnly_ShouldReturnPrefixedName()
    {
        var iconInfo = new BitIconInfo("home", "", "icon-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icon-home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithNameOnly_ShouldUseDefaultBitIconClasses()
    {
        var iconInfo = new BitIconInfo("home");

        var result = iconInfo.GetCssClasses();

        // When BaseClass is null, it defaults to "bit-icon"
        // When Prefix is null, it defaults to "bit-icon--"
        Assert.AreEqual("bit-icon bit-icon--home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithEmptyBaseClassAndPrefix_ShouldReturnNameOnly()
    {
        var iconInfo = new BitIconInfo("home", "", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("home", result);
    }

    [TestMethod]
    public void HasValue_WithName_ShouldReturnTrue()
    {
        var iconInfo = new BitIconInfo("home");

        Assert.IsTrue(iconInfo.HasValue());
    }

    [TestMethod]
    public void HasValue_WithEmptyName_ShouldReturnFalse()
    {
        var iconInfo = new BitIconInfo("");

        Assert.IsFalse(iconInfo.HasValue());
    }

    [TestMethod]
    public void HasValue_WithNullName_ShouldReturnFalse()
    {
        var iconInfo = new BitIconInfo(null!);

        Assert.IsFalse(iconInfo.HasValue());
    }

    [TestMethod]
    public void HasValue_WithWhitespaceName_ShouldReturnFalse()
    {
        var iconInfo = new BitIconInfo("   ");

        Assert.IsFalse(iconInfo.HasValue());
    }

    // Factory Method Tests

    [TestMethod]
    public void Css_ShouldCreateIconInfoWithEmptyBaseClassAndPrefix()
    {
        var iconInfo = BitIconInfo.Css("fa-solid fa-home");

        Assert.AreEqual("fa-solid fa-home", iconInfo.Name);
        Assert.AreEqual("", iconInfo.BaseClass);
        Assert.AreEqual("", iconInfo.Prefix);
        Assert.AreEqual("fa-solid fa-home", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void FontAwesome_ShouldCreateIconInfoForFontAwesome6()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-home");

        Assert.AreEqual("fa-solid fa-home", iconInfo.Name);
        Assert.AreEqual("", iconInfo.BaseClass);
        Assert.AreEqual("", iconInfo.Prefix);
        Assert.AreEqual("fa-solid fa-home", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void FontAwesome_ShouldHandleBrandIcons()
    {
        var iconInfo = BitIconInfo.Fa("fa-brands fa-github");

        Assert.AreEqual("fa-brands fa-github", iconInfo.Name);
        Assert.AreEqual("fa-brands fa-github", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void Material_ShouldCreateMaterialIconInfo()
    {
        var iconInfo = BitIconInfo.Material("home");

        Assert.AreEqual("home", iconInfo.Name);
        Assert.AreEqual("material-icons", iconInfo.BaseClass);
        Assert.AreEqual("", iconInfo.Prefix);
        Assert.AreEqual("material-icons home", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void Material_ShouldHandleVariousIconNames()
    {
        var iconInfo = BitIconInfo.Material("settings");

        Assert.AreEqual("settings", iconInfo.Name);
        Assert.AreEqual("material-icons", iconInfo.BaseClass);
        Assert.AreEqual("material-icons settings", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void Bootstrap_ShouldCreateBootstrapIconInfo()
    {
        var iconInfo = BitIconInfo.Bootstrap("check-circle");

        Assert.AreEqual("check-circle", iconInfo.Name);
        Assert.AreEqual("bi", iconInfo.BaseClass);
        Assert.AreEqual("bi-", iconInfo.Prefix);
        Assert.AreEqual("bi bi-check-circle", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void Bootstrap_ShouldHandleVariousIconNames()
    {
        var iconInfo = BitIconInfo.Bootstrap("house");

        Assert.AreEqual("house", iconInfo.Name);
        Assert.AreEqual("bi", iconInfo.BaseClass);
        Assert.AreEqual("bi-", iconInfo.Prefix);
        Assert.AreEqual("bi bi-house", iconInfo.GetCssClasses());
    }

    // Implicit Conversion Tests

    [TestMethod]
    public void ImplicitConversion_FromString_ShouldCreateIconInfo()
    {
        BitIconInfo iconInfo = "home";

        Assert.AreEqual("home", iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
    }

    [TestMethod]
    public void ImplicitConversion_FromNullString_ShouldReturnNull()
    {
        string? nullString = null;
        BitIconInfo? iconInfo = nullString;

        Assert.IsNull(iconInfo);
    }

    [TestMethod]
    public void ImplicitConversion_ToString_ShouldReturnName()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        string? result = iconInfo;

        Assert.AreEqual("home", result);
    }

    [TestMethod]
    public void ImplicitConversion_FromNullIconInfo_ShouldReturnNull()
    {
        BitIconInfo? iconInfo = null;

        string? result = iconInfo;

        Assert.IsNull(result);
    }

    // CSS class generation tests

    [TestMethod]
    public void GetCssClasses_FontAwesome6_SolidIcon_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-user");

        Assert.AreEqual("fa-solid fa-user", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_FontAwesome6_RegularIcon_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-regular fa-heart");

        Assert.AreEqual("fa-regular fa-heart", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_FontAwesome6_BrandIcon_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-brands fa-github");

        Assert.AreEqual("fa-brands fa-github", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_Bootstrap_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Bootstrap("arrow-right");

        Assert.AreEqual("bi bi-arrow-right", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_Material_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Material("favorite");

        Assert.AreEqual("material-icons favorite", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_CustomCss_ShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Css("my-custom-icon-class");

        Assert.AreEqual("my-custom-icon-class", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_CustomIconWithConstructor_ShouldGenerateCorrectClasses()
    {
        var iconInfo = new BitIconInfo("custom-icon", "my-icons", "ico-");

        Assert.AreEqual("my-icons ico-custom-icon", iconInfo.GetCssClasses());
    }

    // Edge Cases

    [TestMethod]
    public void GetCssClasses_WithEmptyBaseClass_ShouldHandleCorrectly()
    {
        var iconInfo = new BitIconInfo("home", "", "icon-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icon-home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithEmptyPrefix_ShouldHandleCorrectly()
    {
        var iconInfo = new BitIconInfo("home", "icons", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icons home", result);
    }

    [TestMethod]
    public void GetCssClasses_WithAllEmptyStrings_ShouldReturnEmptyString()
    {
        var iconInfo = new BitIconInfo("", "", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetCssClasses_WithSpecialCharactersInName_ShouldHandleCorrectly()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-arrow-circle-right");

        Assert.AreEqual("fa-solid fa-arrow-circle-right", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClasses_Bootstrap_WithHyphenatedName_ShouldHandleCorrectly()
    {
        var iconInfo = BitIconInfo.Bootstrap("check-circle-fill");

        Assert.AreEqual("bi bi-check-circle-fill", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void DefaultConstructor_ShouldCreateEmptyIconInfo()
    {
        var iconInfo = new BitIconInfo();

        Assert.IsNull(iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
        Assert.IsFalse(iconInfo.HasValue());
    }

    [TestMethod]
    public void GetCssClasses_WithNullName_ShouldReturnEmptyString()
    {
        var iconInfo = new BitIconInfo();

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("", result);
    }
}
