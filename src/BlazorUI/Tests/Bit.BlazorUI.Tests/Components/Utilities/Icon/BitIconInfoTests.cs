using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

[TestClass]
public class BitIconInfoTests
{
    [TestMethod]
    public void ConstructorShouldSetAllProperties()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        Assert.AreEqual("home", iconInfo.Name);
        Assert.AreEqual("fa", iconInfo.BaseClass);
        Assert.AreEqual("fa-", iconInfo.Prefix);
    }

    [TestMethod]
    public void ConstructorWithOnlyNameShouldSetNameOnly()
    {
        var iconInfo = new BitIconInfo("home");

        Assert.AreEqual("home", iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
    }

    [TestMethod]
    public void GetCssClassesWithBaseClassAndPrefixShouldReturnCorrectClasses()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("fa fa-home", result);
    }

    [TestMethod]
    public void GetCssClassesWithBaseClassOnlyShouldReturnBaseClassAndName()
    {
        var iconInfo = new BitIconInfo("home", "material-icons", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("material-icons home", result);
    }

    [TestMethod]
    public void GetCssClassesWithPrefixOnlyShouldReturnPrefixedName()
    {
        var iconInfo = new BitIconInfo("home", "", "icon-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icon-home", result);
    }

    [TestMethod]
    public void GetCssClassesWithEmptyBaseClassAndPrefixShouldReturnNameOnly()
    {
        var iconInfo = new BitIconInfo("home", "", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("home", result);
    }

    // Factory Method Tests

    [TestMethod]
    public void CssShouldCreateIconInfoWithEmptyBaseClassAndPrefix()
    {
        var iconInfo = BitIconInfo.Css("fa-solid fa-home");

        Assert.AreEqual("fa-solid fa-home", iconInfo.Name);
        Assert.AreEqual("", iconInfo.BaseClass);
        Assert.AreEqual("", iconInfo.Prefix);
        Assert.AreEqual("fa-solid fa-home", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void FontAwesomeShouldCreateIconInfoForFontAwesome6()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-home");

        Assert.AreEqual("fa-solid fa-home", iconInfo.Name);
        Assert.AreEqual("", iconInfo.BaseClass);
        Assert.AreEqual("", iconInfo.Prefix);
        Assert.AreEqual("fa-solid fa-home", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void FontAwesomeShouldHandleBrandIcons()
    {
        var iconInfo = BitIconInfo.Fa("fa-brands fa-github");

        Assert.AreEqual("fa-brands fa-github", iconInfo.Name);
        Assert.AreEqual("fa-brands fa-github", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void BootstrapShouldCreateBootstrapIconInfo()
    {
        var iconInfo = BitIconInfo.Bi("check-circle");

        Assert.AreEqual("check-circle", iconInfo.Name);
        Assert.AreEqual("bi", iconInfo.BaseClass);
        Assert.AreEqual("bi-", iconInfo.Prefix);
        Assert.AreEqual("bi bi-check-circle", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void BootstrapShouldHandleVariousIconNames()
    {
        var iconInfo = BitIconInfo.Bi("house");

        Assert.AreEqual("house", iconInfo.Name);
        Assert.AreEqual("bi", iconInfo.BaseClass);
        Assert.AreEqual("bi-", iconInfo.Prefix);
        Assert.AreEqual("bi bi-house", iconInfo.GetCssClasses());
    }

    // Implicit Conversion Tests

    [TestMethod]
    public void ImplicitConversionFromStringShouldCreateIconInfo()
    {
        BitIconInfo iconInfo = "home";

        Assert.AreEqual("home", iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
    }

    [TestMethod]
    public void ImplicitConversionFromNullStringShouldReturnNull()
    {
        string? nullString = null;
        BitIconInfo? iconInfo = nullString;

        Assert.IsNull(iconInfo);
    }

    [TestMethod]
    public void ImplicitConversionToStringShouldReturnName()
    {
        var iconInfo = new BitIconInfo("home", "fa", "fa-");

        string? result = iconInfo;

        Assert.AreEqual("home", result);
    }

    [TestMethod]
    public void ImplicitConversionFromNullIconInfoShouldReturnNull()
    {
        BitIconInfo? iconInfo = null;

        string? result = iconInfo;

        Assert.IsNull(result);
    }

    // CSS class generation tests

    [TestMethod]
    public void GetCssClassesFontAwesome6SolidIconShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-user");

        Assert.AreEqual("fa-solid fa-user", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesFontAwesome6RegularIconShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-regular fa-heart");

        Assert.AreEqual("fa-regular fa-heart", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesFontAwesome6BrandIconShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Fa("fa-brands fa-github");

        Assert.AreEqual("fa-brands fa-github", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesBootstrapShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Bi("arrow-right");

        Assert.AreEqual("bi bi-arrow-right", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesCustomCssShouldGenerateCorrectClasses()
    {
        var iconInfo = BitIconInfo.Css("my-custom-icon-class");

        Assert.AreEqual("my-custom-icon-class", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesCustomIconWithConstructorShouldGenerateCorrectClasses()
    {
        var iconInfo = new BitIconInfo("custom-icon", "my-icons", "ico-");

        Assert.AreEqual("my-icons ico-custom-icon", iconInfo.GetCssClasses());
    }

    // Edge Cases

    [TestMethod]
    public void GetCssClassesWithEmptyBaseClassShouldHandleCorrectly()
    {
        var iconInfo = new BitIconInfo("home", "", "icon-");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icon-home", result);
    }

    [TestMethod]
    public void GetCssClassesWithEmptyPrefixShouldHandleCorrectly()
    {
        var iconInfo = new BitIconInfo("home", "icons", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("icons home", result);
    }

    [TestMethod]
    public void GetCssClassesWithAllEmptyStringsShouldReturnEmptyString()
    {
        var iconInfo = new BitIconInfo("", "", "");

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetCssClassesWithSpecialCharactersInNameShouldHandleCorrectly()
    {
        var iconInfo = BitIconInfo.Fa("fa-solid fa-arrow-circle-right");

        Assert.AreEqual("fa-solid fa-arrow-circle-right", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void GetCssClassesBootstrapWithHyphenatedNameShouldHandleCorrectly()
    {
        var iconInfo = BitIconInfo.Bi("check-circle-fill");

        Assert.AreEqual("bi bi-check-circle-fill", iconInfo.GetCssClasses());
    }

    [TestMethod]
    public void DefaultConstructorShouldCreateEmptyIconInfo()
    {
        var iconInfo = new BitIconInfo();

        Assert.IsNull(iconInfo.Name);
        Assert.IsNull(iconInfo.BaseClass);
        Assert.IsNull(iconInfo.Prefix);
    }

    [TestMethod]
    public void GetCssClassesWithNullNameShouldReturnEmptyString()
    {
        var iconInfo = new BitIconInfo();

        var result = iconInfo.GetCssClasses();

        Assert.AreEqual("", result);
    }
}
