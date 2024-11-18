using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Link;

[TestClass]
public class BitLinkTests : BunitTestContext
{
    [DataTestMethod]
    public void BitLinkShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLink>();

        component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
    }

    [DataTestMethod,
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section"),
        DataRow(""),
        DataRow(null)
    ]
    public void BitLinkShouldRenderHref(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        if (href.HasNoValue())
        {
            component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");

            //check clickable element
            component.Find(".bit-lnk").Click();
        }
        else if (href.StartsWith('#'))
        {
            component.MarkupMatches(@"<a class=""bit-lnk"" id:ignore></a>");

            //check clickable element
            component.Find(".bit-lnk").Click();
        }
        else
        {
            component.MarkupMatches(@$"<a href=""{href}"" class=""bit-lnk"" id:ignore></a>");

            Assert.ThrowsException<MissingEventHandlerException>(() => component.Find(".bit-lnk").Click());
        }
    }

    [DataTestMethod,
        DataRow(null, "_blank"),
        DataRow(null, null),
        DataRow(null, ""),
        DataRow("https://bitplatform.dev", "_blank"),
        DataRow("https://bitplatform.dev", null),
        DataRow("https://bitplatform.dev", ""),
        DataRow("#go-to-section", "_blank"),
        DataRow("#go-to-section", null),
        DataRow("#go-to-section", "")
    ]
    public void BitLinkShouldRespectTarget(string href, string target)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Target, target);
        });

        if (href.HasValue())
        {
            if (href.StartsWith('#'))
            {
                component.MarkupMatches(@"<a class=""bit-lnk"" id:ignore></a>");
            }
            else
            {
                if (target.HasValue())
                {
                    component.MarkupMatches(@$"<a target=""{target}"" href=""{href}"" class=""bit-lnk"" id:ignore></a>");
                }
                else
                {
                    component.MarkupMatches(@$"<a class=""bit-lnk"" href=""{href}"" id:ignore></a>");
                }
            }
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectIsEnabled(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, false);
        });

        // This test specifically checks the disabled state of the component.
        // Since the enabled state is the default state and is checked in all tests, we focus on verifying the disabled behavior here.
        if (href.HasValue())
        {
            component.MarkupMatches(@"<a class=""bit-lnk bit-dis"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-dis"" disabled aria-disabled type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectIsEnabledChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@"<a class=""bit-lnk bit-dis"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-dis"" disabled aria-disabled type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, "font-size: 14px; color: red;"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "font-size: 14px; color: red;"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "font-size: 14px; color: red;"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectStyle(string href, string style)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Style, style);
        });

        var styleAttribute = style.HasValue() ? @$"style=""{style}""" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {styleAttribute} {GetHrefAttribute(href)} class=""bit-lnk"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {styleAttribute} class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section"),
    ]
    public void BitLinkShouldRespectStyleChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a style=""padding: 1rem;"" {GetHrefAttribute(href)} class=""bit-lnk"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button style=""padding: 1rem;"" class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, "test-class"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "test-class"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "test-class"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectClass(string href, string @class)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Class, @class);
        });

        var cssClass = @class.HasValue() ? $" {@class}" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectClassChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk test-class"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk test-class"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, "test-id"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "test-id"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "test-id"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectId(string href, string id)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Id, id);
            parameters.Add(p => p.Href, href);
        });

        var expectedId = id.HasValue() ? id : component.Instance.UniqueId.ToString();

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a id=""{expectedId}"" class=""bit-lnk"" {GetHrefAttribute(href)}></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button id=""{expectedId}"" class=""bit-lnk"" type=""button""></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, BitDir.Rtl),
        DataRow(null, BitDir.Ltr),
        DataRow(null, BitDir.Auto),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", BitDir.Rtl),
        DataRow("https://bitplatform.dev", BitDir.Ltr),
        DataRow("https://bitplatform.dev", BitDir.Auto),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", BitDir.Rtl),
        DataRow("#go-to-section", BitDir.Ltr),
        DataRow("#go-to-section", BitDir.Auto),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectDir(string href, BitDir? dir)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.Href, href);
        });

        var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
        var dirAttribute = dir.HasValue ? @$"dir=""{dir.Value.ToString().ToLower()}""" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {dirAttribute} class=""bit-lnk{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {dirAttribute} class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectDirChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a dir=""ltr"" class=""bit-lnk"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button dir=""ltr"" class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, BitVisibility.Visible),
        DataRow(null, BitVisibility.Collapsed),
        DataRow(null, BitVisibility.Hidden),
        DataRow("https://bitplatform.dev", BitVisibility.Visible),
        DataRow("https://bitplatform.dev", BitVisibility.Collapsed),
        DataRow("https://bitplatform.dev", BitVisibility.Hidden),
        DataRow("#go-to-section", BitVisibility.Visible),
        DataRow("#go-to-section", BitVisibility.Collapsed),
        DataRow("#go-to-section", BitVisibility.Hidden)
    ]
    public void BitLinkShouldRespectVisibility(string href, BitVisibility visibility)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Visibility, visibility);
        });

        var visibilityAttribute = visibility switch
        {
            BitVisibility.Visible => null,
            BitVisibility.Hidden => @"style=""visibility: hidden;""",
            BitVisibility.Collapsed => @"style=""display: none;"""
        };

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {visibilityAttribute} {GetHrefAttribute(href)} class=""bit-lnk"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {visibilityAttribute} class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectVisibilityChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a style=""display: none;"" {GetHrefAttribute(href)} class=""bit-lnk"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button style=""display: none;"" class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, "Bit Blazor UI"),
        DataRow(null, "<span>Bit Blazor UI</span>"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "Bit Blazor UI"),
        DataRow("https://bitplatform.dev", "<span>Bit Blazor UI</span>"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "Bit Blazor UI"),
        DataRow("#go-to-section", "<span>Bit Blazor UI</span>"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectChildContent(string href, string childContent)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.AddChildContent(childContent);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk"" {GetHrefAttribute(href)} id:ignore>{childContent}</a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk"" type=""button"" id:ignore>{childContent}</button>");
        }
    }

    [DataTestMethod,
        DataRow(null, "Bit Blazor UI"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "Bit Blazor UI"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "Bit Blazor UI"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectAriaLabel(string href, string ariaLabel)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var ariaLabelAttribute = ariaLabel.HasValue() ? @$"aria-label=""{ariaLabel}""" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {ariaLabelAttribute} class=""bit-lnk"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {ariaLabelAttribute} class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null, true),
        DataRow(null, false),
        DataRow("https://bitplatform.dev", true),
        DataRow("https://bitplatform.dev", false),
        DataRow("#go-to-section", true),
        DataRow("#go-to-section", false)
    ]
    public void BitLinkShouldRespectUnderlined(string href, bool underlined)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Underlined, underlined);
        });

        var cssClass = underlined ? " bit-lnk-und" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectUnderlinedChangingAfterRender(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        MatchSimpleMarkup(component, href);

        component.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Underlined, true);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-und"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-und"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLinkButtonOnClickTest(bool isEnabled)
    {
        var currentCount = 0;
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => currentCount++);
        });

        var bitLinkButton = component.Find(".bit-lnk");

        bitLinkButton.Click();

        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLinkScrollIntoViewTest(bool isEnabled)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Href, "#go-to-section");
        });

        var bitLinkButton = component.Find(".bit-lnk");

        bitLinkButton.Click();

        if (isEnabled)
        {
            Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.scrollElementIntoView");
        }
        else
        {
            Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.scrollElementIntoView");
        }
    }

    [DataTestMethod,
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section"),
        DataRow(""),
        DataRow(null)]
    public void BitLinkShouldRespectHtmlAttributes(string href)
    {
        var component = RenderComponent<BitLinkHtmlAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a data-val-test=""bit"" class=""bit-lnk"" {GetHrefAttribute(href)} id:ignore>I'm a link</a>");
        }
        else
        {
            component.MarkupMatches(@"<button data-val-test=""bit"" class=""bit-lnk"" type=""button"" id:ignore>I'm a link</button>");
        }
    }

    [DataTestMethod,
        DataRow(null, null),
        DataRow(null, BitLinkRel.Bookmark),
        DataRow(null, BitLinkRel.Bookmark | BitLinkRel.Alternate),
        DataRow("https://bitplatform.dev", null),
        DataRow("https://bitplatform.dev", BitLinkRel.Bookmark),
        DataRow("https://bitplatform.dev", BitLinkRel.Bookmark | BitLinkRel.Alternate),
        DataRow("#go-to-section", null),
        DataRow("#go-to-section", BitLinkRel.Bookmark),
        DataRow("#go-to-section", BitLinkRel.Bookmark | BitLinkRel.Alternate)
    ]
    public void BitLinkShouldRespectTarget(string href, BitLinkRel? rel)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Rel, rel);
            parameters.Add(p => p.Href, href);
        });

        if (href.HasValue())
        {
            if (href.StartsWith('#'))
            {
                component.MarkupMatches(@"<a class=""bit-lnk"" id:ignore></a>");
            }
            else
            {
                if (rel.HasValue)
                {
                    var rels = string.Join(" ", Enum.GetValues(typeof(BitLinkRel)).Cast<BitLinkRel>().Where(r => rel.Value.HasFlag(r)).Select(r => r.ToString().ToLower()));

                    component.MarkupMatches(@$"<a rel=""{rels}"" href=""{href}"" class=""bit-lnk"" id:ignore></a>");
                }
                else
                {
                    component.MarkupMatches(@$"<a class=""bit-lnk"" href=""{href}"" id:ignore></a>");
                }
            }
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }



    private void MatchSimpleMarkup(IRenderedComponent<BitLink> component, string href)
    {
        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {GetHrefAttribute(href)} class=""bit-lnk"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
        }
    }

    private string GetHrefAttribute(string href) => href.StartsWith('#') ? null : @$"href=""{href}""";
}
