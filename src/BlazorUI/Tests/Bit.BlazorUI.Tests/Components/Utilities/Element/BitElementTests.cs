using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Element;

[TestClass]
public class BitElementTests : BunitTestContext
{
    [TestMethod]
    public void BitElementShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
    }

    [TestMethod,
         DataRow("div"),
         DataRow("button"),
         DataRow("input"),
         DataRow("a"),
         DataRow(null)
    ]
    public void BitElementShouldRespectElement(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var el = element ?? "div";

        component.MarkupMatches(@$"<{el} class=""bit-elm"" id:ignore></{el}>");
    }

    [TestMethod,
         DataRow(""),
         DataRow("   "),
         DataRow(" span "),
    ]
    public void BitElementShouldTrimElementAndFallBackToDiv(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var el = element.Trim().HasValue() ? element.Trim() : "div";

        component.MarkupMatches(@$"<{el} class=""bit-elm"" id:ignore></{el}>");
    }

    [TestMethod]
    public void BitElementShouldRespectElementChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Element, "section");
        });

        component.MarkupMatches(@"<section class=""bit-elm"" id:ignore></section>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        if (isEnabled)
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm bit-dis"" tabindex=""-1"" aria-disabled=""true"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow("button"),
        DataRow("fieldset"),
        DataRow("input"),
        DataRow("optgroup"),
        DataRow("option"),
        DataRow("select"),
        DataRow("textarea")
    ]
    public void BitElementShouldRenderDisabledAttributeOnTheElementsThatSupportIt(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.IsEnabled, false);
        });

        var el = component.Find(element);

        Assert.IsTrue(el.HasAttribute("disabled"));
        Assert.AreEqual("true", el.GetAttribute("aria-disabled"));
        // The browser takes an element it disables itself out of the tab order, so nothing has to be forced here.
        Assert.IsFalse(el.HasAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("div"),
        DataRow("a"),
        DataRow("span")
    ]
    public void BitElementShouldNotRenderDisabledAttributeOnTheElementsThatDoNotSupportIt(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.IsEnabled, false);
        });

        var el = component.Find(element);

        Assert.IsFalse(el.HasAttribute("disabled"));
        Assert.AreEqual("true", el.GetAttribute("aria-disabled"));
        // Nothing the browser does takes such an element out of the tab order, so the tab stop is removed here.
        Assert.AreEqual("-1", el.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldRemoveTheHrefOfADisabledAnchor()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var element = component.FindAll(".bit-elm")[6];

        // An anchor with an href is focusable of itself and follows that href on the enter key, which neither the
        // forced tab stop nor the pointer events of the disabled class reach, so the href goes while it is disabled.
        Assert.IsFalse(element.HasAttribute("href"));
        Assert.AreEqual("true", element.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitElementShouldKeepTheHrefOfAnEnabledAnchor()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        Assert.AreEqual("https://bitplatform.dev/", component.FindAll(".bit-elm")[5].GetAttribute("href"));
    }

    [TestMethod]
    public void BitElementShouldRespectIsEnabledChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        component.MarkupMatches(@"<div class=""bit-elm bit-dis"" tabindex=""-1"" aria-disabled=""true"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("font-size: 14px; color: red;"),
        DataRow("padding: 1rem;"),
        DataRow(null)
    ]
    public void BitElementShouldRespectStyle(string style)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Style, style);
        });

        if (style.HasValue())
        {
            component.MarkupMatches(@$"<div style=""{style}"" class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitElementShouldRespectStyleChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        component.MarkupMatches(@"<div style=""padding: 1rem;"" class=""bit-elm"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-class"),
        DataRow(null)
    ]
    public void BitElementShouldRespectClass(string @class)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        component.MarkupMatches(@$"<div class=""bit-elm{cssClass}"" id:ignore></div>");
    }

    [TestMethod]
    public void BitElementShouldRespectClassChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        component.MarkupMatches(@"<div class=""bit-elm test-class"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("test-id"),
        DataRow(null)
    ]
    public void BitElementShouldRespectId(string id)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Id, id);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        component.MarkupMatches(@$"<div id=""{expectedId}"" class=""bit-elm""></div>");
    }

    [TestMethod,
        DataRow("0"),
        DataRow("-1"),
        DataRow(null)
    ]
    public void BitElementShouldRespectTabIndex(string tabIndex)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        if (tabIndex.HasValue())
        {
            component.MarkupMatches(@$"<div tabindex=""{tabIndex}"" class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Auto),
        DataRow(null)
    ]
    public void BitElementShouldRespectDir(BitDir? dir)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        if (dir.HasValue)
        {
            var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
            component.MarkupMatches(@$"<div dir=""{dir.Value.ToString().ToLower()}"" class=""bit-elm{cssClass}"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [TestMethod]
    public void BitElementShouldRespectDirChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        component.MarkupMatches(@"<div dir=""ltr"" class=""bit-elm"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectForceAnimation(bool forceAnimation)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.ForceAnimation, forceAnimation);
        });

        var cssClass = forceAnimation ? " bit-fam" : null;

        component.MarkupMatches(@$"<div class=""bit-elm{cssClass}"" id:ignore></div>");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Collapsed),
        DataRow(BitVisibility.Hidden)
    ]
    public void BitElementShouldRespectVisibility(BitVisibility visibility)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        switch (visibility)
        {
            case BitVisibility.Visible:
                component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
                break;
            case BitVisibility.Hidden:
                component.MarkupMatches(@"<div style=""visibility: hidden;"" class=""bit-elm"" id:ignore></div>");
                break;
            case BitVisibility.Collapsed:
                component.MarkupMatches(@"<div style=""display: none;"" class=""bit-elm"" id:ignore></div>");
                break;
        }
    }

    [TestMethod]
    public void BitElementShouldRespectVisibilityChangingAfterRender()
    {
        var component = RenderComponent<BitElement>();

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        component.MarkupMatches(@"<div style=""display: none;"" class=""bit-elm"" id:ignore></div>");
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow(null)
    ]
    public void BitElementShouldRespectAriaLabel(string ariaLabel)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        if (ariaLabel.HasValue())
        {
            component.MarkupMatches(@$"<div aria-label=""{ariaLabel}"" class=""bit-elm"" id:ignore></div>");
        }
        else
        {
            component.MarkupMatches(@"<div class=""bit-elm"" id:ignore></div>");
        }
    }

    [TestMethod,
        DataRow("Bit Blazor UI"),
        DataRow("<span>Bit Blazor UI</span>"),
        DataRow(null)
    ]
    public void BitElementShouldRespectChildContent(string childContent)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.AddChildContent(childContent);
        });

        component.MarkupMatches(@$"<div class=""bit-elm"" id:ignore>{childContent}</div>");
    }

    [TestMethod,
        DataRow("area"),
        DataRow("base"),
        DataRow("basefont"),
        DataRow("bgsound"),
        DataRow("br"),
        DataRow("col"),
        DataRow("embed"),
        DataRow("frame"),
        DataRow("hr"),
        DataRow("img"),
        DataRow("input"),
        DataRow("keygen"),
        DataRow("link"),
        DataRow("meta"),
        DataRow("param"),
        DataRow("source"),
        DataRow("track"),
        DataRow("wbr")
    ]
    public void BitElementShouldNotRenderChildContentInsideAVoidElement(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("this content has nowhere to go");
        });

        Assert.IsFalse(component.Markup.Contains("this content has nowhere to go"));
    }

    [TestMethod]
    public void BitElementShouldRenderChildContentInsideANonVoidElement()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, "span");
            parameters.AddChildContent("content");
        });

        component.MarkupMatches(@"<span class=""bit-elm"" id:ignore>content</span>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectNoWrapper(bool noWrapper)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.NoWrapper, noWrapper);
            parameters.Add(p => p.Element, "span");
            parameters.Add(p => p.Class, "test-class");
            parameters.AddChildContent("content");
        });

        if (noWrapper)
        {
            component.MarkupMatches("content");
        }
        else
        {
            component.MarkupMatches(@"<span class=""bit-elm test-class"" id:ignore>content</span>");
        }
    }

    [TestMethod]
    public void BitElementShouldRespectNoWrapperChangingAfterRender()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, "span");
            parameters.AddChildContent("content");
        });

        component.MarkupMatches(@"<span class=""bit-elm"" id:ignore>content</span>");

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Element, "span");
            parameters.AddChildContent("content");
        });

        component.MarkupMatches("content");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectStopPropagation(bool stopPropagation)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.StopPropagation, stopPropagation);
        });

        Assert.AreEqual(stopPropagation, component.Markup.Contains("onclick:stopPropagation"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitElementShouldRespectPreventDefault(bool preventDefault)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.PreventDefault, preventDefault);
        });

        Assert.AreEqual(preventDefault, component.Markup.Contains("onclick:preventDefault"));
    }

    [TestMethod]
    public void BitElementShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var elements = component.FindAll(".bit-elm");

        // The plain html attributes that match nothing on the component land on the rendered element as they are.
        elements[0].MarkupMatches(@"<div data-val-test=""bit"" class=""bit-elm"" id:ignore>I'm a element</div>");
    }

    [TestMethod]
    public void BitElementShouldMergeTheSplattedClassAndStyleWithItsOwn()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var element = component.FindAll(".bit-elm")[1];

        // A class splatted through a dictionary is kept alongside the class the component builds itself,
        // and the rest of the attributes the component would otherwise write itself are left alone.
        Assert.IsTrue(element.ClassList.Contains("bit-elm"));
        Assert.IsTrue(element.ClassList.Contains("from-attribute"));
        StringAssert.Contains(element.GetAttribute("style")!, "font-weight: bold;");
        Assert.AreEqual("rtl", element.GetAttribute("dir"));
        Assert.AreEqual("from-attribute", element.GetAttribute("id"));
        Assert.AreEqual("from attribute", element.GetAttribute("aria-label"));
        Assert.AreEqual("3", element.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldKeepTheLowercaseHtmlAttributesOfTheRootElement()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var element = component.FindAll(".bit-elm")[2];

        Assert.IsTrue(element.ClassList.Contains("bit-elm"));
        Assert.IsTrue(element.ClassList.Contains("lowercase-class"));
        StringAssert.Contains(element.GetAttribute("style")!, "color: blue;");
        Assert.AreEqual("lowercase-id", element.GetAttribute("id"));
        Assert.AreEqual("4", element.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldKeepASplattedDisabledAttributeOfAnEnabledElement()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var element = component.FindAll(".bit-elm")[3];

        Assert.IsTrue(element.HasAttribute("disabled"));
        Assert.IsFalse(element.HasAttribute("aria-disabled"));
        Assert.IsFalse(element.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitElementShouldCaptureItsRootElement()
    {
        var component = RenderComponent<BitElement>();

        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.RootElement.Id));
    }

    [TestMethod]
    public void BitElementShouldNotKeepAStaleRootElementWhenUnwrapped()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.AddChildContent("content");
        });

        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.RootElement.Id));

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.AddChildContent("content");
        });

        Assert.IsTrue(string.IsNullOrEmpty(component.Instance.RootElement.Id));
    }

    [TestMethod]
    public void BitElementShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitElementCascadingParamsTest>();

        var elements = component.FindAll(".bit-elm");

        // The first element takes everything from the cascading parameters.
        Assert.AreEqual("SECTION", elements[0].TagName);
        Assert.IsTrue(elements[0].ClassList.Contains("cascaded"));
        StringAssert.Contains(elements[0].OuterHtml, "onclick:stoppropagation");
        StringAssert.Contains(elements[0].OuterHtml, "onclick:preventdefault");
        StringAssert.Contains(elements[0].OuterHtml, "ondblclick:stoppropagation");
        StringAssert.Contains(elements[0].OuterHtml, "oncontextmenu:preventdefault");

        // The second one sets its own tag and its own stopped events, which the cascading parameters must not
        // overwrite - and a parameter it did set is not filled in from them even in part.
        Assert.AreEqual("ARTICLE", elements[1].TagName);
        Assert.IsTrue(elements[1].ClassList.Contains("cascaded"));
        StringAssert.Contains(elements[1].OuterHtml, "onpointerdown:stoppropagation");
        Assert.IsFalse(elements[1].OuterHtml.Contains("ondblclick:stoppropagation"));
        StringAssert.Contains(elements[1].OuterHtml, "oncontextmenu:preventdefault");

        // The third one is unwrapped by the cascading parameters, so it is not among the rendered elements at all.
        Assert.AreEqual(2, elements.Count);
        StringAssert.Contains(component.Markup, "unwrapped");
    }

    [TestMethod,
        DataRow("1div"),
        DataRow("-custom"),
        DataRow("div onclick=alert(1)"),
        DataRow("div>"),
        DataRow("<div"),
        DataRow("a/b"),
        DataRow("a&b"),
        DataRow("a=b"),
        DataRow("a\"b"),
        DataRow("a'b"),
        DataRow("a`b")
    ]
    public void BitElementShouldFallBackToDivForAnUnusableElementName(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.AddChildContent("content");
        });

        // A tag name carrying whitespace or any of the characters that end a tag would write markup of its own
        // rather than name an element, and one that does not begin with a letter names no element at all.
        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore>content</div>");
    }

    [TestMethod,
        DataRow("linearGradient"),
        DataRow("clipPath"),
        DataRow("feGaussianBlur"),
        DataRow("my-badge"),
        DataRow("x_y"),
        DataRow("a.b")
    ]
    public void BitElementShouldKeepAValidSvgOrCustomElementName(string element)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        component.MarkupMatches(@$"<{element} class=""bit-elm"" id:ignore></{element}>");
    }

    [TestMethod]
    public void BitElementShouldForceTheTabIndexOfADisabledElementThatHtmlCannotDisable()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, "a");
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.IsEnabled, false);
        });

        // The tab stop of the parameter is the one thing that would still reach a disabled element of a tag the
        // browser does not disable of itself, so the disabled state has the last word on it.
        Assert.AreEqual("-1", component.Find("a").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldKeepTheTabIndexOfADisabledElementThatHtmlDisables()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, "button");
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.IsEnabled, false);
        });

        var el = component.Find("button");

        Assert.IsTrue(el.HasAttribute("disabled"));
        Assert.AreEqual("3", el.GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible),
        DataRow(BitVisibility.Hidden),
        DataRow(BitVisibility.Collapsed)
    ]
    public void BitElementShouldRespectVisibilityWhileUnwrapped(BitVisibility visibility)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Visibility, visibility);
            parameters.AddChildContent("content");
        });

        // A collapsed component is one asked to be out of the DOM, which needs no element of its own to mean
        // something. Hiding an unwrapped element while keeping its space does need one, so it renders as it is.
        Assert.AreEqual(visibility is BitVisibility.Collapsed ? string.Empty : "content", component.Markup);
    }

    [TestMethod]
    public void BitElementShouldRespectCollapsingAnUnwrappedElementAfterRender()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.AddChildContent("content");
        });

        Assert.AreEqual("content", component.Markup);

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
            parameters.AddChildContent("content");
        });

        Assert.AreEqual(string.Empty, component.Markup);
    }

    [TestMethod,
        DataRow("dblclick", "ondblclick:stopPropagation"),
        DataRow("ondblclick", "ondblclick:stopPropagation"),
        DataRow("onKeyDown", "onkeydown:stopPropagation"),
        DataRow(" pointerdown ", "onpointerdown:stopPropagation")
    ]
    public void BitElementShouldRespectStopPropagationEvents(string @event, string expected)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.StopPropagationEvents, new[] { @event });
        });

        StringAssert.Contains(component.Markup, expected);
    }

    [TestMethod,
        DataRow("contextmenu", "oncontextmenu:preventDefault"),
        DataRow("oncontextmenu", "oncontextmenu:preventDefault"),
        DataRow("onDragOver", "ondragover:preventDefault"),
        DataRow(" submit ", "onsubmit:preventDefault")
    ]
    public void BitElementShouldRespectPreventDefaultEvents(string @event, string expected)
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.PreventDefaultEvents, new[] { @event });
        });

        StringAssert.Contains(component.Markup, expected);
    }

    [TestMethod]
    public void BitElementShouldRenderEveryNamedEventModifier()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.StopPropagationEvents, new[] { "dblclick", "pointerdown" });
            parameters.Add(p => p.PreventDefaultEvents, new[] { "contextmenu", "dragover" });
        });

        StringAssert.Contains(component.Markup, "ondblclick:stopPropagation");
        StringAssert.Contains(component.Markup, "onpointerdown:stopPropagation");
        StringAssert.Contains(component.Markup, "oncontextmenu:preventDefault");
        StringAssert.Contains(component.Markup, "ondragover:preventDefault");
    }

    [TestMethod]
    public void BitElementShouldIgnoreTheEmptyNamesAmongTheEventModifiers()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.StopPropagationEvents, new[] { "", "   ", "dblclick" });
            parameters.Add(p => p.PreventDefaultEvents, []);
        });

        StringAssert.Contains(component.Markup, "ondblclick:stopPropagation");
        Assert.IsFalse(component.Markup.Contains("on:stopPropagation"));
        Assert.IsFalse(component.Markup.Contains(":preventDefault"));
    }

    [TestMethod]
    public void BitElementShouldRespectEventModifiersChangingAfterRender()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.StopPropagationEvents, new[] { "dblclick" });
            parameters.AddChildContent("<span>content</span>");
        });

        StringAssert.Contains(component.Markup, "ondblclick:stopPropagation");

        component.Render(parameters =>
        {
            parameters.Add(p => p.StopPropagationEvents, new[] { "pointerdown", "keydown" });
            parameters.AddChildContent("<span>content</span>");
        });

        Assert.IsFalse(component.Markup.Contains("ondblclick:stopPropagation"));
        StringAssert.Contains(component.Markup, "onpointerdown:stopPropagation");
        StringAssert.Contains(component.Markup, "onkeydown:stopPropagation");
        // The frames of the content follow a block of attribute frames as long as the list, so a sequence number
        // that moved with that length would have thrown the content away and built it again.
        StringAssert.Contains(component.Markup, "<span>content</span>");
    }

    [TestMethod]
    public void BitElementShouldKeepASplattedAriaDisabledAttributeOfAnEnabledElement()
    {
        var component = RenderComponent<BitElementHtmlAttributesTest>();

        var element = component.FindAll(".bit-elm")[4];

        Assert.AreEqual("true", element.GetAttribute("aria-disabled"));
        Assert.IsFalse(element.ClassList.Contains("bit-dis"));
        Assert.IsFalse(element.HasAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitElementFocusAsyncShouldDoNothingWhileUnwrapped()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.NoWrapper, true);
            parameters.AddChildContent("content");
        });

        // Nothing is rendered, so there is no element to focus and no interop call to make of it either.
        await component.Instance.FocusAsync();
    }
}
