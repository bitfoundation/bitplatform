using System;
using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitActionButtonTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitActionButtonIsEnabledTest(bool isEnabled)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = component.Find(".bit-acb");

        if (isEnabled)
        {
            Assert.IsFalse(button.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow("Icon1"),
        DataRow("Icon2")
    ]
    public void BitActionButtonIconTest(string iconName)
    {
        var componnet = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = componnet.Find(".bit-icon");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("title1"),
        DataRow("title2")
    ]
    public void BitActionButtonTitleTest(string title)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        var button = component.Find(".bit-acb");

        var expectedTitle = title;
        var actualTitle = button.GetAttribute("title");

        Assert.AreEqual(expectedTitle, actualTitle);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitActionButtonOnClickTest(bool isEnabled)
    {
        var clicked = false;

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var button = component.Find(".bit-acb");

        if (isEnabled)
        {
            Assert.IsFalse(button.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        }

        button.Click();

        var expected = isEnabled;
        var actual = clicked;

        Assert.AreEqual(expected, actual);
    }

    [TestMethod,
       DataRow(true, false, false),
       DataRow(true, true, false),
       DataRow(false, false, true),
       DataRow(false, true, false),
   ]
    public void BitActionButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var button = component.Find(".bit-acb");

        var hasTabIndexAttr = button.HasAttribute("tabindex");

        Assert.AreEqual(expectedResult, hasTabIndexAttr);

        if (hasTabIndexAttr)
        {
            Assert.IsTrue(button?.GetAttribute("tabindex")?.Equals("-1"));
        }

    }

    [TestMethod,
        DataRow("description1"),
        DataRow("description2")
    ]
    public void BitActionButtonAriaDescriptionTest(string ariaDescription)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var button = component.Find(".bit-acb");

        Assert.IsTrue(button?.GetAttribute("aria-describedby")?.Contains(ariaDescription));
    }

    [TestMethod,
        DataRow("label1"),
        DataRow("label2")
    ]
    public void BitActionButtonAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var button = component.Find(".bit-acb");

        Assert.IsTrue(button?.GetAttribute("aria-label")?.Contains(ariaLabel));
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, false),
        DataRow(null, false)
    ]
    public void BitActionButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var button = component.Find(".bit-acb");

        Assert.AreEqual(expectedResult, button.HasAttribute("aria-hidden"));
    }

    [TestMethod,
        DataRow(null, true),
        DataRow(null, false),
        DataRow("", true),
        DataRow("", false),
        DataRow(" ", true),
        DataRow(" ", false),
        DataRow("href", true),
        DataRow("href", false)
    ]
    public void BitActionButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = component.Find(".bit-acb");

        var expectedTag = href.HasValue() ? "a" : "button";

        var actualTag = button.TagName;

        Assert.AreEqual(expectedTag, actualTag, ignoreCase: true);
    }

    [TestMethod,
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

        var button = component.Find(".bit-acb");

        var expectedType = buttonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => throw new NotSupportedException(),
        };

        var actualType = button.GetAttribute("type");

        Assert.AreEqual(expectedType, actualType);
    }

    [TestMethod]
    public void BitActionButtonSubmitStateInEditContextTest()
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });

        var button = component.Find(".bit-acb");

        var expectedType = "submit";
        var actualType = button.GetAttribute("type");

        Assert.AreEqual(expectedType, actualType);
    }

    [TestMethod]
    public void BitActionButtonButtonStateNotOverriddenInEditContextTest()
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });

        var button = component.Find(".bit-acb");

        var expectedType = "button";
        var actualType = button.GetAttribute("type");

        Assert.AreEqual(expectedType, actualType);
    }

    [TestMethod]
    public void BitActionButtonTabIndexIsRespectedWhenEnabled()
    {
        const string expectedTabIndex = "3";

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.TabIndex, expectedTabIndex);
        });

        var button = component.Find(".bit-acb");

        var actualTabIndex = button.GetAttribute("tabindex");

        Assert.AreEqual(expectedTabIndex, actualTabIndex);
    }

    [TestMethod,
        DataRow(true, "href1"),
        DataRow(false, "href2")
    ]
    public void BitActionButtonAnchorRespectsEnabledState(bool isEnabled, string href)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var button = component.Find(".bit-acb");

        var expectedHrefPresence = isEnabled;

        Assert.AreEqual(expectedHrefPresence, button.HasAttribute("href"));
        Assert.AreEqual(isEnabled is false, button.HasAttribute("disabled"));

        if (isEnabled)
        {
            Assert.AreEqual(href, button.GetAttribute("href"));
        }
    }

    [TestMethod,
        DataRow(BitColor.Primary),
        DataRow(BitColor.Secondary),
        DataRow(BitColor.Tertiary),
        DataRow(BitColor.Info),
        DataRow(BitColor.Success),
        DataRow(BitColor.Warning),
        DataRow(BitColor.SevereWarning),
        DataRow(BitColor.Error),
        DataRow(BitColor.PrimaryBackground),
        DataRow(BitColor.SecondaryBackground),
        DataRow(BitColor.TertiaryBackground),
        DataRow(BitColor.PrimaryForeground),
        DataRow(BitColor.SecondaryForeground),
        DataRow(BitColor.TertiaryForeground),
        DataRow(BitColor.PrimaryBorder),
        DataRow(BitColor.SecondaryBorder),
        DataRow(BitColor.TertiaryBorder),
        DataRow(null)
        ]
    public void BitActionButtonColorClassTest(BitColor? color)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var button = component.Find(".bit-acb");

        var expectedClass = GetColorClass(color);

        Assert.IsTrue(button.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small),
        DataRow(BitSize.Medium),
        DataRow(BitSize.Large),
        DataRow(null)
    ]
    public void BitActionButtonSizeClassTest(BitSize? size)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var button = component.Find(".bit-acb");

        var expectedClass = GetSizeClass(size);

        Assert.IsTrue(button.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitActionButtonFullWidthClassTest(bool fullWidth)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var button = component.Find(".bit-acb");

        var expectedClassPresence = fullWidth;

        Assert.AreEqual(expectedClassPresence, button.ClassList.Contains("bit-acb-fwi"));
    }

    [TestMethod,
        DataRow(BitIconPosition.Start),
        DataRow(BitIconPosition.End),
        DataRow(null)
    ]
    public void BitActionButtonIconPositionClassTest(BitIconPosition? iconPosition)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            if (iconPosition.HasValue)
            {
                parameters.Add(p => p.IconPosition, iconPosition.Value);
            }
        });

        var bitButton = com.Find(".bit-acb");

        var expectedClassPresence = iconPosition == BitIconPosition.End;

        Assert.AreEqual(expectedClassPresence, bitButton.ClassList.Contains("bit-acb-eni"));
    }

    [TestMethod,
        DataRow("https://bitplatform.dev", BitLinkRels.NoOpener | BitLinkRels.NoReferrer, "noopener noreferrer"),
        DataRow("#section", BitLinkRels.NoOpener | BitLinkRels.NoReferrer, null)
    ]
    public void BitActionButtonRelAttributeShouldFollowHrefRules(string href, BitLinkRels rel, string? expectedRel)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Rel, rel);
        });

        var button = component.Find(".bit-acb");

        var hasRelAttribute = button.HasAttribute("rel");

        Assert.AreEqual(string.IsNullOrEmpty(expectedRel) is false, hasRelAttribute);

        if (expectedRel is not null)
        {
            Assert.AreEqual(expectedRel, button.GetAttribute("rel"));
        }
    }

    [TestMethod]
    public void BitActionButtonShouldNotRenderIconWhenIconNameIsNull()
    {
        var component = RenderComponent<BitActionButton>();

        Assert.IsEmpty(component.FindAll(".bit-acb-ico"));
    }

    [TestMethod]
    public void BitActionButtonShouldHideContentWhenIconOnlyIsTrue()
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Emoji2");
            parameters.AddChildContent("<span>content</span>");
        });

        var icon = component.Find(".bit-acb-ico");

        Assert.IsNotNull(icon);
        Assert.IsEmpty(component.FindAll(".bit-acb-con"));
    }

    [TestMethod]
    public void BitActionButtonShouldApplyCustomClasses()
    {
        var rootClass = "root-class";
        var iconClass = "icon-class";
        var contentClass = "content-class";

        var classes = new BitActionButtonClassStyles
        {
            Root = rootClass,
            Icon = iconClass,
            Content = contentClass
        };

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Add");
            parameters.Add(p => p.Classes, classes);
            parameters.AddChildContent("<span>bit</span>");
        });

        var button = component.Find(".bit-acb");
        var icon = component.Find(".bit-acb-ico");
        var content = component.Find(".bit-acb-con");

        Assert.IsTrue(button.ClassList.Contains(rootClass));
        Assert.IsTrue(icon.ClassList.Contains(iconClass));
        Assert.IsTrue(content.ClassList.Contains(contentClass));
    }

    [TestMethod]
    public void BitActionButtonShouldApplyCustomStyles()
    {
        var rootStyle = "color: red;";
        var iconStyle = "margin: 4px;";
        var contentStyle = "padding: 8px;";

        var styles = new BitActionButtonClassStyles
        {
            Root = rootStyle,
            Icon = iconStyle,
            Content = contentStyle
        };

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Share");
            parameters.Add(p => p.Styles, styles);
            parameters.AddChildContent("<span>bit</span>");
        });

        var button = component.Find(".bit-acb");
        var icon = component.Find(".bit-acb-ico");
        var content = component.Find(".bit-acb-con");

        Assert.IsTrue(button.GetAttribute("style")?.Contains(rootStyle));
        Assert.AreEqual(iconStyle, icon.GetAttribute("style"));
        Assert.AreEqual(contentStyle, content.GetAttribute("style"));
    }

    [TestMethod]
    public void BitActionButtonShouldRenderChildContentWhenIconOnlyIsFalse()
    {
        const string content = "Action content";

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.AddChildContent(content);
            parameters.Add(p => p.IconOnly, false);
        });

        var button = component.Find(".bit-acb");

        Assert.IsTrue(button.TextContent.Contains(content));
    }

    [TestMethod, Ignore]
    public void BitActionButtonShouldRespectArbitraryHtmlAttributes()
    {
        const string dataTestId = "acb-1";

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.AddUnmatched("data-test-id", dataTestId);
            parameters.AddUnmatched("aria-live", "polite");
        });

        var button = component.Find(".bit-acb");

        Assert.AreEqual(dataTestId, button.GetAttribute("data-test-id"));
        Assert.AreEqual("polite", button.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitActionButtonShouldRespectCascadingDir()
    {
        var component = RenderComponent<CascadingValue<BitDir>>(parameters =>
        {
            parameters.Add(p => p.Value, BitDir.Rtl);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitActionButton>(0);
                builder.CloseComponent();
            });
        });

        var button = component.Find(".bit-acb");

        Assert.AreEqual("rtl", button.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitActionButtonInsideEditFormShouldSubmitForm()
    {
        var submitted = false;

        var component = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, new object());
            parameters.Add(p => p.OnValidSubmit, _ => submitted = true);
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)((ec) => builder =>
            {
                builder.OpenComponent<BitActionButton>(0);
                builder.CloseComponent();
            }));
        });

        var form = component.Find("form");

        form.Submit();

        Assert.IsTrue(submitted);
    }

    [TestMethod]
    public void BitActionButtonTargetAttributeShouldRenderWhenHrefProvided()
    {
        const string href = "https://bitplatform.dev";
        const string target = "_blank";

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Target, target);
        });

        var anchor = component.Find(".bit-acb");

        Assert.AreEqual(target, anchor.GetAttribute("target"));
    }

    [TestMethod]
    public void BitActionButtonRenderPerformanceSmokeTest()
    {
        const int renderCount = 200;

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < renderCount; i++)
        {
            RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.Title, $"title-{i}");
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.IsEnabled, i % 3 != 0);
                parameters.Add(p => p.FullWidth, i % 2 == 0);
                parameters.Add(p => p.Href, i % 5 == 0 ? "https://bitplatform.dev" : null);
            });
        }

        stopwatch.Stop();

        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Rendering {renderCount} BitActionButton instances took {stopwatch.Elapsed}.");
    }

    [TestMethod]
    public void BitActionButtonDynamicParameterUpdateShouldRefreshMarkup()
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.IconName, "Add");
        });

        var button = component.Find(".bit-acb");
        var icon = component.Find(".bit-acb-ico");

        Assert.IsFalse(button.ClassList.Contains("bit-dis"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.IconName, "Delete");
        });

        Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Delete"));
    }


    private static string GetColorClass(BitColor? color) => color switch
    {
        BitColor.Primary => "bit-acb-pri",
        BitColor.Secondary => "bit-acb-sec",
        BitColor.Tertiary => "bit-acb-ter",
        BitColor.Info => "bit-acb-inf",
        BitColor.Success => "bit-acb-suc",
        BitColor.Warning => "bit-acb-wrn",
        BitColor.SevereWarning => "bit-acb-swr",
        BitColor.Error => "bit-acb-err",
        BitColor.PrimaryBackground => "bit-acb-pbg",
        BitColor.SecondaryBackground => "bit-acb-sbg",
        BitColor.TertiaryBackground => "bit-acb-tbg",
        BitColor.PrimaryForeground => "bit-acb-pfg",
        BitColor.SecondaryForeground => "bit-acb-sfg",
        BitColor.TertiaryForeground => "bit-acb-tfg",
        BitColor.PrimaryBorder => "bit-acb-pbr",
        BitColor.SecondaryBorder => "bit-acb-sbr",
        BitColor.TertiaryBorder => "bit-acb-tbr",
        _ => "bit-acb-pri"
    };

    private static string GetSizeClass(BitSize? size) => size switch
    {
        BitSize.Small => "bit-acb-sm",
        BitSize.Medium => "bit-acb-md",
        BitSize.Large => "bit-acb-lg",
        _ => "bit-acb-md"
    };
}
