using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons.Button;

[TestClass]
public class BitButtonTests : BunitTestContext
{
    [TestMethod,
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

    [TestMethod,
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
            Assert.IsTrue(bitButton?.GetAttribute("tabindex")?.Equals("-1"));
        }
    }

    [TestMethod,
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

    [TestMethod, DataRow("Detailed description")]
    public void BitButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = com.Find(".bit-btn");

        var descriptionElement = com.Find(".bit-btn-dsc");

        Assert.AreEqual(descriptionElement.Id, bitButton.GetAttribute("aria-describedby"));

        Assert.AreEqual(ariaDescription, descriptionElement.TextContent);
    }

    [TestMethod, DataRow("Detailed label")]
    public void BitButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.HasAttribute("aria-label"));
    }

    [TestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(expectedResult, bitButton.HasAttribute("aria-hidden"));
    }

    [TestMethod,
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

    [TestMethod,
         DataRow(BitColor.Info),
         DataRow(BitColor.Success),
         DataRow(BitColor.Warning),
         DataRow(BitColor.SevereWarning),
         DataRow(BitColor.Error),
         DataRow(null),
    ]
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

    [TestMethod,
         DataRow(BitSize.Small),
         DataRow(BitSize.Medium),
         DataRow(BitSize.Large),
         DataRow(null)
    ]
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

    [TestMethod,
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
            Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lda"));
            Assert.IsNotNull(com.Find(".bit-btn-ldg"));

            var hiddenContent = com.Find(".bit-btn-hcn");
            Assert.AreEqual(textContent, hiddenContent.TextContent.Trim());
        }
        else
        {
            Assert.IsFalse(bitButton.ClassList.Contains("bit-btn-lda"));
            Assert.AreEqual(textContent, bitButton.TextContent);
        }
    }

    [TestMethod,
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.End),
        DataRow(BitLabelPosition.Bottom),
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

        var labelPositionClass = labelPosition switch
        {
            BitLabelPosition.Top => "bit-btn-top",
            BitLabelPosition.End => "bit-btn-end",
            BitLabelPosition.Bottom => "bit-btn-btm",
            BitLabelPosition.Start => "bit-btn-srt",
            _ => "bit-btn-end"
        };

        var loadingContainer = com.Find(".bit-btn-ldg");

        Assert.AreEqual(loadingLabel, com.Find(".bit-btn-lbl").TextContent.Trim());

        Assert.IsTrue(loadingContainer.ClassList.Contains(labelPositionClass));
    }

    [TestMethod,
        DataRow(BitIconPosition.Start),
        DataRow(BitIconPosition.End),
        DataRow(null)
    ]
    public void BitButtonIconPositionClassTest(BitIconPosition? iconPosition)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            if (iconPosition.HasValue)
            {
                parameters.Add(p => p.IconPosition, iconPosition.Value);
            }
        });

        var bitButton = com.Find(".bit-btn");

        var expectedClassPresence = iconPosition == BitIconPosition.End;

        Assert.AreEqual(expectedClassPresence, bitButton.ClassList.Contains("bit-btn-eni"));
    }

    [TestMethod,
        DataRow("5"),
        DataRow("50"),
        DataRow(null),
    ]
    public void BitButtonTabIndexShouldRecoverAfterReEnable(string? tabIndex)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AllowDisabledFocus, false);
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("-1", bitButton.GetAttribute("tabindex"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
        });

        if (tabIndex is null)
        {
            Assert.IsFalse(bitButton.HasAttribute("tabindex"));
        }
        else
        {
            Assert.AreEqual(tabIndex, bitButton.GetAttribute("tabindex"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonFullWidthClassTest(bool fullWidth)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(fullWidth, bitButton.ClassList.Contains("bit-btn-flw"));
    }

    [TestMethod,
        DataRow(BitColor.Primary),
        DataRow(BitColor.Secondary),
        DataRow(BitColor.Tertiary),
        DataRow(BitColor.PrimaryBackground),
        DataRow(BitColor.SecondaryBackground),
        DataRow(BitColor.TertiaryBackground),
        DataRow(BitColor.PrimaryForeground),
        DataRow(BitColor.SecondaryForeground),
        DataRow(BitColor.TertiaryForeground),
        DataRow(BitColor.PrimaryBorder),
        DataRow(BitColor.SecondaryBorder),
        DataRow(BitColor.TertiaryBorder),
    ]
    public void BitButtonAllColorClassesTest(BitColor color)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var bitButton = com.Find(".bit-btn");

        var expectedClass = color switch
        {
            BitColor.Primary => "bit-btn-pri",
            BitColor.Secondary => "bit-btn-sec",
            BitColor.Tertiary => "bit-btn-ter",
            BitColor.Info => "bit-btn-inf",
            BitColor.Success => "bit-btn-suc",
            BitColor.Warning => "bit-btn-wrn",
            BitColor.SevereWarning => "bit-btn-swr",
            BitColor.Error => "bit-btn-err",
            BitColor.PrimaryBackground => "bit-btn-pbg",
            BitColor.SecondaryBackground => "bit-btn-sbg",
            BitColor.TertiaryBackground => "bit-btn-tbg",
            BitColor.PrimaryForeground => "bit-btn-pfg",
            BitColor.SecondaryForeground => "bit-btn-sfg",
            BitColor.TertiaryForeground => "bit-btn-tfg",
            BitColor.PrimaryBorder => "bit-btn-pbr",
            BitColor.SecondaryBorder => "bit-btn-sbr",
            BitColor.TertiaryBorder => "bit-btn-tbr",
            _ => "bit-btn-pri"
        };

        Assert.IsTrue(bitButton.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(null, true),
        DataRow(null, false),
        DataRow("", true),
        DataRow("", false),
        DataRow("href", true),
        DataRow("href", false)
    ]
    public void BitButtonShouldRenderExpectedElementBasedOnHref(string? href, bool isEnabled)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitButton = com.Find(".bit-btn");

        var expectedTag = href.HasValue() ? "a" : "button";

        Assert.AreEqual(expectedTag, bitButton.TagName, ignoreCase: true);
    }

    [TestMethod,
        DataRow("https://bitplatform.dev", BitLinkRels.NoOpener | BitLinkRels.NoReferrer, "noopener noreferrer"),
        DataRow("#section", BitLinkRels.NoOpener | BitLinkRels.NoReferrer, null)
    ]
    public void BitButtonRelAttributeShouldFollowHrefRules(string href, BitLinkRels rel, string? expectedRel)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Rel, rel);
        });

        var bitButton = com.Find(".bit-btn");

        var hasRelAttribute = bitButton.HasAttribute("rel");

        Assert.AreEqual(string.IsNullOrEmpty(expectedRel) is false, hasRelAttribute);

        if (expectedRel is not null)
        {
            Assert.AreEqual(expectedRel, bitButton.GetAttribute("rel"));
        }
    }

    [TestMethod]
    public void BitButtonDynamicParameterUpdateShouldRefreshMarkup()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IconName, "Emoji");
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsFalse(bitButton.ClassList.Contains("bit-dis"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(bitButton.ClassList.Contains("bit-dis"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonFixedColorClassTest(bool fixedColor)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.FixedColor, fixedColor);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(fixedColor, bitButton.ClassList.Contains("bit-btn-fxc"));
    }

    [TestMethod,
        DataRow(true, true, false, null),
        DataRow(true, false, false, null),
        DataRow(false, true, false, "true"),
        DataRow(false, false, true, "true")
    ]
    public void BitButtonDisabledSemanticsTest(bool isEnabled, bool allowDisabledFocus, bool expectedDisabledAttribute, string? expectedAriaDisabled)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(expectedDisabledAttribute, bitButton.HasAttribute("disabled"));

        Assert.AreEqual(expectedAriaDisabled, bitButton.GetAttribute("aria-disabled"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonAriaBusyTest(bool isLoading)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(isLoading ? "true" : null, bitButton.GetAttribute("aria-busy"));
    }

    [TestMethod,
        DataRow(false, false, null),
        DataRow(true, false, "true"),
        DataRow(true, true, null)
    ]
    public void BitButtonLoadingShouldBeReportedAsDisabledUnlessItIsReclickable(bool isLoading, bool reclickable, string? expectedAriaDisabled)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, isLoading);
            parameters.Add(p => p.Reclickable, reclickable);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(expectedAriaDisabled, bitButton.GetAttribute("aria-disabled"));

        // the class that stops the button pointing, and that keeps the hover and press rules off it, follows the same rule
        Assert.AreEqual(expectedAriaDisabled is not null, bitButton.ClassList.Contains("bit-btn-lnc"));
    }

    [TestMethod,
        DataRow("https://bitplatform.dev", "_blank", "noopener"),
        DataRow("https://bitplatform.dev", "_self", null),
        DataRow("https://bitplatform.dev", null, null)
    ]
    public void BitButtonAutoRelNoOpenerTest(string href, string? target, string? expectedRel)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Target, target);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(expectedRel, bitButton.GetAttribute("rel"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonAnchorShouldRenderLoadingAndRespectReclickable(bool reclickable)
    {
        var clicked = false;
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Reclickable, reclickable);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("a", bitButton.TagName, ignoreCase: true);

        Assert.IsFalse(bitButton.HasAttribute("href"));

        Assert.AreEqual("0", bitButton.GetAttribute("tabindex"));

        Assert.IsNotNull(com.Find(".bit-btn-ldg"));

        bitButton.Click();

        Assert.AreEqual(reclickable, clicked);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonLoadingShouldRespectReclickable(bool reclickable)
    {
        var clicked = false;
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Reclickable, reclickable);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        com.Find(".bit-btn").Click();

        Assert.AreEqual(reclickable, clicked);
    }

    [TestMethod]
    public void BitButtonShouldApplyHiddenContentClassStyles()
    {
        var hiddenContentClass = "hidden-content-class";
        var hiddenContentStyle = "opacity: 0.5;";

        var classes = new BitButtonClassStyles
        {
            HiddenContent = hiddenContentClass
        };

        var styles = new BitButtonClassStyles
        {
            HiddenContent = hiddenContentStyle
        };

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
            parameters.AddChildContent("Content");
        });

        var hiddenContent = com.Find(".bit-btn-hcn");

        Assert.IsTrue(hiddenContent.ClassList.Contains(hiddenContentClass));
        Assert.AreEqual(hiddenContentStyle, hiddenContent.GetAttribute("style"));
    }

    [TestMethod]
    public void BitButtonFormIdTest()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.FormId, "external-form");
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("external-form", bitButton.GetAttribute("form"));
    }

    [TestMethod]
    public void BitButtonDownloadTest()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev/report.pdf");
            parameters.Add(p => p.Download, "report.pdf");
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("report.pdf", bitButton.GetAttribute("download"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonAutoFocusTest(bool autoFocus)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, autoFocus);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(autoFocus, bitButton.HasAttribute("autofocus"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonNoWrapClassTest(bool noWrap)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
            parameters.AddChildContent("A very long label that does not fit");
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual(noWrap, bitButton.ClassList.Contains("bit-btn-nwr"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitButtonLoadingShouldOnlyBlockThePointerWhenItIsNotReclickable(bool reclickable)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Reclickable, reclickable);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lda"));
        Assert.AreEqual(reclickable is false, bitButton.ClassList.Contains("bit-btn-lnc"));
    }

    [TestMethod]
    public void BitButtonIconShouldBeHiddenFromAssistiveTechnologies()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji");
            parameters.AddChildContent("Label");
        });

        Assert.AreEqual("true", com.Find(".bit-btn-icn").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitButtonIconUrlShouldRenderAnEmptyAltAndBeHiddenFromAssistiveTechnologies()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconUrl, "/images/bit-logo.svg");
            parameters.AddChildContent("Label");
        });

        var img = com.Find(".bit-btn-icnu");

        Assert.AreEqual("/images/bit-logo.svg", img.GetAttribute("src"));
        Assert.AreEqual(string.Empty, img.GetAttribute("alt"));
        Assert.AreEqual("true", img.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitButtonLoadingLabelShouldBeAnnouncedFromALiveRegionAndKeptOutOfTheAccessibleName()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.LoadingLabel, "Saving...");
            parameters.AddChildContent("Save");
        });

        // the region exists from the first render, empty, so that the text arriving in it is a change it can announce
        var status = com.Find("span[role=status]");
        Assert.AreEqual(string.Empty, status.TextContent.Trim());

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, "Saving...");
            parameters.AddChildContent("Save");
        });

        Assert.AreEqual("Saving...", com.Find("span[role=status]").TextContent.Trim());

        // the visual half of the loading state stays out of the accessible name, which keeps saying "Save"
        Assert.AreEqual("true", com.Find(".bit-btn-ldg").GetAttribute("aria-hidden"));
        Assert.IsFalse(com.Find(".bit-btn-hcn").HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitButtonWithoutLoadingLabelShouldRenderNoLiveRegion()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.AddChildContent("Save");
        });

        Assert.AreEqual(0, com.FindAll("span[role=status]").Count);
    }

    [TestMethod]
    public void BitButtonAriaHiddenShouldTakeTheButtonOutOfTheTabOrderAndSuppressAutoFocus()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, true);
            parameters.Add(p => p.AutoFocus, true);
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("-1", bitButton.GetAttribute("tabindex"));
        Assert.IsFalse(bitButton.HasAttribute("autofocus"));
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, false)
    ]
    public void BitButtonDisabledShouldSuppressAutoFocusUnlessDisabledFocusIsAllowed(bool allowDisabledFocus, bool expectedAutoFocus)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        Assert.AreEqual(expectedAutoFocus, com.Find(".bit-btn").HasAttribute("autofocus"));
    }

    [TestMethod,
        DataRow(false, true, "link"),
        DataRow(true, true, "link"),
        DataRow(true, false, null)
    ]
    public void BitButtonAnchorShouldKeepTheLinkRoleWhenItLosesItsHref(bool isEnabled, bool isLoading, string? expectedRole)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsLoading, isLoading);
        });

        Assert.AreEqual(expectedRole, com.Find(".bit-btn").GetAttribute("role"));
    }

    [TestMethod]
    public void BitButtonShouldKeepTheSplattedAriaLabelAndForm()
    {
        var bitButton = RenderSplatted(new() { ["aria-label"] = "Splatted label", ["form"] = "splatted-form", ["autofocus"] = true }).Find(".bit-btn");

        Assert.AreEqual("Splatted label", bitButton.GetAttribute("aria-label"));
        Assert.AreEqual("splatted-form", bitButton.GetAttribute("form"));
        Assert.IsTrue(bitButton.HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitButtonParametersShouldWinOverTheSplattedAttributes()
    {
        var bitButton = RenderSplatted(new()
        {
            ["aria-label"] = "Splatted label",
            ["form"] = "splatted-form",
            [nameof(BitButton.AriaLabel)] = "Parameter label",
            [nameof(BitButton.FormId)] = "parameter-form"
        }).Find(".bit-btn");

        Assert.AreEqual("Parameter label", bitButton.GetAttribute("aria-label"));
        Assert.AreEqual("parameter-form", bitButton.GetAttribute("form"));
    }

    [TestMethod]
    public void BitButtonShouldMergeTheSplattedAriaDescribedByWithItsOwnDescription()
    {
        var com = RenderSplatted(new()
        {
            ["aria-describedby"] = "external-hint",
            [nameof(BitButton.AriaDescription)] = "The description"
        });

        var bitButton = com.Find(".bit-btn");
        var description = com.Find(".bit-btn-dsc");

        Assert.AreEqual($"external-hint {description.Id}", bitButton.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitButtonAriaHiddenShouldRemoveTheSplattedAutoFocus()
    {
        var bitButton = RenderSplatted(new() { ["autofocus"] = true, [nameof(BitButton.AriaHidden)] = true }).Find(".bit-btn");

        Assert.IsFalse(bitButton.HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitButtonShouldKeepTheSplattedTitleDirAndTabIndex()
    {
        var bitButton = RenderSplatted(new() { ["title"] = "Splatted title", ["dir"] = "rtl", ["tabindex"] = "3" }).Find(".bit-btn");

        Assert.AreEqual("Splatted title", bitButton.GetAttribute("title"));
        Assert.AreEqual("rtl", bitButton.GetAttribute("dir"));
        Assert.AreEqual("3", bitButton.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonAnchorShouldKeepTheSplattedTargetAndDownload()
    {
        var anchor = RenderSplatted(new()
        {
            [nameof(BitButton.Href)] = "https://bitplatform.dev",
            ["target"] = "_blank",
            ["download"] = "logo.svg"
        }).Find(".bit-btn");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("logo.svg", anchor.GetAttribute("download"));
        // a hand-written target opens the same new browsing context the parameter does, so it closes the same hole
        Assert.AreEqual("noopener", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonSplattedBlankTargetShouldNotDuplicateAnAlreadyAskedForRel()
    {
        var anchor = RenderSplatted(new()
        {
            [nameof(BitButton.Href)] = "https://bitplatform.dev",
            [nameof(BitButton.Rel)] = BitLinkRels.NoReferrer,
            ["target"] = "_blank"
        }).Find(".bit-btn");

        Assert.AreEqual("noreferrer", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonSplattedBlankTargetShouldNotAddARelToAHashOnlyHref()
    {
        var anchor = RenderSplatted(new()
        {
            [nameof(BitButton.Href)] = "#section",
            ["target"] = "_blank"
        }).Find(".bit-btn");

        Assert.IsFalse(anchor.HasAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonIconOnlyShouldKeepTheLabelAsTheAccessibleName()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Emoji");
            parameters.AddChildContent("Add an item");
        });

        Assert.IsEmpty(com.FindAll(".bit-btn-tcn"));
        Assert.AreEqual("Add an item", com.Find(".bit-btn-srl").TextContent.Trim());
    }

    [TestMethod]
    public void BitButtonIconOnlyWithoutAPrimaryLabelShouldFallBackToTheSecondaryText()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.SecondaryText, "secondary");
        });

        Assert.AreEqual("secondary", com.Find(".bit-btn-srl").TextContent.Trim());
    }

    [DataTestMethod,
        DataRow("aria-label"),
        DataRow("aria-labelledby")]
    public void BitButtonIconOnlyShouldDropTheScreenReaderLabelWhenTheButtonIsNamedByHand(string attribute)
    {
        var com = RenderSplatted(new()
        {
            [nameof(BitButton.IconOnly)] = true,
            [nameof(BitButton.IconName)] = "Emoji",
            [attribute] = "the-name"
        });

        Assert.IsEmpty(com.FindAll(".bit-btn-srl"));
    }

    [TestMethod]
    public void BitButtonIconOnlyShouldDropTheScreenReaderLabelWhenAnAriaLabelNamesIt()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.AriaLabel, "Add");
            parameters.AddChildContent("Add an item");
        });

        Assert.IsEmpty(com.FindAll(".bit-btn-srl"));
    }

    [TestMethod]
    public void BitButtonIconOnlyShouldKeepTheAccessibleNameWhileItIsLoading()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.IconName, "Emoji");
            parameters.AddChildContent("Add an item");
        });

        Assert.AreEqual("Add an item", com.Find(".bit-btn-hcn .bit-btn-srl").TextContent.Trim());
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitButtonRoundedClassTest(bool rounded)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Rounded, rounded);
        });

        Assert.AreEqual(rounded, com.Find(".bit-btn").ClassList.Contains("bit-btn-rnd"));
    }

    // The attributes a page writes by hand reach the component the way @attributes sends them, which is the
    // only path a lowercase name matching a parameter of the component can take.
    private IRenderedComponent<BitButton> RenderSplatted(Dictionary<string, object> attributes)
    {
        return Context.Render<BitButton>(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddMultipleAttributes(1, attributes);
            builder.CloseComponent();
        });
    }

    [TestMethod]
    public async Task BitButtonLoadingDelayShouldDeferTheSpinnerButNotTheState()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingDelay, 150);
            parameters.AddChildContent("Save");
        });

        // the state is immediate - the click is already blocked and aria-busy is already rendered
        Assert.AreEqual("true", com.Find(".bit-btn").GetAttribute("aria-busy"));
        Assert.AreEqual(0, com.FindAll(".bit-btn-spn").Count);

        await Task.Delay(400);

        com.WaitForAssertion(() => Assert.AreEqual(1, com.FindAll(".bit-btn-spn").Count));
    }

    [TestMethod]
    public async Task BitButtonAutoLoadingShouldLeaveTheLoadingStateWhenTheHandlerThrows()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.OnClick, EventCallback.Factory.Create<bool>(this, () => throw new InvalidOperationException("boom")));
            parameters.AddChildContent("Save");
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => com.Find(".bit-btn").ClickAsync(new()));

        Assert.IsFalse(com.Instance.IsLoading);
        Assert.AreEqual(0, com.FindAll(".bit-btn-spn").Count);
    }

    [TestMethod]
    public void BitButtonFloatOffsetShouldWriteThePublicCssVariable()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
            parameters.Add(p => p.FloatOffset, "2rem");
        });

        StringAssert.Contains(com.Find(".bit-btn").GetAttribute("style"), "--bit-Button-float-offset:2rem");
    }

    [TestMethod]
    public void BitButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitButton)}", BitButtonParams.ParamName);
    }

    [TestMethod]
    public void BitButtonParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitButtonParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitButtonParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitButtonShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Variant = BitVariant.Outline,
                Title = "Cascaded Title",
                FullWidth = true,
                NoWrap = true
            }
        };

        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.CloseComponent();
            });
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-suc"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lg"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-otl"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-flw"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-nwr"));
        Assert.AreEqual("Cascaded Title", bitButton.GetAttribute("title"));
    }

    [TestMethod]
    public void BitButtonDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Title = "Cascaded Title"
            }
        };

        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.AddAttribute(1, nameof(BitButton.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitButton.Title), "Direct Title");
                builder.CloseComponent();
            });
        });

        var bitButton = com.Find(".bit-btn");

        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-err"));
        Assert.AreEqual("Direct Title", bitButton.GetAttribute("title"));

        // the size that was not written by hand still comes from the cascading parameters
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lg"));
    }

    [TestMethod]
    public void BitButtonParamsShouldResolveTheRelFromTheCascadedHrefAndTarget()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonParams
            {
                Href = "https://bitplatform.dev",
                Target = "_blank"
            }
        };

        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.CloseComponent();
            });
        });

        var bitButton = com.Find(".bit-btn");

        Assert.AreEqual("a", bitButton.TagName, ignoreCase: true);
        Assert.AreEqual("noopener", bitButton.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonShouldNotAddNoOpenerWhenNoReferrerIsAlreadyAskedFor()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Rel, BitLinkRels.NoReferrer);
        });

        Assert.AreEqual("noreferrer", com.Find(".bit-btn").GetAttribute("rel"));
    }

    [TestMethod,
        DataRow("Emoji"),
        DataRow("Save")
    ]
    public void BitButtonIconNameShouldRenderTheBuiltInGlyph(string iconName)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = com.Find(".bit-btn-icn");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitButtonIconInfoHelpersShouldRenderTheExternalLibrarysOwnClasses()
    {
        var css = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-heart"));
        });

        Assert.IsTrue(css.Find(".bit-btn-icn").ClassList.Contains("fa-solid"));
        Assert.IsTrue(css.Find(".bit-btn-icn").ClassList.Contains("fa-heart"));

        var fa = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid rocket"));
        });

        Assert.IsTrue(fa.Find(".bit-btn-icn").ClassList.Contains("fa-solid"));
        Assert.IsTrue(fa.Find(".bit-btn-icn").ClassList.Contains("fa-rocket"));

        var bi = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Bi("github"));
        });

        Assert.IsTrue(bi.Find(".bit-btn-icn").ClassList.Contains("bi"));
        Assert.IsTrue(bi.Find(".bit-btn-icn").ClassList.Contains("bi-github"));
    }

    [TestMethod]
    public void BitButtonIconShouldTakePrecedenceOverIconNameAndIconUrl()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid house"));
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.IconUrl, "/images/bit-logo.svg");
        });

        var icon = com.Find(".bit-btn-icn");

        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Emoji"));
        Assert.IsEmpty(com.FindAll(".bit-btn-icnu"));
    }

    [TestMethod]
    public void BitButtonIconNameShouldTakePrecedenceOverIconUrl()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.IconUrl, "/images/bit-logo.svg");
        });

        Assert.HasCount(1, com.FindAll(".bit-btn-icn"));
        Assert.IsEmpty(com.FindAll(".bit-btn-icnu"));
    }

    [TestMethod]
    public void BitButtonIconUrlShouldRenderAnImageThatKeepsTheIconClassStyles()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconUrl, "/images/bit-logo.svg");
            parameters.Add(p => p.Classes, new BitButtonClassStyles { Icon = "custom-icon" });
            parameters.AddChildContent("Download");
        });

        var image = com.Find(".bit-btn-icnu");

        Assert.AreEqual("img", image.TagName, ignoreCase: true);
        Assert.AreEqual("/images/bit-logo.svg", image.GetAttribute("src"));
        Assert.IsTrue(image.ClassList.Contains("custom-icon"));
    }

    [TestMethod]
    public void BitButtonIconOnlyShouldDropTheTextAndSquareTheBox()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.SecondaryText, "secondary");
            parameters.AddChildContent("Primary");
        });

        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-ntx"));
        Assert.IsEmpty(com.FindAll(".bit-btn-tcn"));
        Assert.HasCount(1, com.FindAll(".bit-btn-icn"));
    }

    [TestMethod]
    public void BitButtonWithoutAnyTextShouldBeSquaredOffLikeAnIconButton()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji");
        });

        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-ntx"));
    }

    [TestMethod]
    public void BitButtonSecondaryTextShouldRenderASecondLineAndTopAlignTheIcon()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji");
            parameters.Add(p => p.SecondaryText, "secondary");
            parameters.AddChildContent("Primary");
        });

        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-hsc"));
        Assert.AreEqual("Primary", com.Find(".bit-btn-prt").TextContent.Trim());
        Assert.AreEqual("secondary", com.Find(".bit-btn-sct").TextContent.Trim());
    }

    [TestMethod]
    public void BitButtonTemplatesShouldReplaceEachLineOfText()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.PrimaryTemplate, "<span class=\"primary-template\">P</span>");
            parameters.Add(p => p.SecondaryTemplate, "<span class=\"secondary-template\">S</span>");
        });

        Assert.HasCount(1, com.FindAll(".bit-btn-prt .primary-template"));
        Assert.HasCount(1, com.FindAll(".bit-btn-sct .secondary-template"));
        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-hsc"));
    }

    [TestMethod]
    public void BitButtonPrimaryTemplateShouldWinOverTheChildContent()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.AddChildContent("child content");
            parameters.Add(p => p.PrimaryTemplate, "<span>template</span>");
        });

        Assert.AreEqual("template", com.Find(".bit-btn-prt").TextContent.Trim());
    }

    [TestMethod]
    public void BitButtonLoadingTemplateShouldReplaceTheSpinnerWhileTheContentStillHoldsTheSize()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, "Wait...");
            parameters.Add(p => p.LoadingTemplate, "<span class=\"custom-loading\">Wait...</span>");
            parameters.AddChildContent("Click me");
        });

        Assert.HasCount(1, com.FindAll(".custom-loading"));
        Assert.IsEmpty(com.FindAll(".bit-btn-spn"));
        Assert.HasCount(1, com.FindAll(".bit-btn-hcn"));

        // The template is the visual half of the loading state, like the spinner it replaces, so it is hidden
        // from assistive technologies: the content underneath it is what keeps the button's accessible name.
        Assert.AreEqual("true", com.Find(".bit-btn-ldg").GetAttribute("aria-hidden"));
        Assert.HasCount(1, com.FindAll(".bit-btn-ldg .custom-loading"));
    }

    [TestMethod,
        DataRow(false, 1),
        DataRow(true, 2)
    ]
    public async Task BitButtonAutoLoadingShouldGuardAgainstDoubleClicks(bool reclickable, int expectedClickCount)
    {
        var clickCount = 0;
        var tcs = new TaskCompletionSource();

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.Reclickable, reclickable);
            parameters.Add(p => p.OnClick, (bool _) =>
            {
                clickCount++;
                return tcs.Task;
            });
        });

        var button = com.Find(".bit-btn");

        var firstClick = button.ClickAsync(new MouseEventArgs());

        com.WaitForAssertion(() => Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-lda")));

        var secondClick = button.ClickAsync(new MouseEventArgs());

        tcs.SetResult();
        await firstClick;
        await secondClick;

        Assert.AreEqual(expectedClickCount, clickCount);
    }

    [TestMethod]
    public async Task BitButtonAutoLoadingShouldNotifyIsLoadingChanged()
    {
        var loadingStates = new List<bool>();
        var tcs = new TaskCompletionSource();

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.IsLoadingChanged, (bool value) => loadingStates.Add(value));
            parameters.Add(p => p.OnClick, (bool _) => tcs.Task);
        });

        var clickTask = com.Find(".bit-btn").ClickAsync(new MouseEventArgs());

        tcs.SetResult();
        await clickTask;

        CollectionAssert.AreEqual(new List<bool> { true, false }, loadingStates);
    }

    [TestMethod]
    public void BitButtonOnClickShouldReportTheLoadingStateTheClickArrivedIn()
    {
        var reported = new List<bool>();

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Reclickable, true);
            parameters.Add(p => p.OnClick, (bool loading) => reported.Add(loading));
        });

        com.Find(".bit-btn").Click();

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
        });

        com.Find(".bit-btn").Click();

        CollectionAssert.AreEqual(new List<bool> { false, true }, reported);
    }

    [TestMethod,
        DataRow(BitPosition.TopLeft, "bit-btn-tlf"),
        DataRow(BitPosition.TopCenter, "bit-btn-tcr"),
        DataRow(BitPosition.TopRight, "bit-btn-trg"),
        DataRow(BitPosition.TopStart, "bit-btn-tst"),
        DataRow(BitPosition.TopEnd, "bit-btn-ten"),
        DataRow(BitPosition.CenterLeft, "bit-btn-clf"),
        DataRow(BitPosition.Center, "bit-btn-ctr"),
        DataRow(BitPosition.CenterRight, "bit-btn-crg"),
        DataRow(BitPosition.CenterStart, "bit-btn-cst"),
        DataRow(BitPosition.CenterEnd, "bit-btn-cen"),
        DataRow(BitPosition.BottomLeft, "bit-btn-blf"),
        DataRow(BitPosition.BottomCenter, "bit-btn-bcr"),
        DataRow(BitPosition.BottomRight, "bit-btn-brg"),
        DataRow(BitPosition.BottomStart, "bit-btn-bst"),
        DataRow(BitPosition.BottomEnd, "bit-btn-ben")
    ]
    public void BitButtonFloatPositionClassTest(BitPosition position, string expectedClass)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
            parameters.Add(p => p.FloatPosition, position);
        });

        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitButtonFloatShouldPickItsAnchoringModeAndIgnoreThePositionWhenItIsNotFloating()
    {
        var viewport = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
        });

        Assert.IsTrue(viewport.Find(".bit-btn").ClassList.Contains("bit-btn-ffx"));

        // FloatAbsolute outranks Float: a button asked for both is pinned to its container.
        var container = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
            parameters.Add(p => p.FloatAbsolute, true);
        });

        Assert.IsTrue(container.Find(".bit-btn").ClassList.Contains("bit-btn-fab"));
        Assert.IsFalse(container.Find(".bit-btn").ClassList.Contains("bit-btn-ffx"));

        var stationary = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.FloatPosition, BitPosition.TopLeft);
        });

        Assert.IsFalse(stationary.Find(".bit-btn").ClassList.Contains("bit-btn-tlf"));
    }

    [TestMethod]
    public void BitButtonDraggableShouldOnlyEnableTheDragScriptWhileTheButtonIsFloating()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Draggable, true);
        });

        // Dragging means nothing for a button that is laid out with the rest of the page.
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Draggables.enable"));
        Assert.IsFalse(com.Find(".bit-btn").ClassList.Contains("bit-btn-drg"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.Float, true);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Draggables.enable"));
        Assert.IsTrue(com.Find(".bit-btn").ClassList.Contains("bit-btn-drg"));

        // A re-render that changes nothing about the dragging leaves the listeners where they are.
        com.Render(parameters =>
        {
            parameters.Add(p => p.Title, "Move me");
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Draggables.enable"));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Draggables.disable"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.Draggable, false);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Draggables.disable"));
        Assert.IsFalse(com.Find(".bit-btn").ClassList.Contains("bit-btn-drg"));
    }

    [TestMethod]
    public void BitButtonDraggableShouldNameTheKeysThatMoveItWithoutADrag()
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
            parameters.Add(p => p.Draggable, true);
        });

        Assert.AreEqual("ArrowUp ArrowDown ArrowLeft ArrowRight", com.Find(".bit-btn").GetAttribute("aria-keyshortcuts"));

        // Nothing to announce on a button that cannot be moved.
        var stationary = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Draggable, true);
        });

        Assert.IsFalse(stationary.Find(".bit-btn").HasAttribute("aria-keyshortcuts"));
    }

    /// <summary>
    /// The drag script invokes all three callbacks with the pointer's position. The interop dispatcher
    /// matches a call to a method by the number of arguments and throws when they differ, and the script
    /// swallows what it throws - so a callback declared without the coordinates is never reached at all,
    /// and the drag it is there to report goes unnoticed.
    /// </summary>
    [TestMethod,
        DataRow("_OnDragStart"),
        DataRow("_OnDragging"),
        DataRow("_OnDragEnd")
    ]
    public void BitButtonDragCallbacksShouldTakeTheCoordinatesTheScriptSendsThem(string name)
    {
        var method = typeof(BitButton).GetMethod(name);

        Assert.IsNotNull(method, $"BitButton has no {name} for the drag script to call.");
        Assert.HasCount(2, method!.GetParameters());
    }

    [TestMethod]
    public async Task BitButtonShouldSwallowTheClickThatEndsADragButNotTheNextPress()
    {
        var clicks = 0;

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Float, true);
            parameters.Add(p => p.Draggable, true);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        await com.Instance._OnDragging(10, 10);

        com.Find(".bit-btn").Click();

        Assert.AreEqual(0, clicks);

        com.Find(".bit-btn").Click();

        Assert.AreEqual(1, clicks);
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev")
    ]
    public async Task BitButtonFocusAsyncShouldFocusTheRootElement(string? href)
    {
        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.AddChildContent("Focus me");
        });

        await com.Instance.FocusAsync();

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitButtonShouldRespectCascadingDir()
    {
        var com = RenderComponent<CascadingValue<BitDir>>(parameters =>
        {
            parameters.Add(p => p.Value, BitDir.Rtl);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("rtl", com.Find(".bit-btn").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitButtonInsideAnEditFormShouldBeItsSubmitButton()
    {
        var submitted = false;

        var com = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, new object());
            parameters.Add(p => p.OnValidSubmit, _ => submitted = true);
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.CloseComponent();
            }));
        });

        Assert.AreEqual("submit", com.Find(".bit-btn").GetAttribute("type"));

        com.Find("form").Submit();

        Assert.IsTrue(submitted);
    }

    [TestMethod]
    public void BitButtonParamsUpdateParametersShouldSetEveryPropertyItCarries()
    {
        var classes = new BitButtonClassStyles { Root = "cascaded-root" };
        var styles = new BitButtonClassStyles { Root = "color: red;" };

        var @params = new BitButtonParams
        {
            AllowDisabledFocus = false,
            AriaDescription = "Test description",
            AriaHidden = true,
            AutoFocus = true,
            AutoLoading = true,
            ButtonType = BitButtonType.Reset,
            Classes = classes,
            Color = BitColor.Warning,
            Download = "report.pdf",
            Draggable = true,
            FixedColor = true,
            Float = true,
            FloatAbsolute = true,
            FloatOffset = "2rem",
            FloatPosition = BitPosition.TopLeft,
            FormId = "my-form",
            FullWidth = true,
            Href = "https://bitplatform.dev",
            IconName = "Share",
            IconOnly = true,
            IconPosition = BitIconPosition.End,
            IconUrl = "/images/icon.svg",
            IsLoading = true,
            LoadingDelay = 250,
            LoadingLabel = "Sending...",
            LoadingLabelPosition = BitLabelPosition.Top,
            NoWrap = true,
            Reclickable = true,
            Rel = BitLinkRels.NoOpener,
            Rounded = true,
            SecondaryText = "secondary",
            Size = BitSize.Small,
            StopPropagation = true,
            Styles = styles,
            Target = "_blank",
            Title = "Test Title",
            Variant = BitVariant.Outline,
            AriaLabel = "Test Label",
            IsEnabled = false,
            TabIndex = "5"
        };

        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.CloseComponent();
            });
        });

        var instance = com.FindComponent<BitButton>().Instance;

        Assert.IsFalse(instance.AllowDisabledFocus);
        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreEqual(BitButtonType.Reset, instance.ButtonType);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("report.pdf", instance.Download);
        Assert.IsTrue(instance.Draggable);
        Assert.IsTrue(instance.FixedColor);
        Assert.IsTrue(instance.Float);
        Assert.IsTrue(instance.FloatAbsolute);
        Assert.AreEqual("2rem", instance.FloatOffset);
        Assert.AreEqual(BitPosition.TopLeft, instance.FloatPosition);
        Assert.AreEqual("my-form", instance.FormId);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual("https://bitplatform.dev", instance.Href);
        Assert.AreEqual("Share", instance.IconName);
        Assert.IsTrue(instance.IconOnly);
        Assert.AreEqual(BitIconPosition.End, instance.IconPosition);
        Assert.AreEqual("/images/icon.svg", instance.IconUrl);
        Assert.IsTrue(instance.IsLoading);
        Assert.AreEqual(250, instance.LoadingDelay);
        Assert.AreEqual("Sending...", instance.LoadingLabel);
        Assert.AreEqual(BitLabelPosition.Top, instance.LoadingLabelPosition);
        Assert.IsTrue(instance.NoWrap);
        Assert.IsTrue(instance.Reclickable);
        Assert.AreEqual(BitLinkRels.NoOpener, instance.Rel);
        Assert.IsTrue(instance.Rounded);
        Assert.AreEqual("secondary", instance.SecondaryText);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual("_blank", instance.Target);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.AreEqual(BitVariant.Outline, instance.Variant);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsFalse(instance.IsEnabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitButtonParamsShouldNotOverwriteWhatTheMarkupAlreadySet()
    {
        var @params = new BitButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Params Title"
        };

        var com = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Title, "Existing Title");
        });

        @params.UpdateParameters(com.Instance);

        Assert.AreEqual(BitColor.Error, com.Instance.Color);
        Assert.AreEqual(BitSize.Small, com.Instance.Size);
        Assert.AreEqual("Existing Title", com.Instance.Title);
    }

    [TestMethod]
    public void BitButtonParamsShouldApplyTheCascadedClassesAndStyles()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonParams
            {
                Classes = new BitButtonClassStyles { Root = "custom-root", Icon = "custom-icon" },
                Styles = new BitButtonClassStyles { Root = "color: red;", Icon = "font-size: 2rem;" }
            }
        };

        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                builder.AddAttribute(1, nameof(BitButton.IconName), "Emoji");
                builder.CloseComponent();
            });
        });

        var root = com.Find(".bit-btn");
        var icon = com.Find(".bit-btn-icn");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.AreEqual("font-size: 2rem;", icon.GetAttribute("style"));
    }
}
