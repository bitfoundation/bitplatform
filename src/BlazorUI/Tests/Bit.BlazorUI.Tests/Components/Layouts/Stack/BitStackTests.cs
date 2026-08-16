using System;
using System.Collections.Generic;
using System.Text;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Stack;

[TestClass]
public class BitStackTests : BunitTestContext
{
    private const string STYLE = "display:flex;flex-direction:column;gap:1rem;";

    private static readonly Dictionary<BitAlignment, string> _AlignmentMap = new()
    {
        { BitAlignment.Start, "flex-start" },
        { BitAlignment.End, "flex-end" },
        { BitAlignment.Center, "center" },
        { BitAlignment.SpaceBetween, "space-between" },
        { BitAlignment.SpaceAround, "space-around" },
        { BitAlignment.SpaceEvenly, "space-evenly" },
        { BitAlignment.Baseline, "baseline" },
        { BitAlignment.Stretch, "stretch" },
    };

    // The members that share room out between the children say nothing about the axis across them, and
    // Baseline says nothing about the axis they are laid out along, so each axis only accepts its own.
    private static readonly BitAlignment[] _Distributions =
    [
        BitAlignment.SpaceBetween,
        BitAlignment.SpaceAround,
        BitAlignment.SpaceEvenly
    ];

    private static bool IsCrossAlignment(BitAlignment alignment) => Array.IndexOf(_Distributions, alignment) < 0;

    private static bool IsMainAlignment(BitAlignment alignment) => alignment != BitAlignment.Baseline;



