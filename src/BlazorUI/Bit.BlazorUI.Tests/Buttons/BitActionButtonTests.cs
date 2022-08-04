using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitActionButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true, BitIconName.AddFriend, "title"),
        DataRow(Visual.Fluent, false, BitIconName.AddFriend, "title"),

        DataRow(Visual.Cupertino, true, BitIconName.AddFriend, "title"),
        DataRow(Visual.Cupertino, false, BitIconName.AddFriend, "title"),

        DataRow(Visual.Material, true, BitIconName.AddFriend, "title"),
        DataRow(Visual.Material, false, BitIconName.AddFriend, "title"),
    ]
    public void BitActionButtonTest(Visual visual, bool isEnabled, BitIconName iconName, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-act-btn");
        var bitIconITag = com.Find(".bit-act-btn > span > i");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitButton.ClassList.Contains($"bit-act-btn-{isEnabledClass}-{visualClass}"));

        Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

        Assert.AreEqual(bitButton.GetAttribute("title"), title);

        bitButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
       DataRow(true, false, false),
       DataRow(true, true, false),
       DataRow(false, false, true),
       DataRow(false, true, false),
   ]
    public void BitActionButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-act-btn");

        var hasTabindexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(hasTabindexAttr, expectedResult);

        if (hasTabindexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }

    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitActionButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = com.Find(".bit-act-btn");

        Assert.IsTrue(bitButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitActionButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = com.Find(".bit-act-btn");

        Assert.IsTrue(bitButton.GetAttribute("aria-label").Contains(ariaLabel));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitActionButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitButton = com.Find(".bit-act-btn");

        Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);
    }

    [DataTestMethod,
        DataRow("", true),
        DataRow("bing.com", true),
        DataRow("bing.com", false)
    ]
    public void BitActionButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitActionButton = component.Find(".bit-act-btn");
        var tagName = bitActionButton.TagName;
        var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(BitButtonType.Button),
        DataRow(BitButtonType.Submit),
        DataRow(BitButtonType.Reset)
    ]
    public void BitActionButtonTypeOfButtonTest(BitButtonType buttonType)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitActionButton = component.Find(".bit-act-btn");

        var buttonTypeName = buttonType == BitButtonType.Button ? "button" : buttonType == BitButtonType.Submit ? "submit" : "reset";
        Assert.AreEqual(buttonTypeName, bitActionButton.GetAttribute("type"));
    }
    
    [TestMethod]
    public void BitActionButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });
        
        var bitButton = com.Find(".bit-act-btn");

        Assert.AreEqual("submit", bitButton.GetAttribute("type"));
    }
    
    [TestMethod]
    public void BitActionButtonButtonStateNotOverridenInEditContextTest()
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });
        
        var bitButton = com.Find(".bit-act-btn");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}