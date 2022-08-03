using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Fluent, true, BitButtonStyle.Standard, "title"),
        DataRow(Visual.Fluent, false, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Fluent, false, BitButtonStyle.Standard, "title"),

        DataRow(Visual.Cupertino, true, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Cupertino, true, BitButtonStyle.Standard, "title"),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Standard, "title"),

        DataRow(Visual.Material, true, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Material, true, BitButtonStyle.Standard, "title"),
        DataRow(Visual.Material, false, BitButtonStyle.Primary, "title"),
        DataRow(Visual.Material, false, BitButtonStyle.Standard, "title"),
    ]
    public void BitButtonTest(Visual visual, bool isEnabled, BitButtonStyle style, string title)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Title, title);
        });

        var bitButton = com.Find(".bit-btn");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitButton.ClassList.Contains($"bit-btn-{isEnabledClass}-{visualClass}"));

        Assert.AreEqual(bitButton.GetAttribute("title"), title);

        bitButton.Click();

        Assert.AreEqual(isEnabled ? 1 : 0, com.Instance.CurrentCount);
    }

    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary, false, false),
        DataRow(true, BitButtonStyle.Standard, true, false),
        DataRow(false, BitButtonStyle.Primary, false, true),
        DataRow(false, BitButtonStyle.Standard, true, false),
    ]
    public void BitButtonDisabledFocusTest(bool isEnabled, BitButtonStyle style, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-btn");

        var hasTabindexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(hasTabindexAttr, expectedResult);

        if (hasTabindexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }
    }

    [DataTestMethod,
         DataRow(Visual.Fluent, true, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Fluent, true, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Fluent, false, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Fluent, false, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),

         DataRow(Visual.Cupertino, true, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Cupertino, true, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Cupertino, false, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Cupertino, false, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),

         DataRow(Visual.Material, true, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Material, true, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Material, false, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(Visual.Material, false, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
    ]
    public void BitAnchorButtonTest(Visual visual, bool isEnabled, BitButtonStyle style, string href, string title, string target)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.Target, target);
        });

        var bitButton = com.Find(".bit-btn");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        if (isEnabled)
        {
            Assert.IsTrue(bitButton.ClassList.Contains($"bit-btn-{visualClass}"));
            Assert.AreEqual(bitButton.GetAttribute("target"), target);
            Assert.IsTrue(bitButton.HasAttribute("href"));
        }
        else
        {
            Assert.IsTrue(bitButton.ClassList.Contains($"bit-btn-{isEnabledClass}-{visualClass}"));
        }

        Assert.AreEqual(bitButton.GetAttribute("title"), title);
    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.HasAttribute("aria-label"));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(expectedResult, bitButton.HasAttribute("aria-hidden"));
    }

    [DataTestMethod,
        DataRow(BitButtonType.Button),
        DataRow(BitButtonType.Submit),
        DataRow(BitButtonType.Reset)
    ]
    public void BitButtonTypeOfButtonTest(BitButtonType buttonType)
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitButton = com.Find(".bit-btn");

        var buttonTypeName = buttonType == BitButtonType.Button ? "button" : buttonType == BitButtonType.Submit ? "submit" : "reset";
        Assert.AreEqual(buttonTypeName, bitButton.GetAttribute("type"));
    }

    [TestMethod]
    public void BitButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });
        
        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("submit", bitButton.GetAttribute("type"));
    }
    
    [TestMethod]
    public void BitButtonButtonStateNotOverridenInEditContextTest()
    {
        var com = RenderComponent<BitButtonTest>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });
        
        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}