    [TestMethod]
    public void BitStackShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRenderADivByDefault()
    {
        var component = RenderComponent<BitStack>();

        Assert.AreEqual("DIV", component.Find(".bit-stc").TagName);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@$"<div class=""bit-stc bit-dis"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitStackShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldAppendTheCustomStyleAfterTheGeneratedOne()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Style, "gap:5rem");
        });

        // The custom style is appended after the generated one so it wins over it.
        Assert.AreEqual("display:flex;flex-direction:column;gap:1rem;gap:5rem", component.Find(".bit-stc").GetAttribute("style"));
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitStackShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-stc {cssClass}"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitStackShouldRespectId(string id)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-stc"" style=""{STYLE}""></div>");
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitStackShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-stc{cssClass}"" style=""{STYLE}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@$"<div class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStackShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@$"<div dir=""ltr"" class=""bit-stc"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitStackShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = visibility switch
        {
            BitVisibility.Hidden => "visibility: hidden;",
            BitVisibility.Collapsed => "display: none;",
            _ => null
        };

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}display: none;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldLetVisibilityWinOverInline()
    {
        // The collapsed display is registered after everything the stack generates, so an inline stack
        // that is collapsed is still removed from the layout rather than left as an inline-flex box.
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        var style = component.Find(".bit-stc").GetAttribute("style");

        StringAssert.Contains(style, "display:inline-flex");
        StringAssert.EndsWith(style, "display:none");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitStackShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            if (childContent is not null)
            {
                parameters.AddChildContent(childContent);
            }
        });

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore>{childContent}</div>");
    }

    [TestMethod]
    public void BitStackShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitStackHtmlAttributesTest>();

        component.MarkupMatches(@$"<div data-val-test=""bit"" style=""{STYLE}"" class=""bit-stc"" id:ignore>I'm a stack</div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "main navigation");
        });

        // A stack rendered as a landmark element has to be nameable, so the label reaches the root.
        Assert.AreEqual("main navigation", component.Find(".bit-stc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitStackShouldNotRenderAriaLabelByDefault()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitStackShouldRespectTabIndex()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "0");
        });

        // A scrollable stack has to be reachable by the keyboard, which is what the tab index is for.
        Assert.AreEqual("0", component.Find(".bit-stc").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitStackShouldNotRenderTabIndexByDefault()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").HasAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("p"),
        DataRow("span"),
        DataRow("nav"),
        DataRow(null)
    ]
    public void BitStackShouldRespectElement(string element)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var el = element.HasValue() ? element : "div";
        component.MarkupMatches(@$"<{el} class=""bit-stc"" style=""{STYLE}"" id:ignore></{el}>");
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, true),
        DataRow(true, false),
        DataRow(false, false)
    ]
    public void BitStackShouldRespectFillContent(bool fillContent, bool horizontal)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FillContent, fillContent);
            parameters.Add(p => p.Horizontal, horizontal);
        });

        var fd = horizontal ? "row" : "column";
        var cssClass = fillContent ? (horizontal ? " bit-stc-fch" : " bit-stc-fcv") : null;

        component.MarkupMatches(@$"<div class=""bit-stc{cssClass}"" style=""display:flex;flex-direction:{fd};gap:1rem"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitStackShouldRespectFillContentChangingAfterRender(bool horizontal)
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.FillContent, true);
            parameters.Add(p => p.Horizontal, horizontal);
        });

        var fd = horizontal ? "row" : "column";
        var cssClass = horizontal ? "bit-stc-fch" : "bit-stc-fcv";

        component.MarkupMatches(@$"<div class=""bit-stc {cssClass}"" style=""display:flex;flex-direction:{fd};gap:1rem"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectFillContentWithAResponsiveDirection()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FillContent, true);
            parameters.Add(p => p.HorizontalMd, true);
        });

        var root = component.Find(".bit-stc");

        // Which of the two axes is the cross one is only known to the stylesheet once the direction
        // answers to the width of the window, so the responsive fill class replaces the fixed pair.
        Assert.IsTrue(root.ClassList.Contains("bit-stc-fcr"));
        Assert.IsFalse(root.ClassList.Contains("bit-stc-fch"));
        Assert.IsFalse(root.ClassList.Contains("bit-stc-fcv"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectGrowContent(bool growContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.GrowContent, growContent);
        });

        Assert.AreEqual(growContent, component.Find(".bit-stc").ClassList.Contains("bit-stc-grc"));
    }

    [TestMethod]
    public void BitStackShouldRespectGrowContentChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").ClassList.Contains("bit-stc-grc"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.GrowContent, true);
        });

        Assert.IsTrue(component.Find(".bit-stc").ClassList.Contains("bit-stc-grc"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectEqualContent(bool equalContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.EqualContent, equalContent);
        });

        Assert.AreEqual(equalContent, component.Find(".bit-stc").ClassList.Contains("bit-stc-eqc"));
    }

    [TestMethod]
    public void BitStackShouldRespectEqualContentChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").ClassList.Contains("bit-stc-eqc"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.EqualContent, true);
        });

        Assert.IsTrue(component.Find(".bit-stc").ClassList.Contains("bit-stc-eqc"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectNoShrinkContent(bool noShrinkContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.NoShrinkContent, noShrinkContent);
        });

        Assert.AreEqual(noShrinkContent, component.Find(".bit-stc").ClassList.Contains("bit-stc-nsc"));
    }

    [TestMethod]
    public void BitStackShouldRespectNoShrinkContentChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").ClassList.Contains("bit-stc-nsc"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoShrinkContent, true);
        });

        Assert.IsTrue(component.Find(".bit-stc").ClassList.Contains("bit-stc-nsc"));
    }

    [TestMethod]
    public void BitStackShouldNotLetTheParametersThatActOnTheChildrenReachTheStackItself()
    {
        // The four of them are classes handed to the children, and none of them describes the stack as a
        // child of its own container, so nothing about the stack itself may be written out by them.
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FillContent, true);
            parameters.Add(p => p.GrowContent, true);
            parameters.Add(p => p.EqualContent, true);
            parameters.Add(p => p.NoShrinkContent, true);
        });

        component.MarkupMatches(@$"<div class=""bit-stc bit-stc-fcv bit-stc-grc bit-stc-eqc bit-stc-nsc"" style=""{STYLE}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldCombineFillContentAndGrowContent()
    {
        // The two act on different axes, so asking for both is asking for both.
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.FillContent, true);
            parameters.Add(p => p.GrowContent, true);
        });

        var root = component.Find(".bit-stc");

        Assert.IsTrue(root.ClassList.Contains("bit-stc-fch"));
        Assert.IsTrue(root.ClassList.Contains("bit-stc-grc"));
    }

    [TestMethod,
        DataRow("10px"),
        DataRow("1rem"),
        DataRow("1rem 2rem"),
        DataRow(null)
    ]
    public void BitStackShouldRespectGap(string gap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        var style = gap.HasValue() ? $"gap:{gap}" : $"gap:1rem";

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:column;{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectGapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var gap = "2rem";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Gap, gap);
        });

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:column;gap:{gap};"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-stc-sm"),
        DataRow(BitSize.Medium, "bit-stc-md"),
        DataRow(BitSize.Large, "bit-stc-lg")
    ]
    public void BitStackShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var root = component.Find(".bit-stc");

        // The length itself belongs to the theme, so the component only points the gap at the token
        // the class of the step declares.
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
        StringAssert.Contains(root.GetAttribute("style")!, "gap:var(--bit-stc-size)");
    }

    [TestMethod]
    public void BitStackShouldNotRenderASizeClassByDefault()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldLetGapWinOverSize()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.Gap, "3rem");
        });

        var root = component.Find(".bit-stc");

        // The class of the step is still handed out - it only declares a token - but the explicit
        // length is the more specific of the two and is what the gap reads.
        Assert.IsTrue(root.ClassList.Contains("bit-stc-lg"));
        StringAssert.Contains(root.GetAttribute("style")!, "gap:3rem");
    }

    [TestMethod]
    public void BitStackShouldRespectSizeChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").ClassList.Contains("bit-stc-sm"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
        });

        var root = component.Find(".bit-stc");

        Assert.IsTrue(root.ClassList.Contains("bit-stc-sm"));
        StringAssert.Contains(root.GetAttribute("style")!, "gap:var(--bit-stc-size)");
    }

    [TestMethod,
        DataRow("2rem"),
        DataRow("0"),
        DataRow(null)
    ]
    public void BitStackShouldRespectHorizontalGap(string horizontalGap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalGap, horizontalGap);
        });

        var style = horizontalGap.HasValue() ? $"column-gap:{horizontalGap}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("2rem"),
        DataRow("0"),
        DataRow(null)
    ]
    public void BitStackShouldRespectVerticalGap(string verticalGap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.VerticalGap, verticalGap);
        });

        var style = verticalGap.HasValue() ? $"row-gap:{verticalGap}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldWriteThePerAxisGapsAfterTheShorthand()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.HorizontalGap, "2rem");
            parameters.Add(p => p.VerticalGap, "0.5rem");
        });

        // A longhand only replaces the shorthand while it comes after it.
        Assert.AreEqual("display:flex;flex-direction:column;gap:1rem;column-gap:2rem;row-gap:0.5rem",
                        component.Find(".bit-stc").GetAttribute("style"));
    }

    [TestMethod]
    public void BitStackShouldRespectHorizontalGapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.HorizontalGap, "2rem");
        });

        component.MarkupMatches(@$"<div style=""{STYLE}column-gap:2rem;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectVerticalGapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.VerticalGap, "0.5rem");
        });

        component.MarkupMatches(@$"<div style=""{STYLE}row-gap:0.5rem;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("3"),
        DataRow("initial"),
        DataRow("inherit"),
        DataRow(null)
    ]
    public void BitStackShouldRespectGrow(string grow)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
        });

        var style = grow.HasValue() ? $"flex-grow:{grow}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectGrowChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        var grow = "2";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-grow:{grow};"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectGrows(bool grows)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grows, grows);
        });

        var style = grows ? "flex-grow:1" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectGrowsChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Grows, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-grow:1;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectNoShrink(bool noShrink)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.NoShrink, noShrink);
        });

        var style = noShrink ? "flex-shrink:0" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectNoShrinkChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoShrink, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-shrink:0;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("3"),
        DataRow("0"),
        DataRow("initial"),
        DataRow(null)
    ]
    public void BitStackShouldRespectShrink(string shrink)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Shrink, shrink);
        });

        var style = shrink.HasValue() ? $"flex-shrink:{shrink}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectShrinkChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Shrink, "2");
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-shrink:2;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true, null),
        DataRow(true, "2"),
        DataRow(false, null),
        DataRow(false, "2")
    ]
    public void BitStackShouldRespectNoShrinkAndShrink(bool noShrink, string shrink)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Shrink, shrink);
            parameters.Add(p => p.NoShrink, noShrink);
        });

        // The factor is the more specific of the two, so it is what a stack that was given both answers for.
        var style = (shrink.HasValue() || noShrink) ? $"flex-shrink:{(shrink.HasValue() ? shrink : "0")}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("0"),
        DataRow("50%"),
        DataRow("12rem"),
        DataRow("auto"),
        DataRow(null)
    ]
    public void BitStackShouldRespectBasis(string basis)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Basis, basis);
        });

        var style = basis.HasValue() ? $"flex-basis:{basis}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectBasisChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Basis, "0");
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-basis:0;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldWriteTheFlexItemDeclarationsInTheOrderOfTheShorthand()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grow, "2");
            parameters.Add(p => p.Shrink, "3");
            parameters.Add(p => p.Basis, "0");
        });

        // The three are longhands of one shorthand, and they read as one line in that order.
        Assert.AreEqual("display:flex;flex-direction:column;gap:1rem;flex-grow:2;flex-shrink:3;flex-basis:0",
                        component.Find(".bit-stc").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(0),
        DataRow(2),
        DataRow(-1),
        DataRow(null)
    ]
    public void BitStackShouldRespectOrder(int? order)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Order, order);
        });

        var style = order.HasValue ? $"order:{order.Value}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectOrderChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Order, -1);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}order:-1;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("1rem"),
        DataRow("2rem 0.5rem"),
        DataRow("1px 2px 3px 4px"),
        DataRow(null)
    ]
    public void BitStackShouldRespectPadding(string padding)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Padding, padding);
        });

        var style = padding.HasValue() ? $"padding:{padding}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectPaddingChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Padding, "1rem");
        });

        component.MarkupMatches(@$"<div style=""{STYLE}padding:1rem;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectInline(bool inline)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Inline, inline);
        });

        var display = inline ? "inline-flex" : "flex";
        var cssClass = inline ? " bit-stc-inl" : null;

        component.MarkupMatches(@$"<div style=""display:{display};flex-direction:column;gap:1rem"" class=""bit-stc{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectInlineChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Inline, true);
        });

        component.MarkupMatches(@$"<div style=""display:inline-flex;flex-direction:column;gap:1rem"" class=""bit-stc bit-stc-inl"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldSizeAnInlineStackByItsContent()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Inline, true);
            parameters.Add(p => p.FitWidth, true);
        });

        var root = component.Find(".bit-stc");

        // The sizing of an inline stack is handed back through a class, so an explicit size - here a
        // fitted width, which is written onto the element - still outranks it.
        Assert.IsTrue(root.ClassList.Contains("bit-stc-inl"));
        StringAssert.Contains(root.GetAttribute("style")!, "width:fit-content");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectHorizontal(bool horizontal)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, horizontal);
        });

        var fd = horizontal ? "row" : "column";

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:{fd};gap:1rem"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectHorizontalChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
        });

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:row;gap:1rem"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(null),
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch)
    ]
    public void BitStackShouldRespectAlignment(BitAlignment? alignment)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
        });

        var style = new StringBuilder();

        if (alignment.HasValue)
        {
            // The shorthand only reaches the axis its value means something on, so a distribution ends
            // up on one axis alone and Baseline on the other, rather than on both as invalid CSS.
            if (IsCrossAlignment(alignment.Value)) style.Append($"align-items:{_AlignmentMap[alignment.Value]};");
            if (IsMainAlignment(alignment.Value)) style.Append($"justify-content:{_AlignmentMap[alignment.Value]};");
        }

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAlignmentChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.SpaceBetween);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}justify-content:space-between;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch)
    ]
    public void BitStackShouldRespectHorizontalAlign(BitAlignment horizontalAlign)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, horizontalAlign);
        });

        // A vertical stack lays its children out down the page, so the horizontal axis is the one
        // across them and only the members that mean something there are written out.
        var style = IsCrossAlignment(horizontalAlign) ? $"align-items:{_AlignmentMap[horizontalAlign]}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectHorizontalAlignChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, BitAlignment.Center);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}align-items:center"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch)
    ]
    public void BitStackShouldRespectVerticalAlign(BitAlignment verticalAlign)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, verticalAlign);
        });

        // A vertical stack lays its children out down the page, so the vertical axis is the one they
        // are laid out along and Baseline is the member that means nothing there.
        var style = IsMainAlignment(verticalAlign) ? $"justify-content:{_AlignmentMap[verticalAlign]}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectVerticalAlignChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, BitAlignment.SpaceBetween);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}justify-content:space-between;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldLetAnAlignmentThatMeansNothingOnItsAxisFallThroughToTheShorthand()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.SpaceBetween);
        });

        // The horizontal axis of a vertical stack cannot distribute, so the specific value steps aside
        // for the shorthand rather than silencing it, and the vertical axis is untouched by either.
        component.MarkupMatches(@$"<div style=""{STYLE}align-items:center;justify-content:center"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldLetTheSpecificAlignmentWinOverTheShorthand()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.End);
            parameters.Add(p => p.VerticalAlign, BitAlignment.Start);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}align-items:flex-end;justify-content:flex-start"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch),
        DataRow(null)
    ]
    public void BitStackShouldRespectAlignContent(BitAlignment? alignContent)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AlignContent, alignContent);
        });

        // Every member but Baseline places the rows of a wrapping stack; a flex container has no
        // baseline behavior for align-content, so that one is dropped.
        var style = (alignContent.HasValue && alignContent != BitAlignment.Baseline)
                        ? $"align-content:{_AlignmentMap[alignContent.Value]}"
                        : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAlignContentChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AlignContent, BitAlignment.SpaceEvenly);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}align-content:space-evenly;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitAlignment.Start),
        DataRow(BitAlignment.End),
        DataRow(BitAlignment.Center),
        DataRow(BitAlignment.SpaceBetween),
        DataRow(BitAlignment.SpaceAround),
        DataRow(BitAlignment.SpaceEvenly),
        DataRow(BitAlignment.Baseline),
        DataRow(BitAlignment.Stretch),
        DataRow(null)
    ]
    public void BitStackShouldRespectSelf(BitAlignment? self)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Self, self);
        });

        // A single child has no room to share out, so the three distributions mean nothing here.
        var style = (self.HasValue && IsCrossAlignment(self.Value)) ? $"align-self:{_AlignmentMap[self.Value]}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectSelfChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Self, BitAlignment.End);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}align-self:flex-end;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldNotLetSelfBeAffectedByTheAlignmentShorthand()
    {
        // Self is about the stack within its own container, which the alignments of its children say
        // nothing about, so the two must not bleed into each other.
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
        });

        Assert.IsFalse(component.Find(".bit-stc").GetAttribute("style")!.Contains("align-self"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectReversed(bool reversed)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var fd = reversed ? "column-reverse" : "column";

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:{fd};gap:1rem"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectReversedChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
        });

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:column-reverse;gap:1rem"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectWrap(bool wrap)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Wrap, wrap);
        });

        var style = wrap ? "flex-wrap:wrap" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectWrapChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Wrap, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-wrap:wrap"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectWrapReverse(bool wrapReverse)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.WrapReverse, wrapReverse);
        });

        var style = wrapReverse ? "flex-wrap:wrap-reverse" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldLetWrapReverseWinOverWrap()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Wrap, true);
            parameters.Add(p => p.WrapReverse, true);
        });

        // Both are a request to wrap, so the one that also says which way wins.
        component.MarkupMatches(@$"<div style=""{STYLE}flex-wrap:wrap-reverse"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectWrapReverseChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.WrapReverse, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}flex-wrap:wrap-reverse"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true, null),
        DataRow(true, "2"),
        DataRow(false, null),
        DataRow(false, "2")
    ]
    public void BitStackShouldRespectGrowsAndGrow(bool grows, string grow)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Grow, grow);
            parameters.Add(p => p.Grows, grows);
        });

        var style = (grow.HasValue() || grows) ? $"flex-grow:{(grow.HasValue() ? grow : "1")}" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitStackShouldRespectHorizontalAndReversed(bool horizontal, bool reversed)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, horizontal);
            parameters.Add(p => p.Reversed, reversed);
        });

        var fd = $"{(horizontal ? "row" : "column")}{(reversed ? "-reverse" : null)}";

        component.MarkupMatches(@$"<div style=""display:flex;flex-direction:{fd};gap:1rem"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectHorizontalAndReversedAndHorizontalAlignAndVerticalAlign(bool horizontal)
    {
        var aligns = Enum.GetValues(typeof(BitAlignment));

        foreach (BitAlignment horizontalAlign in aligns)
        {
            foreach (BitAlignment verticalAlign in aligns)
            {
                var component = RenderComponent<BitStack>(parameters =>
                {
                    parameters.Add(p => p.Horizontal, horizontal);
                    parameters.Add(p => p.VerticalAlign, verticalAlign);
                    parameters.Add(p => p.HorizontalAlign, horizontalAlign);
                });

                // Which of the two named alignments reaches which of the two CSS properties follows the
                // direction of the stack, and each property only accepts the members it can express.
                var fd = horizontal ? "row" : "column";
                var cross = horizontal ? verticalAlign : horizontalAlign;
                var main = horizontal ? horizontalAlign : verticalAlign;

                var style = new StringBuilder();
                if (IsCrossAlignment(cross)) style.Append($"align-items:{_AlignmentMap[cross]};");
                if (IsMainAlignment(main)) style.Append($"justify-content:{_AlignmentMap[main]};");

                component.MarkupMatches(@$"<div style=""display:flex;flex-direction:{fd};gap:1rem;{style}"" class=""bit-stc"" id:ignore></div>");
            }
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectAutoSize(bool autoSize)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AutoSize, autoSize);
        });

        var style = autoSize ? "width:auto;height:auto;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAutoSizeChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoSize, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:auto;height:auto;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectAutoWidth(bool autoWidth)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AutoWidth, autoWidth);
        });

        var style = autoWidth ? "width:auto;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAutoWidthChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoWidth, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:auto;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectAutoHeight(bool autoHeight)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AutoHeight, autoHeight);
        });

        var style = autoHeight ? "height:auto;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectAutoHeightChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoHeight, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}height:auto;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true, true, true),
        DataRow(true, true, false),
        DataRow(true, false, true),
        DataRow(true, false, false),
        DataRow(false, true, true),
        DataRow(false, true, false),
        DataRow(false, false, true),
        DataRow(false, false, false)
    ]
    public void BitStackShouldRespectAutoSizeAndAutoWidthAndAutoHeight(bool autoSize, bool autoWidth, bool autoHeight)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.AutoSize, autoSize);
            parameters.Add(p => p.AutoWidth, autoWidth);
            parameters.Add(p => p.AutoHeight, autoHeight);
        });

        StringBuilder style = new();

        if (autoSize || autoWidth)
        {
            style.Append("width:auto;");
        }

        if (autoSize || autoHeight)
        {
            style.Append("height:auto;");
        }

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFitHeight(bool fitHeight)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FitHeight, fitHeight);
        });

        var style = fitHeight ? "height:fit-content;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectFitHeightChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.FitHeight, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}height:fit-content;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFitWidth(bool fitWidth)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FitWidth, fitWidth);
        });

        var style = fitWidth ? "width:fit-content;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectFitWidthChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.FitWidth, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:fit-content;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStackShouldRespectFitSize(bool fitSize)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.FitSize, fitSize);
        });

        var style = fitSize ? "width:fit-content;height:fit-content;" : null;

        component.MarkupMatches(@$"<div style=""{STYLE}{style}"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldRespectFitSizeChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        component.MarkupMatches(@$"<div style=""{STYLE}"" class=""bit-stc"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.FitSize, true);
        });

        component.MarkupMatches(@$"<div style=""{STYLE}width:fit-content;height:fit-content;"" class=""bit-stc"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStackShouldNotBeResponsiveByDefault()
    {
        var component = RenderComponent<BitStack>();

        var root = component.Find(".bit-stc");

        // A stack that never changes direction keeps it inline, where nothing in a stylesheet reaches it.
        Assert.IsFalse(root.ClassList.Contains("bit-stc-rsp"));
        StringAssert.Contains(root.GetAttribute("style")!, "flex-direction:column");
        Assert.IsFalse(root.GetAttribute("style")!.Contains("--bit-stc-dir"));
    }

    [TestMethod,
        DataRow("xs"),
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg"),
        DataRow("xl"),
        DataRow("xxl")
    ]
    public void BitStackShouldRespectThePerBreakpointDirections(string breakpoint)
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            switch (breakpoint)
            {
                case "xs": parameters.Add(p => p.HorizontalXs, true); break;
                case "sm": parameters.Add(p => p.HorizontalSm, true); break;
                case "md": parameters.Add(p => p.HorizontalMd, true); break;
                case "lg": parameters.Add(p => p.HorizontalLg, true); break;
                case "xl": parameters.Add(p => p.HorizontalXl, true); break;
                default: parameters.Add(p => p.HorizontalXxl, true); break;
            }
        });

        var root = component.Find(".bit-stc");
        var style = root.GetAttribute("style")!;

        // The direction moves off the element and into a custom property the stylesheet reads, since an
        // inline flex-direction would outrank every media query.
        Assert.IsTrue(root.ClassList.Contains("bit-stc-rsp"));
        Assert.IsFalse(style.Contains("flex-direction"));
        StringAssert.Contains(style, "--bit-stc-dir:column");
        StringAssert.Contains(style, $"--bit-stc-dir-{breakpoint}:row");
    }

    [TestMethod]
    public void BitStackShouldOnlyDeclareTheBreakpointsItWasGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.HorizontalMd, false);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // The stylesheet chains every breakpoint that was not asked for to the one below it, which is
        // what carries a value upwards, so declaring them all here would defeat that chain.
        StringAssert.Contains(style, "--bit-stc-dir:row");
        StringAssert.Contains(style, "--bit-stc-dir-md:column");
        Assert.IsFalse(style.Contains("--bit-stc-dir-xs"));
        Assert.IsFalse(style.Contains("--bit-stc-dir-sm"));
        Assert.IsFalse(style.Contains("--bit-stc-dir-lg"));
    }

    [TestMethod]
    public void BitStackShouldReverseEveryBreakpointOfAResponsiveDirection()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.HorizontalMd, true);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // A reversed stack is reversed at every width, so the suffix belongs to every value of the chain.
        StringAssert.Contains(style, "--bit-stc-dir:column-reverse");
        StringAssert.Contains(style, "--bit-stc-dir-md:row-reverse");
    }

    [TestMethod]
    public void BitStackShouldRespectTheResponsiveDirectionChangingAfterRender()
    {
        var component = RenderComponent<BitStack>();

        Assert.IsFalse(component.Find(".bit-stc").ClassList.Contains("bit-stc-rsp"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.HorizontalSm, true);
        });

        var root = component.Find(".bit-stc");

        Assert.IsTrue(root.ClassList.Contains("bit-stc-rsp"));
        StringAssert.Contains(root.GetAttribute("style")!, "--bit-stc-dir-sm:row");

        component.Render(parameters =>
        {
            parameters.Add(p => p.HorizontalSm, (bool?)null);
        });

        root = component.Find(".bit-stc");

        // Taking the last per breakpoint direction away hands the direction back to the element.
        Assert.IsFalse(root.ClassList.Contains("bit-stc-rsp"));
        StringAssert.Contains(root.GetAttribute("style")!, "flex-direction:column");
    }

    [TestMethod]
    public void BitStackShouldResolveTheAlignmentsPerBreakpointOfAResponsiveDirection()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
            parameters.Add(p => p.VerticalAlign, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.SpaceBetween);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // The two alignments move off the element for the same reason the direction does, and each declared
        // direction resolves them itself: the base is a column, where spreading the children out is what the
        // vertical one does and what the horizontal one cannot, and at md the stack is a row and they swap.
        Assert.IsFalse(style.Contains("align-items:"));
        Assert.IsFalse(style.Contains("justify-content:"));
        StringAssert.Contains(style, "--bit-stc-ai:initial");
        StringAssert.Contains(style, "--bit-stc-jc:center");
        StringAssert.Contains(style, "--bit-stc-ai-md:center");
        StringAssert.Contains(style, "--bit-stc-jc-md:space-between");
    }

    [TestMethod]
    public void BitStackShouldNotDeclareTheAlignmentVariablesOfAResponsiveStackThatWasGivenNoAlignment()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // There is nothing for the breakpoints to resolve, and an empty chain is what the stylesheet already
        // falls back from to the start edge of both axes.
        Assert.IsFalse(style.Contains("--bit-stc-ai"));
        Assert.IsFalse(style.Contains("--bit-stc-jc"));
    }

    [TestMethod]
    public void BitStackShouldUndoAnAlignmentThatTheDirectionOfABreakpointMakesMeaningless()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.Baseline);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // Lining the children up on their first line of text says nothing about the axis they are laid out
        // along, so the value the column below md is laid out by has to be undone at md rather than carried
        // up the chain - which is what the initial value of a custom property is for.
        StringAssert.Contains(style, "--bit-stc-ai:baseline");
        StringAssert.Contains(style, "--bit-stc-ai-md:initial");
        StringAssert.Contains(style, "--bit-stc-jc-md:initial");
    }

    [TestMethod]
    public void BitStackShouldResolveTheAlignmentsOfABreakpointThatTurnsARowIntoAColumn()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.HorizontalLg, false);
            parameters.Add(p => p.VerticalAlign, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.SpaceBetween);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // The chain runs the other way round here: the base is a row, where the horizontal axis is the one the
        // children are laid out along, and from lg the stack is a column and the pair swaps over.
        StringAssert.Contains(style, "--bit-stc-ai:center");
        StringAssert.Contains(style, "--bit-stc-jc:space-between");
        StringAssert.Contains(style, "--bit-stc-ai-lg:initial");
        StringAssert.Contains(style, "--bit-stc-jc-lg:center");
    }

    [TestMethod]
    public void BitStackShouldFallBackToTheAlignmentShorthandPerBreakpointOfAResponsiveDirection()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.Baseline);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // Baseline means something across the children and nothing along them, so the specific value is what the
        // column below md is laid out by across it, and at md - where it lands on the axis they are laid out
        // along - it steps aside for the shorthand rather than silencing it.
        StringAssert.Contains(style, "--bit-stc-ai:baseline");
        StringAssert.Contains(style, "--bit-stc-jc:center");
        StringAssert.Contains(style, "--bit-stc-ai-md:center");
        StringAssert.Contains(style, "--bit-stc-jc-md:center");
    }

    [TestMethod]
    public void BitStackShouldOnlyDeclareTheAlignmentsOfTheBreakpointsItWasGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalSm, true);
            parameters.Add(p => p.Alignment, BitAlignment.Center);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // The stylesheet chains the breakpoints that were not asked for to the one below them, exactly as it
        // does for the direction, so declaring them here would defeat that chain.
        StringAssert.Contains(style, "--bit-stc-ai-sm:center");
        StringAssert.Contains(style, "--bit-stc-jc-sm:center");
        Assert.IsFalse(style.Contains("--bit-stc-ai-xs"));
        Assert.IsFalse(style.Contains("--bit-stc-jc-md"));
    }

    [TestMethod]
    public void BitStackShouldNotDeclareTheAlignmentVariablesWithoutAResponsiveDirection()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // A stack that never changes direction keeps its alignments inline, where nothing can reach them.
        StringAssert.Contains(style, "align-items:center");
        StringAssert.Contains(style, "justify-content:center");
        Assert.IsFalse(style.Contains("--bit-stc-ai"));
        Assert.IsFalse(style.Contains("--bit-stc-jc"));
    }

    [TestMethod]
    public void BitStackShouldRespectTheAlignmentsOfAResponsiveDirectionChangingAfterRender()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, BitAlignment.End);
        });

        StringAssert.Contains(component.Find(".bit-stc").GetAttribute("style")!, "align-items:flex-end");

        component.Render(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        // Being given a per breakpoint direction is what hands the alignments over to the stylesheet.
        Assert.IsFalse(style.Contains("align-items:"));
        StringAssert.Contains(style, "--bit-stc-ai:flex-end");
        StringAssert.Contains(style, "--bit-stc-jc-md:flex-end");
    }

    [TestMethod]
    public void BitStackShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitStackCascadingParamsTest>();

        var stacks = component.FindAll(".bit-stc");

        // The first stack takes everything from the cascading parameters, the cascaded Size included,
        // even though the cascaded Gap is the more specific of the two and wins the gap itself.
        Assert.IsTrue(stacks[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(stacks[0].ClassList.Contains("bit-stc-lg"));
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "flex-direction:row");
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "gap:3rem");
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "padding:1rem");
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "flex-wrap:wrap");
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "flex-shrink:3");

        // The second one sets its own gap, which the cascading parameters must not overwrite.
        Assert.IsTrue(stacks[1].ClassList.Contains("cascaded"));
        StringAssert.Contains(stacks[1].GetAttribute("style")!, "gap:5rem");
        Assert.IsFalse(stacks[1].GetAttribute("style")!.Contains("3rem"));

        // The third one takes the parameters that describe a stack as a child of another one, and the
        // cascaded per breakpoint direction moves its direction into the stylesheet.
        Assert.IsTrue(stacks[2].ClassList.Contains("cascaded"));
        Assert.IsTrue(stacks[2].ClassList.Contains("bit-stc-rsp"));
        Assert.IsTrue(stacks[2].ClassList.Contains("bit-stc-grc"));
        Assert.IsTrue(stacks[2].ClassList.Contains("bit-stc-eqc"));
        Assert.IsTrue(stacks[2].ClassList.Contains("bit-stc-nsc"));
        Assert.IsTrue(stacks[2].ClassList.Contains("bit-stc-inl"));
        Assert.AreEqual("SECTION", stacks[2].TagName);
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "display:inline-flex");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "flex-grow:2");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "flex-shrink:0");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "flex-basis:0");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "align-self:flex-end");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "order:-1");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "--bit-stc-dir-md:row");

        // A cascaded alignment is resolved per breakpoint exactly as one set on the stack itself is: the
        // cascaded direction of md is a row, where placing the children on the horizontal axis is what
        // justify-content does, and the column below it is laid out by align-items.
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "--bit-stc-ai:center");
        StringAssert.Contains(stacks[2].GetAttribute("style")!, "--bit-stc-jc-md:center");
    }
}
