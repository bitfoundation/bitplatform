using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Labels;

[TestClass]
public class BitLabelTests : BunitTestContext
{
    [TestMethod]
    public void BitLabelShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? "bit-lbl" : "bit-lbl bit-dis";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-dis"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectRequired(bool required)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, required);
        });

        // The asterisk is an element rather than a css pseudo element, so that it can be hidden from the assistive
        // technologies that would otherwise announce a "star" in the middle of the caption.
        var expected = required
            ? @"<label class=""bit-lbl bit-lbl-req"" id:ignore><span class=""bit-lbl-rqi"" aria-hidden=""true"">*</span></label>"
            : @"<label class=""bit-lbl"" id:ignore></label>";

        component.MarkupMatches(expected);
    }

    [TestMethod]
    public void BitLabelShouldRespectRequiredChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Required, true);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req"" id:ignore><span class=""bit-lbl-rqi"" aria-hidden=""true"">*</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRenderRequiredIndicatorAfterTheContent()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.AddChildContent("Bit Blazor UI");
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req"" id:ignore>Bit Blazor UI<span class=""bit-lbl-rqi"" aria-hidden=""true"">*</span></label>");
    }

    [TestMethod,
        DataRow("(required)"),
        DataRow("**")
    ]
    public void BitLabelShouldRespectRequiredText(string requiredText)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.RequiredText, requiredText);
        });

        // An indicator written by the page is a word put there to be read, so it is left announced.
        component.MarkupMatches(@$"<label class=""bit-lbl bit-lbl-req"" id:ignore><span class=""bit-lbl-rqi"">{requiredText}</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectRequiredTemplate()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.RequiredText, "(required)");
            parameters.Add(p => p.RequiredTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<b>!</b>")));
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req"" id:ignore><span class=""bit-lbl-rqi""><b>!</b></span></label>");
    }

    [TestMethod]
    public void BitLabelShouldNotRenderRequiredIndicatorWhenNotRequired()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.RequiredText, "(required)");
            parameters.Add(p => p.RequiredTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<b>!</b>")));
        });

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectOptional(bool optional)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Optional, optional);
        });

        var expected = optional
            ? @"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span class=""bit-lbl-opi"">(optional)</span></label>"
            : @"<label class=""bit-lbl"" id:ignore></label>";

        component.MarkupMatches(expected);
    }

    [TestMethod]
    public void BitLabelShouldRespectOptionalChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Optional, true);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span class=""bit-lbl-opi"">(optional)</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectOptionalText()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Optional, true);
            parameters.Add(p => p.OptionalText, "(اختیاری)");
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span class=""bit-lbl-opi"">(اختیاری)</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectOptionalTemplate()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Optional, true);
            parameters.Add(p => p.OptionalText, "(optional)");
            parameters.Add(p => p.OptionalTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<i>opt</i>")));
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span class=""bit-lbl-opi""><i>opt</i></span></label>");
    }

    [TestMethod]
    public void BitLabelShouldPreferRequiredOverOptional()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Optional, true);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req"" id:ignore><span class=""bit-lbl-rqi"" aria-hidden=""true"">*</span></label>");
    }

    [TestMethod,
        DataRow("div"),
        DataRow("span"),
        DataRow("legend")
    ]
    public void BitLabelShouldRespectElement(string element)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("Bit Blazor UI");
        });

        component.MarkupMatches(@$"<{element} class=""bit-lbl"" id:ignore>Bit Blazor UI</{element}>");
    }

    [TestMethod,
        DataRow("not a tag name"),
        DataRow("label!"),
        DataRow("1div"),
        DataRow(" "),
        DataRow("")
    ]
    public void BitLabelShouldFallBackToLabelForAnInvalidElement(string element)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldNotRenderForOnANonLabelElement()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Element, "div");
            parameters.Add(p => p.For, "test-for");
        });

        component.MarkupMatches(@"<div class=""bit-lbl"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectNoWrap(bool noWrap)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
        });

        var cssClass = noWrap ? "bit-lbl bit-lbl-nwr" : "bit-lbl";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectNoSelect(bool noSelect)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.NoSelect, noSelect);
        });

        var cssClass = noSelect ? "bit-lbl bit-lbl-nsl" : "bit-lbl";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLabelShouldRespectVisuallyHidden(bool visuallyHidden)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.VisuallyHidden, visuallyHidden);
        });

        var cssClass = visuallyHidden ? "bit-lbl bit-lbl-vhd" : "bit-lbl";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectVisuallyHiddenChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.VisuallyHidden, true);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-vhd"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-lbl-sm"),
        DataRow(BitSize.Medium, "bit-lbl-md"),
        DataRow(BitSize.Large, "bit-lbl-lg"),
        DataRow(null, null)
    ]
    public void BitLabelShouldRespectSize(BitSize? size, string sizeClass)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var cssClass = sizeClass is null ? "bit-lbl" : $"bit-lbl {sizeClass}";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectSizeChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-lg"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-lbl-pri"),
        DataRow(BitColor.Secondary, "bit-lbl-sec"),
        DataRow(BitColor.Tertiary, "bit-lbl-ter"),
        DataRow(BitColor.Info, "bit-lbl-inf"),
        DataRow(BitColor.Success, "bit-lbl-suc"),
        DataRow(BitColor.Warning, "bit-lbl-wrn"),
        DataRow(BitColor.SevereWarning, "bit-lbl-swr"),
        DataRow(BitColor.Error, "bit-lbl-err"),
        DataRow(BitColor.PrimaryBackground, "bit-lbl-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-lbl-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-lbl-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-lbl-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-lbl-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-lbl-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-lbl-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-lbl-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-lbl-tbr"),
        DataRow(null, null)
    ]
    public void BitLabelShouldRespectColor(BitColor? color, string colorClass)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var cssClass = colorClass is null ? "bit-lbl" : $"bit-lbl {colorClass}";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-err"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectClasses()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Classes, new BitLabelClassStyles
            {
                Root = "custom-root",
                RequiredIndicator = "custom-required"
            });
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-req custom-root"" id:ignore><span class=""bit-lbl-rqi custom-required"" aria-hidden=""true"">*</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectOptionalIndicatorClasses()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Optional, true);
            parameters.Add(p => p.Classes, new BitLabelClassStyles { OptionalIndicator = "custom-optional" });
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span class=""bit-lbl-opi custom-optional"">(optional)</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectStyles()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Styles, new BitLabelClassStyles
            {
                Root = "font-style: italic",
                RequiredIndicator = "color: blueviolet"
            });
        });

        component.MarkupMatches(@"<label style=""font-style: italic"" class=""bit-lbl bit-lbl-req"" id:ignore><span style=""color: blueviolet"" class=""bit-lbl-rqi"" aria-hidden=""true"">*</span></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectOptionalIndicatorStyles()
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Optional, true);
            parameters.Add(p => p.Styles, new BitLabelClassStyles { OptionalIndicator = "color: mediumseagreen" });
        });

        component.MarkupMatches(@"<label class=""bit-lbl bit-lbl-opt"" id:ignore><span style=""color: mediumseagreen"" class=""bit-lbl-opi"">(optional)</span></label>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<label style=""{style}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [TestMethod]
    public void BitLabelShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@"<label style=""padding: 1rem;"" class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $"bit-lbl {@class}" : "bit-lbl";

        component.MarkupMatches(@$"<label class=""{cssClass}"" id:ignore></label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<label class=""bit-lbl test-class"" id:ignore></label>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectId(string id)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId;

        component.MarkupMatches(@$"<label id=""{expectedId}"" class=""bit-lbl""></label>");
    }

    [TestMethod,
        DataRow("test-for"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectFor(string @for)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.For, @for);
        });

        if (@for.HasValue())
        {
            component.MarkupMatches(@$"<label for=""{@for}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [TestMethod,
        DataRow("-1"),
        DataRow("0"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectTabIndex(string tabIndex)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        if (tabIndex.HasValue())
        {
            component.MarkupMatches(@$"<label tabindex=""{tabIndex}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitLabelShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? "bit-lbl bit-rtl" : "bit-lbl";
            component.MarkupMatches(@$"<label dir=""{dir.Value.ToString().ToLower()}"" class=""{cssClass}"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [TestMethod]
    public void BitLabelShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<label dir=""ltr"" class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitLabelShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<label style=""visibility: hidden;"" class=""bit-lbl"" id:ignore></label>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<label style=""display: none;"" class=""bit-lbl"" id:ignore></label>");
                break;
        }
    }

    [TestMethod]
    public void BitLabelShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitLabel>();

        component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<label style=""display: none;"" class=""bit-lbl"" id:ignore></label>");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<label class=""bit-lbl"" id:ignore>{childContent}</label>");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitLabelShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitLabel>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<label aria-label=""{ariaLabel}"" class=""bit-lbl"" id:ignore></label>");
        }
        else
        {
            component.MarkupMatches(@"<label class=""bit-lbl"" id:ignore></label>");
        }
    }

    [TestMethod]
    public void BitLabelShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitLabelHtmlAttributesTest>();

        component.MarkupMatches(@"<label data-val-test=""bit"" class=""bit-lbl"" id:ignore>I'm a label</label>");
    }

    [TestMethod]
    public void BitLabelShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitLabelCascadingParamsTest>();

        var labels = component.FindAll(".bit-lbl");

        Assert.AreEqual(2, labels.Count);

        // The first label takes everything from the cascading parameters.
        Assert.AreEqual("DIV", labels[0].TagName);
        Assert.IsTrue(labels[0].ClassList.Contains("bit-lbl-lg"));
        Assert.IsTrue(labels[0].ClassList.Contains("bit-lbl-err"));
        Assert.IsTrue(labels[0].ClassList.Contains("bit-lbl-nwr"));
        Assert.IsTrue(labels[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(labels[0].ClassList.Contains("bit-lbl-req"));
        Assert.AreEqual("(required)", labels[0].QuerySelector(".bit-lbl-rqi")!.TextContent);

        // The second one sets its own size and renders a label element, which the cascading parameters must not
        // overwrite.
        Assert.AreEqual("LABEL", labels[1].TagName);
        Assert.IsTrue(labels[1].ClassList.Contains("bit-lbl-sm"));
        Assert.IsFalse(labels[1].ClassList.Contains("bit-lbl-lg"));
        Assert.IsTrue(labels[1].ClassList.Contains("bit-lbl-err"));
    }
}
