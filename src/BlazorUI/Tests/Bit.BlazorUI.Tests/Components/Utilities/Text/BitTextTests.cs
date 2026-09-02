using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

[TestClass]
public class BitTextTests : BunitTestContext
{
    private static readonly Dictionary<BitTypography, string> VariantMapping = new()
    {
        { BitTypography.Body1, "p" },
        { BitTypography.Body2, "p" },
        { BitTypography.Button, "span" },
        { BitTypography.Caption1, "span" },
        { BitTypography.Caption2, "span" },
        { BitTypography.H1, "h1" },
        { BitTypography.H2, "h2" },
        { BitTypography.H3, "h3" },
        { BitTypography.H4, "h4" },
        { BitTypography.H5, "h5" },
        { BitTypography.H6, "h6" },
        { BitTypography.Inherit, "p" },
        { BitTypography.Overline, "span" },
        { BitTypography.Subtitle1, "h6" },
        { BitTypography.Subtitle2, "h6" }
    };

    [TestMethod]
    public void BitTextShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod,
         DataRow(BitTypography.H1),
         DataRow(BitTypography.H2),
         DataRow(BitTypography.H3),
         DataRow(BitTypography.H4),
         DataRow(BitTypography.H5),
         DataRow(BitTypography.H6),
         DataRow(BitTypography.Subtitle1),
         DataRow(BitTypography.Subtitle2),
         DataRow(BitTypography.Body1),
         DataRow(BitTypography.Body2),
         DataRow(BitTypography.Button),
         DataRow(BitTypography.Caption1),
         DataRow(BitTypography.Caption2),
         DataRow(BitTypography.Overline),
         DataRow(BitTypography.Inherit),
    ]
    public void BitTextShouldRespectVariant(BitTypography variant)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Typography, variant);
        });

        var el = VariantMapping[variant];
        var cssClass = $"bit-txt-{variant.ToString().ToLower(CultureInfo.InvariantCulture)}";

        component.MarkupMatches(@$"<{el} class=""bit-txt {cssClass}"" id:ignore></{el}>");
    }

    [TestMethod,
        DataRow("h1"),
        DataRow("div"),
        DataRow(null)
    ]
    public void BitTextShouldRespectElement(string element)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var defaultVariant = BitTypography.Subtitle1;
        var el = element is null ? VariantMapping[defaultVariant] : element;

        component.MarkupMatches(@$"<{el} class=""bit-txt bit-txt-subtitle1"" id:ignore></{el}>");

    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectNoWrap(bool noWrap)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
        });

        var cssClass = noWrap ? " bit-txt-nowrap" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectNoWrapChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-nowrap"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectGutter(bool gutter)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Gutter, gutter);
        });

        var cssClass = gutter ? " bit-txt-gutter" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectGutterChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Gutter, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-gutter"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-dis"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitTextShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<h6 style=""{style}"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [TestMethod]
    public void BitTextShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@$"<h6 style=""padding: 1rem;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }


    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitTextShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 test-class"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitTextShouldRespectId(string id)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<h6 id=""{expectedId}"" class=""bit-txt bit-txt-subtitle1""></h6>");
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitTextShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<h6 dir=""{dir.Value.ToString().ToLower()}"" class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [TestMethod]
    public void BitTextShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<h6 dir=""ltr"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitTextShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<h6 style=""visibility: hidden;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
                break;
        }
    }

    [TestMethod]
    public void BitTextShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<h6 style=""display: none;"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitTextShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<h6 aria-label=""{ariaLabel}"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
        else
        {
            component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitTextShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore>{childContent}</h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitTextHtmlAttributesTest>();

        component.MarkupMatches(@"<h6 data-val-test=""bit"" class=""bit-txt bit-txt-subtitle1"" id:ignore>I'm a text</h6>");
    }



    [TestMethod,
        DataRow(BitFontWeight.Light, "bit-txt-fwl"),
        DataRow(BitFontWeight.Regular, "bit-txt-fwr"),
        DataRow(BitFontWeight.Medium, "bit-txt-fwm"),
        DataRow(BitFontWeight.Semibold, "bit-txt-fws"),
        DataRow(BitFontWeight.Bold, "bit-txt-fwb"),
        DataRow(null, null)
    ]
    public void BitTextShouldRespectWeight(BitFontWeight? weight, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Weight, weight);
        });

        var expected = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{expected}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectWeightChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Weight, BitFontWeight.Bold);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-fwb"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(BitTextTransform.None, "bit-txt-trn"),
        DataRow(BitTextTransform.Uppercase, "bit-txt-tru"),
        DataRow(BitTextTransform.Lowercase, "bit-txt-trl"),
        DataRow(BitTextTransform.Capitalize, "bit-txt-trc"),
        DataRow(null, null)
    ]
    public void BitTextShouldRespectTransform(BitTextTransform? transform, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Transform, transform);
        });

        var expected = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{expected}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(BitTextWrap.Wrap, "bit-txt-wrp"),
        DataRow(BitTextWrap.NoWrap, "bit-txt-wnw"),
        DataRow(BitTextWrap.Balance, "bit-txt-wbl"),
        DataRow(BitTextWrap.Pretty, "bit-txt-wpr"),
        DataRow(BitTextWrap.Stable, "bit-txt-wst"),
        DataRow(null, null)
    ]
    public void BitTextShouldRespectWrap(BitTextWrap? wrap, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Wrap, wrap);
        });

        var expected = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{expected}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectItalic(bool italic)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Italic, italic);
        });

        var cssClass = italic ? " bit-txt-itl" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectUnderline(bool underline)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Underline, underline);
        });

        var cssClass = underline ? " bit-txt-und" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectStrikethrough(bool strikethrough)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Strikethrough, strikethrough);
        });

        var cssClass = strikethrough ? " bit-txt-stk" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRenderBothDecorationsTogether()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Underline, true);
            parameters.Add(p => p.Strikethrough, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-und bit-txt-stk"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectNumeric(bool numeric)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Numeric, numeric);
        });

        var cssClass = numeric ? " bit-txt-num" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectMonospace(bool monospace)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Monospace, monospace);
        });

        var cssClass = monospace ? " bit-txt-mno" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectMonospaceChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Monospace, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-mno"" id:ignore></h6>");
    }

    // The family and the figures are two decisions: the monospaced family draws every character at one width, and
    // the tabular figures only the digits, so a text asking for both is written with both classes.
    [TestMethod]
    public void BitTextShouldRenderTheMonospaceAndTheNumericAskedForTogether()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Monospace, true);
            parameters.Add(p => p.Numeric, true);
        });

        var classList = component.Find("h6").ClassList;

        Assert.IsTrue(classList.Contains("bit-txt-mno"));
        Assert.IsTrue(classList.Contains("bit-txt-num"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectNoSelect(bool noSelect)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.NoSelect, noSelect);
        });

        var cssClass = noSelect ? " bit-txt-nsl" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectBlock(bool block)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Block, block);
        });

        var cssClass = block ? " bit-txt-blk" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectBreakWord(bool breakWord)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.BreakWord, breakWord);
        });

        var cssClass = breakWord ? " bit-txt-brw" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectForceBreak(bool forceBreak)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.ForceBreak, forceBreak);
        });

        var cssClass = forceBreak ? " bit-txt-fbr" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectHyphenate(bool hyphenate)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Hyphenate, hyphenate);
        });

        var cssClass = hyphenate ? " bit-txt-hyp" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectPreserveWhitespace(bool preserveWhitespace)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.PreserveWhitespace, preserveWhitespace);
        });

        var cssClass = preserveWhitespace ? " bit-txt-pws" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectPreserveWhitespaceChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.PreserveWhitespace, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-pws"" id:ignore></h6>");
    }

    // The whitespace and what may be broken in the middle of a word are two decisions, so asking for one of them
    // never answers the other: a forced break no longer preserves the runs of spaces around what it breaks.
    [TestMethod]
    public void BitTextShouldComposePreserveWhitespaceWithTheBreakingParameters()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.PreserveWhitespace, true);
            parameters.Add(p => p.ForceBreak, true);
            parameters.Add(p => p.BreakWord, true);
        });

        var classList = component.Find("h6").ClassList;

        Assert.IsTrue(classList.Contains("bit-txt-pws"));
        Assert.IsTrue(classList.Contains("bit-txt-fbr"));
        Assert.IsTrue(classList.Contains("bit-txt-brw"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitTextTrim.None, "bit-txt-tmn"),
        DataRow(BitTextTrim.Start, "bit-txt-tms"),
        DataRow(BitTextTrim.End, "bit-txt-tme"),
        DataRow(BitTextTrim.Both, "bit-txt-tmb")
    ]
    public void BitTextShouldRespectTrim(BitTextTrim? trim, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Trim, trim);
        });

        var @class = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{@class}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectTrimChangingAfterRender()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Trim, BitTextTrim.Start);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-tms"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Trim, BitTextTrim.Both);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-tmb"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTextShouldRespectVisuallyHidden(bool visuallyHidden)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.VisuallyHidden, visuallyHidden);
        });

        var cssClass = visuallyHidden ? " bit-txt-vhd" : null;

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{cssClass}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectVisuallyHiddenChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.VisuallyHidden, true);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-vhd"" id:ignore></h6>");
    }



    [TestMethod,
        DataRow(1),
        DataRow(2),
        DataRow(5)
    ]
    public void BitTextShouldRespectLineClamp(int lineClamp)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.LineClamp, lineClamp);
        });

        var element = component.Find("h6");

        StringAssert.Contains(element.GetAttribute("class"), "bit-txt-clp");
        Assert.AreEqual($"-webkit-line-clamp:{lineClamp};line-clamp:{lineClamp}", element.GetAttribute("style"));
    }

    // Only whole lines can be clamped, so a value that names none of them is a value the component leaves the text
    // alone for rather than one it writes an unusable declaration of.
    [TestMethod,
        DataRow(0),
        DataRow(-1),
        DataRow(null)
    ]
    public void BitTextShouldIgnoreALineClampBelowOne(int? lineClamp)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.LineClamp, lineClamp);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectLineClampChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.LineClamp, 3);
        });

        var element = component.Find("h6");

        StringAssert.Contains(element.GetAttribute("class"), "bit-txt-clp");
        Assert.AreEqual("-webkit-line-clamp:3;line-clamp:3", element.GetAttribute("style"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.LineClamp, null);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    // The two truncations are separate parameters and neither turns the other off, so both land on the element and
    // the stylesheet decides between them - the clamp is declared last and is the one that stands.
    [TestMethod,
        DataRow(null, null),
        DataRow(BitColor.Primary, "bit-txt-pri"),
        DataRow(BitColor.Secondary, "bit-txt-sec"),
        DataRow(BitColor.Tertiary, "bit-txt-ter"),
        DataRow(BitColor.Info, "bit-txt-inf"),
        DataRow(BitColor.Success, "bit-txt-suc"),
        DataRow(BitColor.Warning, "bit-txt-wrn"),
        DataRow(BitColor.SevereWarning, "bit-txt-swr"),
        DataRow(BitColor.Error, "bit-txt-err"),
        DataRow(BitColor.PrimaryBackground, "bit-txt-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-txt-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-txt-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-txt-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-txt-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-txt-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-txt-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-txt-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-txt-tbr")
    ]
    public void BitTextShouldRespectColor(BitColor? color, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var @class = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{@class}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectColorChangingAfterRender()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Info);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-inf"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-err"" id:ignore></h6>");
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitColorKind.Primary, "bit-txt-pfg"),
        DataRow(BitColorKind.Secondary, "bit-txt-sfg"),
        DataRow(BitColorKind.Tertiary, "bit-txt-tfg"),
        DataRow(BitColorKind.Transparent, "bit-txt-rfg")
    ]
    public void BitTextShouldRespectForeground(BitColorKind? foreground, string cssClass)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Foreground, foreground);
        });

        var @class = cssClass is null ? null : $" {cssClass}";

        component.MarkupMatches(@$"<h6 class=""bit-txt bit-txt-subtitle1{@class}"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectForegroundChangingAfterRender()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Foreground, BitColorKind.Secondary);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-sfg"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Foreground, BitColorKind.Transparent);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1 bit-txt-rfg"" id:ignore></h6>");
    }

    // A color role and a foreground kind are two registrations of their own, so the two are written side by side
    // rather than one of them replacing the other; which of them paints the text is left to the stylesheet.
    [TestMethod]
    public void BitTextShouldRenderTheColorAndTheForegroundAskedForTogether()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Success);
            parameters.Add(p => p.Foreground, BitColorKind.Tertiary);
        });

        var classList = component.Find("h6").ClassList;

        Assert.IsTrue(classList.Contains("bit-txt-suc"));
        Assert.IsTrue(classList.Contains("bit-txt-tfg"));
    }



    [TestMethod]
    public void BitTextShouldRespectGradient()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Gradient, "linear-gradient(90deg, red, blue)");
        });

        var element = component.Find("h6");

        Assert.IsTrue(element.ClassList.Contains("bit-txt-grd"));
        Assert.AreEqual("background-image:linear-gradient(90deg, red, blue)", element.GetAttribute("style"));
    }

    // The gradient is the whole of what the parameter writes, so a value naming none of it is a value the component
    // leaves the text alone for rather than one it writes an empty declaration of.
    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("   ")
    ]
    public void BitTextShouldIgnoreAGradientWithNoValue(string gradient)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Gradient, gradient);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldRespectGradientChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Gradient, "linear-gradient(red, blue)");
        });

        var element = component.Find("h6");

        Assert.IsTrue(element.ClassList.Contains("bit-txt-grd"));
        Assert.AreEqual("background-image:linear-gradient(red, blue)", element.GetAttribute("style"));
    }

    // The clip is what the class carries and the number of lines is what the clamp writes inline, so the two
    // truncations and the gradient are three declarations that stand beside each other rather than replacing one.
    [TestMethod]
    public void BitTextShouldWriteTheGradientBesideTheOtherStylesItBuilds()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Align, BitTextAlign.Center);
            parameters.Add(p => p.Gradient, "linear-gradient(red, blue)");
            parameters.Add(p => p.LineClamp, 2);
        });

        Assert.AreEqual("text-align:center;background-image:linear-gradient(red, blue);-webkit-line-clamp:2;line-clamp:2",
                        component.Find("h6").GetAttribute("style"));
    }



    [TestMethod]
    public void BitTextShouldRenderBothTruncationsAskedForTogether()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
            parameters.Add(p => p.LineClamp, 2);
        });

        var element = component.Find("h6");

        Assert.IsTrue(element.ClassList.Contains("bit-txt-nowrap"));
        Assert.IsTrue(element.ClassList.Contains("bit-txt-clp"));
    }

    // Every one of these is a property of a run of text rather than of a step of the ramp, so they compose with
    // any variant and with each other rather than replacing one another.
    [TestMethod]
    public void BitTextShouldComposeEveryTextParameterWithTheVariant()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Typography, BitTypography.Overline);
            parameters.Add(p => p.Weight, BitFontWeight.Bold);
            parameters.Add(p => p.Transform, BitTextTransform.None);
            parameters.Add(p => p.Wrap, BitTextWrap.Pretty);
            parameters.Add(p => p.Italic, true);
            parameters.Add(p => p.Numeric, true);
            parameters.Add(p => p.Block, true);
            parameters.Add(p => p.BreakWord, true);
            parameters.Add(p => p.NoSelect, true);
            parameters.Add(p => p.Hyphenate, true);
            parameters.Add(p => p.Gutter, true);
            parameters.Add(p => p.PreserveWhitespace, true);
            parameters.Add(p => p.Monospace, true);
            parameters.Add(p => p.Trim, BitTextTrim.Both);
            parameters.Add(p => p.Color, BitColor.Warning);
            parameters.Add(p => p.Gradient, "linear-gradient(red, blue)");
        });

        var classList = component.Find("span").ClassList;

        foreach (var expected in new[]
        {
            "bit-txt", "bit-txt-overline", "bit-txt-fwb", "bit-txt-trn", "bit-txt-wpr",
            "bit-txt-itl", "bit-txt-num", "bit-txt-blk", "bit-txt-brw", "bit-txt-nsl",
            "bit-txt-hyp", "bit-txt-gutter", "bit-txt-pws", "bit-txt-tmb", "bit-txt-wrn",
            "bit-txt-grd", "bit-txt-mno"
        })
        {
            Assert.IsTrue(classList.Contains(expected), $"the {expected} class is missing");
        }
    }

    // The style of the page is registered after the one the component builds, so a declaration written by the page
    // is the one that stands where the two name the same property.
    [TestMethod]
    public void BitTextShouldKeepTheStyleParameterAfterTheOneItBuilds()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.LineClamp, 2);
            parameters.Add(p => p.Style, "color:red");
        });

        Assert.AreEqual("-webkit-line-clamp:2;line-clamp:2;color:red", component.Find("h6").GetAttribute("style"));
    }



    [TestMethod,
        DataRow(BitTextAlign.Start, "start"),
        DataRow(BitTextAlign.End, "end"),
        DataRow(BitTextAlign.Left, "left"),
        DataRow(BitTextAlign.Right, "right"),
        DataRow(BitTextAlign.Center, "center"),
        DataRow(BitTextAlign.Justify, "justify"),
        DataRow(BitTextAlign.JustifyAll, "justify-all"),
        DataRow(BitTextAlign.MatchParent, "match-parent"),
        DataRow(BitTextAlign.Inherit, "inherit"),
        DataRow(BitTextAlign.Initial, "initial"),
        DataRow(BitTextAlign.Revert, "revert"),
        DataRow(BitTextAlign.RevertLayer, "revert-layer"),
        DataRow(BitTextAlign.Unset, "unset")
    ]
    public void BitTextShouldRespectAlign(BitTextAlign align, string expected)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Align, align);
        });

        Assert.AreEqual($"text-align:{expected}", component.Find("h6").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTextShouldRespectAlignChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Align, BitTextAlign.Center);
        });

        Assert.AreEqual("text-align:center", component.Find("h6").GetAttribute("style"));
    }



    [TestMethod]
    public void BitTextShouldRespectLang()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Lang, "fa");
        });

        component.MarkupMatches(@"<h6 lang=""fa"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldNotRenderALangItWasNotGiven()
    {
        var component = RenderComponent<BitText>();

        Assert.IsFalse(component.Find("h6").HasAttribute("lang"));
    }

    [TestMethod,
        DataRow("0"),
        DataRow("-1"),
        DataRow(null)
    ]
    public void BitTextShouldRespectTabIndex(string tabIndex)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        if (tabIndex is null)
        {
            Assert.IsFalse(component.Find("h6").HasAttribute("tabindex"));
        }
        else
        {
            component.MarkupMatches(@$"<h6 tabindex=""{tabIndex}"" class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
        }
    }

    // A level on a tag that is not a heading names nothing on its own, so the role is what makes it one.
    [TestMethod]
    public void BitTextShouldRenderTheHeadingRoleBesideAnAriaLevelOnANonHeadingElement()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, "div");
            parameters.Add(p => p.AriaLevel, 3);
        });

        component.MarkupMatches(@"<div role=""heading"" aria-level=""3"" class=""bit-txt bit-txt-subtitle1"" id:ignore></div>");
    }

    // A heading tag carries a level of its own, so only the override is written on one.
    [TestMethod]
    public void BitTextShouldNotRenderTheHeadingRoleBesideAnAriaLevelOnAHeadingElement()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Typography, BitTypography.H2);
            parameters.Add(p => p.AriaLevel, 4);
        });

        component.MarkupMatches(@"<h2 aria-level=""4"" class=""bit-txt bit-txt-h2"" id:ignore></h2>");
    }

    // Heading levels count from one, so a value below that names no level and is left out along with the role it
    // would otherwise have written beside it.
    [TestMethod,
        DataRow(0),
        DataRow(-1)
    ]
    public void BitTextShouldIgnoreAnAriaLevelBelowOne(int ariaLevel)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, "div");
            parameters.Add(p => p.AriaLevel, ariaLevel);
        });

        component.MarkupMatches(@"<div class=""bit-txt bit-txt-subtitle1"" id:ignore></div>");
    }

    [TestMethod]
    public void BitTextShouldNotRenderAnAriaLevelItWasNotGiven()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, "div");
        });

        var element = component.Find("div");

        Assert.IsFalse(element.HasAttribute("role"));
        Assert.IsFalse(element.HasAttribute("aria-level"));
    }



    [TestMethod,
        DataRow("  h1  ", "h1"),
        DataRow("my-element", "my-element"),
        DataRow("linearGradient", "linearGradient")
    ]
    public void BitTextShouldTrimAndKeepAUsableElementName(string element, string expected)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        component.MarkupMatches(@$"<{expected} class=""bit-txt bit-txt-subtitle1"" id:ignore></{expected}>");
    }

    // A name that would end the tag, or that an engine refuses to build an element of, falls back to the tag of the
    // variant rather than reaching the markup.
    [TestMethod,
        DataRow("not a tag name"),
        DataRow("h4!"),
        DataRow("<script>"),
        DataRow("1h"),
        DataRow("   ")
    ]
    public void BitTextShouldFallBackToTheVariantElementForAnUnusableElementName(string element)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");
    }

    [TestMethod]
    public void BitTextShouldFallBackToTheElementOfTheVariantItWasGiven()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Typography, BitTypography.Body1);
            parameters.Add(p => p.Element, "not a tag name");
        });

        component.MarkupMatches(@"<p class=""bit-txt bit-txt-body1"" id:ignore></p>");
    }

    [TestMethod]
    public void BitTextShouldRespectElementChangingAfterRender()
    {
        var component = RenderComponent<BitText>();

        component.MarkupMatches(@"<h6 class=""bit-txt bit-txt-subtitle1"" id:ignore></h6>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Element, "span");
        });

        component.MarkupMatches(@"<span class=""bit-txt bit-txt-subtitle1"" id:ignore></span>");
    }

    // A void element is defined to hold no content, so what is put inside one would either be dropped by the static
    // renderer or end up as a sibling of the element in the rendered markup.
    [TestMethod,
        DataRow("br"),
        DataRow("hr"),
        DataRow("img"),
        DataRow("input"),
        DataRow("wbr")
    ]
    public void BitTextShouldNotRenderChildContentInsideAVoidElement(string element)
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("this content has nowhere to go");
        });

        Assert.IsFalse(component.Markup.Contains("this content has nowhere to go"));
    }

    [TestMethod]
    public void BitTextShouldRenderChildContentInsideANonVoidElement()
    {
        var component = RenderComponent<BitText>(parameters =>
        {
            parameters.Add(p => p.Element, "section");
            parameters.AddChildContent("kept");
        });

        component.MarkupMatches(@"<section class=""bit-txt bit-txt-subtitle1"" id:ignore>kept</section>");
    }



    [TestMethod]
    public void BitTextShouldMergeTheSplattedClassAndStyleWithItsOwn()
    {
        var component = RenderComponent<BitTextSplattedAttributesTest>();

        var element = component.Find("p");

        Assert.IsTrue(element.ClassList.Contains("bit-txt"));
        Assert.IsTrue(element.ClassList.Contains("bit-txt-subtitle1"));
        Assert.IsTrue(element.ClassList.Contains("splatted-class"));

        Assert.AreEqual("font-weight:bold;text-align:center", element.GetAttribute("style"));
    }

    // A value the component would otherwise write as null does not leave a splatted attribute of the same name
    // alone - it removes it - so each one is resolved against what the page splatted instead.
    [TestMethod]
    public void BitTextShouldKeepTheSplattedAttributesItWouldOtherwiseWriteItself()
    {
        var component = RenderComponent<BitTextSplattedAttributesTest>();

        var element = component.Find("section");

        Assert.AreEqual("splatted-id", element.GetAttribute("id"));
        Assert.AreEqual("rtl", element.GetAttribute("dir"));
        Assert.AreEqual("de", element.GetAttribute("lang"));
        Assert.AreEqual("splatted label", element.GetAttribute("aria-label"));
        Assert.AreEqual("-1", element.GetAttribute("tabindex"));
        Assert.AreEqual("2", element.GetAttribute("aria-level"));
        Assert.AreEqual("note", element.GetAttribute("role"));
    }

    // What the component is given itself is written over the splatted spelling of the same attribute.
    [TestMethod]
    public void BitTextShouldWriteItsOwnParametersOverTheSplattedAttributes()
    {
        var component = RenderComponent<BitTextSplattedAttributesTest>();

        var element = component.Find("article");

        Assert.AreEqual("own-id", element.GetAttribute("id"));
        Assert.AreEqual("ltr", element.GetAttribute("dir"));
        Assert.AreEqual("fa", element.GetAttribute("lang"));
        Assert.AreEqual("own label", element.GetAttribute("aria-label"));
        Assert.AreEqual("0", element.GetAttribute("tabindex"));
        Assert.AreEqual("5", element.GetAttribute("aria-level"));
        // The level is the component's own, so the role that makes it a heading is written over the splatted one.
        Assert.AreEqual("heading", element.GetAttribute("role"));
    }



    [TestMethod]
    public void BitTextShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitTextCascadingParamsTest>();

        var cascaded = component.Find("section");
        var cssClass = cascaded.GetAttribute("class");
        StringAssert.Contains(cssClass, "bit-txt-body2");
        StringAssert.Contains(cssClass, "bit-txt-fwb");
        StringAssert.Contains(cssClass, "bit-txt-itl");
        StringAssert.Contains(cssClass, "bit-txt-clp");
        StringAssert.Contains(cssClass, "bit-txt-pws");
        StringAssert.Contains(cssClass, "bit-txt-mno");
        StringAssert.Contains(cssClass, "bit-txt-tmb");
        StringAssert.Contains(cssClass, "bit-txt-suc");
        StringAssert.Contains(cssClass, "bit-txt-sfg");
        StringAssert.Contains(cssClass, "bit-txt-grd");
        StringAssert.Contains(cssClass, "cascaded");
        Assert.AreEqual("fr", cascaded.GetAttribute("lang"));
        Assert.AreEqual("4", cascaded.GetAttribute("aria-level"));
        Assert.AreEqual("heading", cascaded.GetAttribute("role"));
        // The style the cascade fills in is built the same way the component's own is, in the order it registers it.
        var cascadedStyle = cascaded.GetAttribute("style");
        StringAssert.Contains(cascadedStyle, "text-align:center");
        StringAssert.Contains(cascadedStyle, "background-image:linear-gradient(red, blue)");
        StringAssert.Contains(cascadedStyle, "line-clamp:2");

        // Everything the text set for itself is kept, and only what it left unset is filled in from the cascade.
        var own = component.Find("article");
        var ownClass = own.GetAttribute("class");
        StringAssert.Contains(ownClass, "bit-txt-h3");
        StringAssert.Contains(ownClass, "bit-txt-fwl");
        StringAssert.Contains(ownClass, "bit-txt-itl");
        StringAssert.Contains(ownClass, "bit-txt-tms");
        StringAssert.Contains(ownClass, "bit-txt-err");
        StringAssert.Contains(ownClass, "bit-txt-pws");
        Assert.AreEqual("de", own.GetAttribute("lang"));
        Assert.AreEqual("2", own.GetAttribute("aria-level"));

        var ownStyle = own.GetAttribute("style");
        StringAssert.Contains(ownStyle, "text-align:end");
        StringAssert.Contains(ownStyle, "background-image:linear-gradient(green, yellow)");
    }
}
