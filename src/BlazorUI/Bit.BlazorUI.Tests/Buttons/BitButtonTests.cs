using System;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary, "title"),
        DataRow(true, BitButtonStyle.Standard, "title"),
        DataRow(false, BitButtonStyle.Primary, "title"),
        DataRow(false, BitButtonStyle.Standard, "title")
    ]
    public void BitButtonTest(bool isEnabled, BitButtonStyle style, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-btn");

        if (isEnabled)
        {
            Assert.IsFalse(bitButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitButton.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(bitButton.GetAttribute("title"), title);

        bitButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
        DataRow(BitButtonSize.Small),
        DataRow(BitButtonSize.Medium),
        DataRow(BitButtonSize.Large)
    ]
    public void BitButtonSizeTest(BitButtonSize size)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.ButtonSize, size);
        });

        var bitButton = com.Find(".bit-btn");
        var sizeClass = size switch
        {
            BitButtonSize.Small => "small",
            BitButtonSize.Medium => "medium",
            BitButtonSize.Large => "large",
            _ => throw new NotSupportedException()
        };

        Assert.IsTrue(bitButton.ClassList.Contains(sizeClass));
    }

    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary, false, false),
        DataRow(true, BitButtonStyle.Standard, true, false),
        DataRow(false, BitButtonStyle.Primary, false, true),
        DataRow(false, BitButtonStyle.Standard, true, false),
    ]
    public void BitButtonDisabledFocusTest(bool isEnabled, BitButtonStyle style, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-btn");

        var hasTabIndexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(hasTabIndexAttr, expectedResult);

        if (hasTabIndexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }
    }

    [DataTestMethod,
         DataRow(true, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(true, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(false, BitButtonStyle.Primary, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(false, BitButtonStyle.Standard, "https://github.com/bitfoundation", "bit", "_blank")
    ]
    public void BitAnchorButtonTest(bool isEnabled, BitButtonStyle style, string href, string title, string target)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.Target, target);
        });

        var bitButton = com.Find(".bit-btn");

        if (isEnabled)
        {
            Assert.AreEqual(bitButton.GetAttribute("target"), target);
            Assert.IsTrue(bitButton.HasAttribute("href"));
            Assert.IsFalse(bitButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitButton.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(bitButton.GetAttribute("title"), title);
    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.HasAttribute("aria-label"));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitButton>(parameters =>
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
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitButton = com.Find(".bit-btn");

        var buttonTypeName = buttonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => throw new NotSupportedException(),
        };

        Assert.AreEqual(buttonTypeName, bitButton.GetAttribute("type"));
    }

    [TestMethod]
    public void BitButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("submit", bitButton.GetAttribute("type"));
    }

    [TestMethod]
    public void BitButtonButtonStateNotOverriddenInEditContextTest()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}
