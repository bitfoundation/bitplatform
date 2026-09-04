using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

[TestClass]
public class BitStickyTests : BunitTestContext
{
    [TestMethod]
    public void BitStickyShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitStickyShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var style = "padding: 1rem;";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        component.MarkupMatches(@$"<div style=""{style}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var cssClass = "test-class";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, cssClass);
        });

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top {cssClass}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectId(string id)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-stk bit-stk-top""></div>");
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitStickyShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-stk bit-stk-top{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitStickyShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
                break;
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<div aria-label=""{ariaLabel}"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitStickyShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<div class=""bit-stk bit-stk-top"" id:ignore>{childContent}</div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitStickyHtmlAttributesTest>();

        // The two attributes the component only writes while the parameter carries a value have to
        // leave the splatted ones of the same name alone: writing them as null would take them away
        // rather than skip them.
        component.MarkupMatches(@"<div data-val-test=""bit"" aria-label=""splatted label"" dir=""rtl"" class=""bit-stk bit-stk-top"" id:ignore>I'm a sticky</div>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectTop(string top)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, top);
        });

        if (top.HasValue())
        {
            component.MarkupMatches(@$"<div style=""top: {top};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectTopChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var top = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Top, top);
        });

        component.MarkupMatches(@$"<div style=""top: {top};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectBottom(string bottom)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Bottom, bottom);
        });

        if (bottom.HasValue())
        {
            component.MarkupMatches(@$"<div style=""bottom: {bottom};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectBottomChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var bottom = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Bottom, "20px");
        });

        component.MarkupMatches(@$"<div style=""bottom: {bottom};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectLeft(string left)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Left, left);
        });

        if (left.HasValue())
        {
            component.MarkupMatches(@$"<div style=""left: {left};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectLeftChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var left = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Left, left);
        });

        component.MarkupMatches(@$"<div style=""left: {left};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("14px"),
       DataRow("1.5rem")
    ]
    public void BitStickyShouldRespectRight(string right)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Right, right);
        });

        if (right.HasValue())
        {
            component.MarkupMatches(@$"<div style=""right: {right};"" class=""bit-stk"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectRightChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        var right = "20px";

        component.Render(parameters =>
        {
            parameters.Add(p => p.Right, right);
        });

        component.MarkupMatches(@$"<div style=""right: {right};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow("14px", "15px", "16px", "17px"),
       DataRow("1.5rem", "2.5rem", "3.5rem", "4.5rem")
    ]
    public void BitStickyShouldRespectTopBottomLeftRight(string top, string bottom, string left, string right)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, top);
            parameters.Add(p => p.Bottom, bottom);
            parameters.Add(p => p.Left, left);
            parameters.Add(p => p.Right, right);
        });

        component.MarkupMatches(@$"<div style=""top: {top};bottom: {bottom};left: {left};right: {right};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow("20", "20px"),
       DataRow("1.5", "1.5px"),
       DataRow("0", "0px")
    ]
    public void BitStickyShouldReadBareNumberOffsetsAsPixels(string offset, string expected)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, offset);
        });

        component.MarkupMatches(@$"<div style=""top: {expected};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow("calc(1rem + 2px)"),
       DataRow("10%"),
       DataRow("Infinity"),
       DataRow("NaN")
    ]
    public void BitStickyShouldLeaveOffsetsThatAreNoPixelCountAsWritten(string offset)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Top, offset);
        });

        // The infinities and the not-a-number double.TryParse also accepts by name are numbers no
        // length can be written of, so they go to the stylesheet as the words they were given as.
        component.MarkupMatches(@$"<div style=""top: {offset};"" class=""bit-stk"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(BitStickyPosition.Top),
       DataRow(BitStickyPosition.Bottom),
       DataRow(BitStickyPosition.TopAndBottom),
       DataRow(BitStickyPosition.Start),
       DataRow(BitStickyPosition.End),
       DataRow(BitStickyPosition.StartAndEnd)
    ]
    public void BitStickyShouldRespectPosition(BitStickyPosition? position)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Position, position);
        });

        var cssClass = position switch
        {
            BitStickyPosition.Top => " bit-stk-top",
            BitStickyPosition.Bottom => " bit-stk-btm",
            BitStickyPosition.TopAndBottom => " bit-stk-tab",
            BitStickyPosition.Start => " bit-stk-srt",
            BitStickyPosition.End => " bit-stk-end",
            BitStickyPosition.StartAndEnd => " bit-stk-sae",
            _ => " bit-stk-top"
        };

        component.MarkupMatches(@$"<div class=""bit-stk{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectPositionChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Position, BitStickyPosition.Start);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-srt"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldRespectPositionAlongsideOffsets()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Position, BitStickyPosition.Top);
            parameters.Add(p => p.Top, "10px");
        });

        component.MarkupMatches(@"<div style=""top: 10px;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
       DataRow(3),
       DataRow(0),
       DataRow(-1),
       DataRow(null)
    ]
    public void BitStickyShouldRespectZIndex(int? zIndex)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.ZIndex, zIndex);
        });

        if (zIndex.HasValue)
        {
            component.MarkupMatches(@$"<div style=""z-index: {zIndex};"" class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitStickyShouldRespectZIndexChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>();

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ZIndex, 5);
        });

        component.MarkupMatches(@"<div style=""z-index: 5;"" class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod,
       DataRow("header"),
       DataRow("footer"),
       DataRow("nav"),
       DataRow("aside"),
       DataRow("section")
    ]
    public void BitStickyShouldRespectElement(string element)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("I'm a sticky");
        });

        component.MarkupMatches(@$"<{element} class=""bit-stk bit-stk-top"" id:ignore>I'm a sticky</{element}>");
    }

    [TestMethod,
       DataRow(null),
       DataRow(""),
       DataRow("   "),
       DataRow("1header"),
       DataRow("my element"),
       DataRow("div>span"),
       DataRow("<div")
    ]
    public void BitStickyShouldFallBackToDivForAnythingThatIsNoTagName(string element)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public void BitStickyShouldTrimTheElementAndKeepEveryParameterOnIt()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Element, "  header  ");
            parameters.Add(p => p.Position, BitStickyPosition.Bottom);
            parameters.Add(p => p.ZIndex, 2);
            parameters.Add(p => p.AriaLabel, "pinned bar");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        component.MarkupMatches(@"<header aria-label=""pinned bar"" dir=""rtl"" style=""z-index: 2;"" class=""bit-stk bit-stk-btm bit-rtl"" id:ignore></header>");
    }

    [TestMethod]
    public void BitStickyShouldRespectElementChangingAfterRender()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Element, "header");
        });

        component.MarkupMatches(@"<header class=""bit-stk bit-stk-top"" id:ignore></header>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Element, "footer");
        });

        component.MarkupMatches(@"<footer class=""bit-stk bit-stk-top"" id:ignore></footer>");
    }

    [TestMethod]
    public async Task BitStickyShouldKeepTheStuckStateAcrossAnElementChange()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Element, "header");
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        component.MarkupMatches(@"<header class=""bit-stk bit-stk-top bit-stk-stc bit-stk-stc-top my-stuck"" id:ignore></header>");

        // A change of tag replaces the rendered element rather than patching it, so the detection has
        // to be attached to the new one; the state it had derived so far is what it carries over.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Element, "footer");
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        Assert.IsTrue(component.Instance.IsStuck);

        component.MarkupMatches(@"<footer class=""bit-stk bit-stk-top bit-stk-stc bit-stk-stc-top my-stuck"" id:ignore></footer>");
    }

    [TestMethod]
    public void BitStickyShouldNotApplyStuckClassAndStyleWhileNotStuck()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        Assert.IsFalse(component.Instance.IsStuck);
        Assert.AreEqual(BitStickyEdges.None, component.Instance.StuckEdges);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldApplyStuckClassAndStyleWhileStuck()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        Assert.IsTrue(component.Instance.IsStuck);

        component.MarkupMatches(@"<div style=""color: red;"" class=""bit-stk bit-stk-top bit-stk-stc bit-stk-stc-top my-stuck"" id:ignore></div>");

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.None));

        Assert.IsFalse(component.Instance.IsStuck);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldAppendStuckStyleAfterStyle()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.Style, "color: blue");
            parameters.Add(p => p.StuckStyle, "color: red");
        });

        Assert.AreEqual("color: blue", component.Find("div").GetAttribute("style"));

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        // The stuck style has to land after the resting one, since the later declaration of the same
        // property is the one an inline style resolves to.
        Assert.AreEqual("color: blue;color: red", component.Find("div").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitStickyShouldRespectOnStuckChanged()
    {
        var stuckStates = new List<bool>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);
        Assert.IsTrue(component.Instance.IsStuck);

        // A repeated report of the same state must not raise the callback again.
        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.None));

        CollectionAssert.AreEqual(new List<bool> { true, false }, stuckStates);
        Assert.IsFalse(component.Instance.IsStuck);
    }

    [TestMethod,
       DataRow(BitStickyEdges.Top, "bit-stk-stc-top"),
       DataRow(BitStickyEdges.Bottom, "bit-stk-stc-btm"),
       DataRow(BitStickyEdges.Left, "bit-stk-stc-lft"),
       DataRow(BitStickyEdges.Right, "bit-stk-stc-rgt")
    ]
    public async Task BitStickyShouldCarryAClassPerStuckEdge(BitStickyEdges edge, string cssClass)
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)edge));

        Assert.AreEqual(edge, component.Instance.StuckEdges);

        var classList = component.Find("div").ClassList;

        Assert.IsTrue(classList.Contains("bit-stk-stc"));
        Assert.IsTrue(classList.Contains(cssClass));
        Assert.IsTrue(classList.Contains("my-stuck"));
    }

    [TestMethod]
    public async Task BitStickyShouldCarryBothClassesWhilePinnedIntoACorner()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)(BitStickyEdges.Top | BitStickyEdges.Left)));

        Assert.AreEqual(BitStickyEdges.Top | BitStickyEdges.Left, component.Instance.StuckEdges);

        var classList = component.Find("div").ClassList;

        Assert.IsTrue(classList.Contains("bit-stk-stc-top"));
        Assert.IsTrue(classList.Contains("bit-stk-stc-lft"));
        Assert.IsFalse(classList.Contains("bit-stk-stc-btm"));
        Assert.IsFalse(classList.Contains("bit-stk-stc-rgt"));
    }

    [TestMethod]
    public async Task BitStickyShouldRespectOnStuckEdgesChanged()
    {
        var edges = new List<BitStickyEdges>();
        var stuckStates = new List<bool>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
            parameters.Add(p => p.OnStuckEdgesChanged, (BitStickyEdges e) => edges.Add(e));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Bottom));

        CollectionAssert.AreEqual(new List<BitStickyEdges> { BitStickyEdges.Bottom }, edges);
        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);

        // The move from one edge of a pair to the other never stops the element from being stuck, so
        // it is reported here and nowhere else.
        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        CollectionAssert.AreEqual(new List<BitStickyEdges> { BitStickyEdges.Bottom, BitStickyEdges.Top }, edges);
        CollectionAssert.AreEqual(new List<bool> { true }, stuckStates);

        // A repeated report of the same edges raises neither of them.
        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        Assert.AreEqual(2, edges.Count);

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.None));

        CollectionAssert.AreEqual(new List<BitStickyEdges> { BitStickyEdges.Bottom, BitStickyEdges.Top, BitStickyEdges.None }, edges);
        CollectionAssert.AreEqual(new List<bool> { true, false }, stuckStates);
    }

    [TestMethod]
    public async Task BitStickyShouldAttachTheDetectionForOnStuckEdgesChangedAlone()
    {
        var edges = new List<BitStickyEdges>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckEdgesChanged, (BitStickyEdges e) => edges.Add(e));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        Assert.IsTrue(component.Instance.IsStuck);

        component.Render(parameters =>
        {
            parameters.Add(p => p.OnStuckEdgesChanged, (BitStickyEdges e) => edges.Add(e));
            parameters.Add(p => p.IsEnabled, false);
        });

        // Detaching the detection reports the state it leaves behind, the same way a flip would.
        Assert.IsFalse(component.Instance.IsStuck);
        CollectionAssert.AreEqual(new List<BitStickyEdges> { BitStickyEdges.Top, BitStickyEdges.None }, edges);
    }

    [TestMethod]
    public async Task BitStickyShouldIgnoreStuckReportsWhileDisabled()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        Assert.IsFalse(component.Instance.IsStuck);
        Assert.AreEqual(BitStickyEdges.None, component.Instance.StuckEdges);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldResetStuckStateWhenDisabled()
    {
        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-stk-stc bit-stk-stc-top my-stuck"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.StuckClass, "my-stuck");
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsFalse(component.Instance.IsStuck);
        Assert.AreEqual(BitStickyEdges.None, component.Instance.StuckEdges);

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top bit-dis"" id:ignore></div>");
    }

    [TestMethod]
    public async Task BitStickyShouldReportUnstuckWhenDetectionIsDetached()
    {
        var stuckStates = new List<bool>();

        var component = RenderComponent<BitSticky>(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
        });

        await component.InvokeAsync(() => component.Instance._OnStuckChange((int)BitStickyEdges.Top));

        CollectionAssert.AreEqual(new[] { true }, stuckStates);

        component.Render(parameters =>
        {
            parameters.Add(p => p.OnStuckChanged, (bool stuck) => stuckStates.Add(stuck));
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsFalse(component.Instance.IsStuck);

        CollectionAssert.AreEqual(new[] { true, false }, stuckStates);
    }

    [TestMethod]
    public async Task BitStickyShouldNotFailRefreshingWithoutAnAttachedDetection()
    {
        var component = RenderComponent<BitSticky>();

        // Nothing observes the state, so no script is attached and there is nothing to read again.
        await component.Instance.RefreshAsync();

        Assert.IsFalse(component.Instance.IsStuck);
    }

    [TestMethod]
    public void BitStickyShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitStickyCascadingParamsTest>();

        var stickies = component.FindAll(".bit-stk");

        // The first sticky takes everything from the cascading parameters.
        Assert.AreEqual("HEADER", stickies[0].TagName);
        Assert.IsTrue(stickies[0].ClassList.Contains("cascaded"));
        Assert.IsTrue(stickies[0].ClassList.Contains("bit-stk-btm"));
        StringAssert.Contains(stickies[0].GetAttribute("style"), "top: 1rem");
        StringAssert.Contains(stickies[0].GetAttribute("style"), "z-index: 4");

        // The second one sets its own element, position and z-index, which must not be overwritten.
        Assert.AreEqual("FOOTER", stickies[1].TagName);
        Assert.IsTrue(stickies[1].ClassList.Contains("bit-stk-srt"));
        Assert.IsFalse(stickies[1].ClassList.Contains("bit-stk-btm"));
        StringAssert.Contains(stickies[1].GetAttribute("style"), "z-index: 9");

        // What it did not set is still filled in from them.
        Assert.IsTrue(stickies[1].ClassList.Contains("cascaded"));
        StringAssert.Contains(stickies[1].GetAttribute("style"), "top: 1rem");
    }

    [TestMethod]
    public async Task BitStickyShouldRespectStuckClassFromCascadingParams()
    {
        var component = RenderComponent<BitStickyCascadingParamsTest>();

        var sticky = component.FindComponents<BitSticky>()[0];

        await component.InvokeAsync(() => sticky.Instance._OnStuckChange((int)BitStickyEdges.Bottom));

        var classList = component.FindAll(".bit-stk")[0].ClassList;

        Assert.IsTrue(classList.Contains("bit-stk-stc"));
        Assert.IsTrue(classList.Contains("bit-stk-stc-btm"));
        Assert.IsTrue(classList.Contains("cascaded-stuck"));
    }
}
