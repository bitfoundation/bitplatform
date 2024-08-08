using System;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, BitVariant.Fill, "title"),
        DataRow(true, BitVariant.Outline, "title"),
        DataRow(false, BitVariant.Fill, "title"),
        DataRow(false, BitVariant.Outline, "title")
    ]
    public void BitButtonTest(bool isEnabled, BitVariant variant, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
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

        if (variant == BitVariant.Fill)
        {
            Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-fil"));
            Assert.IsFalse(bitButton.ClassList.Contains("bit-btn-otl"));
        }

        if (variant == BitVariant.Outline)
        {
            Assert.IsFalse(bitButton.ClassList.Contains("bit-btn-fil"));
            Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-otl"));
        }

        Assert.AreEqual(bitButton.GetAttribute("title"), title);

        bitButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
        DataRow(true, BitVariant.Fill, false, false),
        DataRow(true, BitVariant.Outline, true, false),
        DataRow(false, BitVariant.Fill, false, true),
        DataRow(false, BitVariant.Outline, true, false),
    ]
    public void BitButtonDisabledFocusTest(bool isEnabled, BitVariant variant, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
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
         DataRow(true, BitVariant.Fill, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(true, BitVariant.Outline, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(false, BitVariant.Fill, "https://github.com/bitfoundation", "bit", "_blank"),
         DataRow(false, BitVariant.Outline, "https://github.com/bitfoundation", "bit", "_blank")
    ]
    public void BitAnchorButtonTest(bool isEnabled, BitVariant variant, string href, string title, string target)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
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

    [DataTestMethod,
         DataRow(BitColor.Info),
         DataRow(BitColor.Success),
         DataRow(BitColor.Warning),
         DataRow(BitColor.SevereWarning),
         DataRow(BitColor.Error),
         DataRow(null),
    ]
    [TestMethod]
    public void BitColorOfButtonTest(BitColor? color)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        var bitButton = com.Find(".bit-btn");

        var colorClassName = color switch
        {
            BitColor.Info => "bit-btn-inf",
            BitColor.Success => "bit-btn-suc",
            BitColor.Warning => "bit-btn-wrn",
            BitColor.SevereWarning => "bit-btn-swr",
            BitColor.Error => "bit-btn-err",
            _ => "bit-btn-pri"
        };

        if (color.HasValue)
        {
            Assert.IsTrue(bitButton.ClassList.Contains(colorClassName));
        }
        else
        {
            Assert.AreEqual(5, bitButton.ClassList.Length);
        }
    }

    [DataTestMethod,
         DataRow(BitSize.Small),
         DataRow(BitSize.Medium),
         DataRow(BitSize.Large),
         DataRow(null)
    ]
    [TestMethod]
    public void BitSizeOfButtonTest(BitSize? size)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.Size, size.Value);
            }
        });

        var bitButton = com.Find(".bit-btn");

        var sizeClassName = size switch
        {
            BitSize.Small => "bit-btn-sm",
            BitSize.Medium => "bit-btn-md",
            BitSize.Large => "bit-btn-lg",
            _ => "bit-btn-md"
        };

        if (size.HasValue)
        {
            Assert.IsTrue(bitButton.ClassList.Contains(sizeClassName));
        }
        else
        {
            Assert.AreEqual(5, bitButton.ClassList.Length);
        }
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitButtonLoadingContentTest(bool isLoading)
    {
        const string textContent = "Hi";

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.PrimaryTemplate, textContent);
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var bitButton = com.Find(".bit-btn");

        if (isLoading)
        {
            Assert.IsTrue(bitButton.FirstElementChild.ClassList.Contains("bit-btn-ldg"));
        }
        else
        {
            Assert.AreEqual(textContent, bitButton.TextContent);
        }
    }

    [DataTestMethod,
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),

        DataRow(BitLabelPosition.End),
        DataRow(BitLabelPosition.End),
        DataRow(BitLabelPosition.End),
        DataRow(BitLabelPosition.End),

        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),

        DataRow(BitLabelPosition.Start),
        DataRow(BitLabelPosition.Start),
        DataRow(BitLabelPosition.Start),
        DataRow(BitLabelPosition.Start),

        DataRow(null),
    ]
    public void BitButtonLoaderTest(BitLabelPosition? labelPosition)
    {
        const string loadingLabel = "I'm Loading Label";

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, loadingLabel);
            if (labelPosition.HasValue)
            {
                parameters.Add(p => p.LoadingLabelPosition, labelPosition.Value);
            }
        });

        var bitButton = com.Find(".bit-btn");

        var labelPositionClass = labelPosition switch
        {
            BitLabelPosition.Top => "bit-btn-top",
            BitLabelPosition.End => "bit-btn-end",
            BitLabelPosition.Bottom => "bit-btn-btm",
            BitLabelPosition.Start => "bit-btn-srt",
            _ => "bit-btn-end"
        };

        Assert.AreEqual(loadingLabel, bitButton.LastElementChild.TextContent);

        Assert.IsTrue(bitButton.FirstElementChild.ClassList.Contains("bit-btn-ldg"));

        Assert.IsTrue(bitButton.FirstElementChild.ClassList.Contains(labelPositionClass));
    }

}
