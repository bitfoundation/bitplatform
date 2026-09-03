using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Link;

[TestClass]
public class BitLinkTests : BunitTestContext
{
    [TestMethod]
    public void BitLinkShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLink>();

        component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
    }

    [TestMethod,
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
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
        else if (href.StartsWith('#'))
        {
            // Hash links keep their href so the anchor stays focusable and keyboard-operable while the
            // actual scrolling happens in the click handler with the default navigation prevented.
            component.MarkupMatches(@$"<a href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<a href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }

        //check clickable element (every render mode wires the click handler)
        component.Find(".bit-lnk").Click();
    }

    [TestMethod,
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
                component.MarkupMatches(@$"<a href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
            }
            else
            {
                if (target.HasValue())
                {
                    var relAttribute = target is "_blank" ? @"rel=""noopener""" : null;

                    // A new-tab link also says so, in text drawn nowhere but read out with the link.
                    var hint = target is "_blank" ? @"<span class=""bit-lnk-hnt"">(opens in a new tab)</span>" : null;

                    component.MarkupMatches(@$"<a target=""{target}"" {relAttribute} href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore>{hint}</a>");
                }
                else
                {
                    component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri"" href=""{href}"" id:ignore></a>");
                }
            }
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            // A disabled anchor loses its href, so the role and aria-disabled attributes keep it perceivable
            // as a (disabled) link for assistive technologies.
            component.MarkupMatches(@"<a role=""link"" aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button tabindex=""-1"" class=""bit-lnk bit-lnk-pri bit-dis"" disabled aria-disabled=""true"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@"<a role=""link"" aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button tabindex=""-1"" class=""bit-lnk bit-lnk-pri bit-dis"" disabled aria-disabled=""true"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a {styleAttribute} {GetHrefAttribute(href)} class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {styleAttribute} class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Style, "padding: 1rem;");
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a style=""padding: 1rem;"" {GetHrefAttribute(href)} class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button style=""padding: 1rem;"" class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk bit-lnk-pri{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Class, "test-class");
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri test-class"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri test-class"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a id=""{expectedId}"" class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)}></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button id=""{expectedId}"" class=""bit-lnk bit-lnk-pri"" type=""button""></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a {dirAttribute} class=""bit-lnk bit-lnk-pri{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {dirAttribute} class=""bit-lnk bit-lnk-pri{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Ltr);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a dir=""ltr"" class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button dir=""ltr"" class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            BitVisibility.Collapsed => @"style=""display: none;""",
            _ => null
        };

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {visibilityAttribute} {GetHrefAttribute(href)} class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {visibilityAttribute} class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a style=""display: none;"" {GetHrefAttribute(href)} class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button style=""display: none;"" class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)} id:ignore>{childContent}</a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore>{childContent}</button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a {ariaLabelAttribute} class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {ariaLabelAttribute} class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk bit-lnk-pri{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
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

        component.Render(parameters =>
        {
            parameters.Add(p => p.Underlined, true);
        });

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri bit-lnk-und"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri bit-lnk-und"" type=""button"" id:ignore></button>");
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

    [TestMethod]
    public void BitLinkShouldNotScrollForBareHashHref()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "#");
        });

        component.Find(".bit-lnk").Click();

        // A bare # href names no element, so clicking it must not fire the scroll-into-view interop with an empty id.
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.scrollElementIntoView");
    }

    [TestMethod,
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
            component.MarkupMatches(@$"<a data-val-test=""bit"" class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)} id:ignore>I'm a link</a>");
        }
        else
        {
            component.MarkupMatches(@"<button data-val-test=""bit"" class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore>I'm a link</button>");
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(null, BitLinkRels.Bookmark),
        DataRow(null, BitLinkRels.Bookmark | BitLinkRels.Alternate),
        DataRow("https://bitplatform.dev", null),
        DataRow("https://bitplatform.dev", BitLinkRels.Bookmark),
        DataRow("https://bitplatform.dev", BitLinkRels.Bookmark | BitLinkRels.Alternate),
        DataRow("#go-to-section", null),
        DataRow("#go-to-section", BitLinkRels.Bookmark),
        DataRow("#go-to-section", BitLinkRels.Bookmark | BitLinkRels.Alternate)
    ]
    public void BitLinkShouldRespectRel(string href, BitLinkRels? rel)
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
                // The rel attribute is meaningless on an in-page hash link, so it is never rendered there.
                component.MarkupMatches(@$"<a href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
            }
            else
            {
                if (rel.HasValue)
                {
                    var rels = string.Join(" ", Enum.GetValues(typeof(BitLinkRels)).Cast<BitLinkRels>().Where(r => rel.Value.HasFlag(r)).Select(r => r.ToString().ToLower()));

                    component.MarkupMatches(@$"<a rel=""{rels}"" href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
                }
                else
                {
                    component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri"" href=""{href}"" id:ignore></a>");
                }
            }
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }



    [TestMethod,
        DataRow(null, "bit-lnk-pri"),
        DataRow(BitColor.Primary, "bit-lnk-pri"),
        DataRow(BitColor.Secondary, "bit-lnk-sec"),
        DataRow(BitColor.Tertiary, "bit-lnk-ter"),
        DataRow(BitColor.Info, "bit-lnk-inf"),
        DataRow(BitColor.Success, "bit-lnk-suc"),
        DataRow(BitColor.Warning, "bit-lnk-wrn"),
        DataRow(BitColor.SevereWarning, "bit-lnk-swr"),
        DataRow(BitColor.Error, "bit-lnk-err"),
        DataRow(BitColor.PrimaryBackground, "bit-lnk-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-lnk-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-lnk-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-lnk-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-lnk-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-lnk-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-lnk-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-lnk-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-lnk-tbr")
    ]
    public void BitLinkShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        component.MarkupMatches(@$"<a class=""bit-lnk {expectedClass}"" href=""https://bitplatform.dev"" id:ignore></a>");
    }

    [TestMethod,
        DataRow(null, true),
        DataRow(null, false),
        DataRow("https://bitplatform.dev", true),
        DataRow("https://bitplatform.dev", false),
        DataRow("#go-to-section", true),
        DataRow("#go-to-section", false)
    ]
    public void BitLinkShouldRespectNoUnderline(string href, bool noUnderline)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.NoUnderline, noUnderline);
        });

        var cssClass = noUnderline ? " bit-lnk-nun" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri{cssClass}"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk bit-lnk-pri{cssClass}"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLinkShouldRespectNoColor(bool noColor)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.NoColor, noColor);
        });

        var cssClass = noColor ? " bit-lnk-ncl" : null;

        component.MarkupMatches(@$"<a class=""bit-lnk bit-lnk-pri{cssClass}"" href=""https://bitplatform.dev"" id:ignore></a>");
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public async Task BitLinkFocusAsyncShouldFocusTheRootElement(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        await component.Instance.FocusAsync();

        var invocation = Context.JSInterop.Invocations.Last(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal));
        var reference = (ElementReference)invocation.Arguments[0]!;

        Assert.AreEqual(component.Find(".bit-lnk").GetAttribute("blazor:elementreference"), reference.Id);
    }

    [TestMethod,
        DataRow(null, "1"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev", "1"),
        DataRow("https://bitplatform.dev", null),
        DataRow("#go-to-section", "1"),
        DataRow("#go-to-section", null)
    ]
    public void BitLinkShouldRespectTabIndex(string href, string tabIndex)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.TabIndex, tabIndex);
        });

        var tabIndexAttribute = tabIndex.HasValue() ? @$"tabindex=""{tabIndex}""" : null;

        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {tabIndexAttribute} class=""bit-lnk bit-lnk-pri"" {GetHrefAttribute(href)} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button {tabIndexAttribute} class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectAllowDisabledFocus(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AllowDisabledFocus, true);
        });

        if (href.HasValue())
        {
            // The disabled anchor has no href to make it focusable, so an explicit zero tabindex keeps it in the tab order.
            component.MarkupMatches(@"<a tabindex=""0"" role=""link"" aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" id:ignore></a>");
        }
        else
        {
            // The button conveys its disabled state via aria-disabled alone, so it stays natively focusable.
            component.MarkupMatches(@"<button aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" type=""button"" id:ignore></button>");
        }
    }

    [TestMethod,
        DataRow("_blank", null, "noopener"),
        DataRow("_blank", BitLinkRels.NoFollow, "nofollow noopener"),
        DataRow("_blank", BitLinkRels.NoOpener, "noopener"),
        DataRow("_blank", BitLinkRels.NoReferrer, "noreferrer"),
        DataRow("_blank", BitLinkRels.NoOpener | BitLinkRels.NoReferrer, "noopener noreferrer"),
        DataRow("_blank", BitLinkRels.Opener, "opener"),
        DataRow("_blank", BitLinkRels.Sponsored, "sponsored noopener"),
        DataRow("_self", null, null),
        DataRow(null, null, null)
    ]
    public void BitLinkShouldAddNoOpenerToBlankTargetLinks(string target, BitLinkRels? rel, string expectedRel)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Rel, rel);
            parameters.Add(p => p.Target, target);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        var anchor = component.Find(".bit-lnk");

        // A _blank target without an explicit opener-related Rel gets an automatic noopener for security.
        Assert.AreEqual(string.IsNullOrEmpty(expectedRel) is false, anchor.HasAttribute("rel"));

        if (expectedRel is not null)
        {
            Assert.AreEqual(expectedRel, anchor.GetAttribute("rel"));
        }
    }

    [TestMethod,
        DataRow(BitLinkRels.Me, "me"),
        DataRow(BitLinkRels.Opener, "opener"),
        DataRow(BitLinkRels.Ugc, "ugc"),
        DataRow(BitLinkRels.PrivacyPolicy, "privacy-policy"),
        DataRow(BitLinkRels.TermsOfService, "terms-of-service"),
        DataRow(BitLinkRels.Sponsored | BitLinkRels.Ugc, "sponsored ugc"),
        DataRow(BitLinkRels.NoFollow | BitLinkRels.PrivacyPolicy, "nofollow privacy-policy")
    ]
    public void BitLinkShouldRenderRelValuesWithTheirHtmlNames(BitLinkRels rel, string expectedRel)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Rel, rel);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        // The multi-word rel values are hyphenated in HTML, so the enum member name alone cannot be the source of the attribute.
        Assert.AreEqual(expectedRel, component.Find(".bit-lnk").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitLinkRelShouldFollowTargetAndHrefChanges()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual("noopener", component.Find(".bit-lnk").GetAttribute("rel"));

        component.Render(parameters => parameters.Add(p => p.Target, "_self"));

        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("rel"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "#go-to-section");
        });

        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("rel"));
    }

    [TestMethod,
        DataRow(null, "file.pdf"),
        DataRow(null, null),
        DataRow("https://bitplatform.dev/file.pdf", ""),
        DataRow("https://bitplatform.dev/file.pdf", "file.pdf"),
        DataRow("https://bitplatform.dev/file.pdf", null)
    ]
    public void BitLinkShouldRespectDownload(string href, string download)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Download, download);
        });

        var root = component.Find(".bit-lnk");

        if (href.HasValue() && download is not null)
        {
            Assert.IsTrue(root.HasAttribute("download"));
            Assert.AreEqual(download, root.GetAttribute("download"));
        }
        else
        {
            // The download attribute only belongs on a rendered anchor.
            Assert.IsFalse(root.HasAttribute("download"));
        }

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

        // A disabled link cannot be navigated, so it must not offer a download either.
        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("download"));
    }

    [TestMethod,
        DataRow("https://bitplatform.dev", true),
        DataRow("https://bitplatform.dev", false),
        DataRow("#go-to-section", true),
        DataRow("#go-to-section", false)
    ]
    public void BitLinkAnchorOnClickTest(string href, bool isEnabled)
    {
        var currentCount = 0;
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => currentCount++);
        });

        component.Find(".bit-lnk").Click();

        // OnClick fires in the anchor render modes too, alongside the navigation or the scroll.
        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [TestMethod,
        DataRow(null, true),
        DataRow(null, false),
        DataRow("https://bitplatform.dev", true),
        DataRow("https://bitplatform.dev", false),
        DataRow("#go-to-section", true),
        DataRow("#go-to-section", false)
    ]
    public void BitLinkShouldRespectStopPropagation(string href, bool stopPropagation)
    {
        var component = RenderComponent<BitLinkPropagationTest>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.StopPropagation, stopPropagation);
        });

        component.Find(".bit-lnk").Click();

        // the click a link answers is still a click on the page, so it reaches a clickable container around
        // it unless the link is told to keep it to itself
        Assert.AreEqual(1, component.Instance.LinkClickCount);
        Assert.AreEqual(stopPropagation ? 0 : 1, component.Instance.ContainerClickCount);
    }



    [TestMethod]
    public void BitLinkShouldRenderTheIconBeforeTheContentByDefault()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IconName, "Link");
            parameters.AddChildContent("bit");
        });

        // The glyph says nothing a reader has not already been told by the link text, so it is decoration.
        component.MarkupMatches(@"<a href=""https://bitplatform.dev"" class=""bit-lnk bit-lnk-pri"" id:ignore>
                                    <i class=""bit-lnk-icn bit-lnk-sic bit-icon bit-icon--Link"" aria-hidden=""true""></i>bit
                                  </a>");
    }

    [TestMethod]
    public void BitLinkShouldRenderTheIconAfterTheContentAtTheEndPosition()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IconName, "Link");
            parameters.Add(p => p.IconPosition, BitIconPosition.End);
            parameters.AddChildContent("bit");
        });

        component.MarkupMatches(@"<a href=""https://bitplatform.dev"" class=""bit-lnk bit-lnk-pri"" id:ignore>
                                    bit<i class=""bit-lnk-icn bit-lnk-eic bit-icon bit-icon--Link"" aria-hidden=""true""></i>
                                  </a>");
    }

    [TestMethod]
    public void BitLinkShouldRenderTheIconOnTheButtonRenderModeToo()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.IconName, "Link");
            parameters.AddChildContent("bit");
        });

        component.MarkupMatches(@"<button type=""button"" class=""bit-lnk bit-lnk-pri"" id:ignore>
                                    <i class=""bit-lnk-icn bit-lnk-sic bit-icon bit-icon--Link"" aria-hidden=""true""></i>bit
                                  </button>");
    }

    [TestMethod]
    public void BitLinkIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.IconName, "Link");
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-house"));
        });

        Assert.AreEqual("bit-lnk-icn bit-lnk-sic fa-solid fa-house", component.Find(".bit-lnk-icn").GetAttribute("class"));
    }

    [TestMethod]
    public void BitLinkShouldRenderNoIconElementWithoutAnIcon()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual(0, component.FindAll(".bit-lnk-icn").Count);
    }

    [TestMethod,
        DataRow(null, ""),
        DataRow(BitSize.Small, " bit-lnk-sm"),
        DataRow(BitSize.Medium, " bit-lnk-md"),
        DataRow(BitSize.Large, " bit-lnk-lg")
    ]
    public void BitLinkShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Size, size);
        });

        component.MarkupMatches(@$"<a href=""https://bitplatform.dev"" class=""bit-lnk bit-lnk-pri{expectedClass}"" id:ignore></a>");
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectTitle(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.Title, "the bit platform");
        });

        Assert.AreEqual("the bit platform", component.Find(".bit-lnk").GetAttribute("title"));
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev")
    ]
    public void BitLinkShouldRespectAutoFocus(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.AutoFocus, true);
        });

        Assert.IsTrue(component.Find(".bit-lnk").HasAttribute("autofocus"));

        component.Render(parameters => parameters.Add(p => p.AutoFocus, false));

        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitLinkShouldKeepTheHrefAndTheClickHandlerWithPreventDefault()
    {
        var currentCount = 0;
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.PreventDefault, true);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.OnClick, () => currentCount++);
        });

        var anchor = component.Find(".bit-lnk");

        // The suppressed navigation is the browser's to skip - what the component owes is the href that a
        // middle click and "copy link address" still reach, and the handler that answers the plain click.
        Assert.AreEqual("https://bitplatform.dev", anchor.GetAttribute("href"));

        anchor.Click();

        Assert.AreEqual(1, currentCount);
    }

    [TestMethod]
    public void BitLinkShouldAnnounceABlankTargetLinkAsOpeningANewTab()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.AddChildContent("bit");
        });

        Assert.AreEqual("(opens in a new tab)", component.Find(".bit-lnk-hnt").TextContent);
    }

    [TestMethod,
        DataRow("_self"),
        DataRow(""),
        DataRow(null)
    ]
    public void BitLinkShouldNotAnnounceANewTabForAnyOtherTarget(string target)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, target);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual(0, component.FindAll(".bit-lnk-hnt").Count);
    }

    [TestMethod]
    public void BitLinkShouldNotAnnounceANewTabForAHashHrefOrADisabledLink()
    {
        // A hash link scrolls instead of navigating and a disabled one goes nowhere at all, so neither
        // renders the target it was given - and neither says it opens a tab it will never open.
        var hashLink = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "#go-to-section");
        });

        Assert.AreEqual(0, hashLink.FindAll(".bit-lnk-hnt").Count);

        var disabledLink = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual(0, disabledLink.FindAll(".bit-lnk-hnt").Count);
    }

    [TestMethod,
        DataRow("(opens in a new window)", "(opens in a new window)"),
        DataRow("", null),
        DataRow(null, "(opens in a new tab)")
    ]
    public void BitLinkShouldRespectNewTabHint(string newTabHint, string expected)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.NewTabHint, newTabHint);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        var hints = component.FindAll(".bit-lnk-hnt");

        if (expected is null)
        {
            // An empty hint is the announcement being taken off, not an empty one being made.
            Assert.AreEqual(0, hints.Count);
        }
        else
        {
            Assert.AreEqual(expected, hints[0].TextContent);
        }
    }

    [TestMethod]
    public void BitLinkShouldRespectNoNewTabHint()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.NoNewTabHint, true);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual(0, component.FindAll(".bit-lnk-hnt").Count);
    }

    [TestMethod]
    public void BitLinkShouldAppendTheNewTabHintToTheAriaLabelInsteadOfTheContent()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.AriaLabel, "bit platform");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        // An aria-label replaces the content rather than adding to it, so hidden text beside the content
        // would never be read out - the sentence has to be part of the name itself.
        Assert.AreEqual(0, component.FindAll(".bit-lnk-hnt").Count);
        Assert.AreEqual("bit platform (opens in a new tab)", component.Find(".bit-lnk").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitLinkNoUnderlineShouldWinOverUnderlined()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Underlined, true);
            parameters.Add(p => p.NoUnderline, true);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        // Two parameters asking for opposite things are answered here rather than by whichever css rule
        // happens to be declared last.
        component.MarkupMatches(@"<a href=""https://bitplatform.dev"" class=""bit-lnk bit-lnk-nun bit-lnk-pri"" id:ignore></a>");
    }

    [TestMethod]
    public void BitLinkShouldPassFocusToTheScrollIntoViewInterop()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "#go-to-section");
        });

        component.Find(".bit-lnk").Click();

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Utils.scrollElementIntoView"].Single();

        // The scroll takes the keyboard with it, so the next Tab carries on from the destination.
        Assert.AreEqual("go-to-section", invocation.Arguments[0]);
        Assert.AreEqual(true, invocation.Arguments[1]);
    }

    [TestMethod,
        DataRow("rel", "me"),
        DataRow("title", "the bit platform"),
        DataRow("aria-label", "bit platform"),
        DataRow("hreflang", "en"),
        DataRow("referrerpolicy", "no-referrer")
    ]
    public void BitLinkShouldKeepSplattedAttributesTheParametersLeaveUnset(string name, string value)
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Attributes, new Dictionary<string, object> { [name] = value });
        });

        // An attribute the component writes after the splat wins over the splatted one, and a null takes it
        // off the element - so every attribute a parameter can write has to read the splatted value back.
        Assert.AreEqual(value, component.Find(".bit-lnk").GetAttribute(name));
    }

    [TestMethod]
    public void BitLinkShouldKeepASplattedTargetAndDownload()
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev/file.pdf");
            parameters.Add(p => p.Attributes, new Dictionary<string, object>
            {
                ["target"] = "_blank",
                ["download"] = "file.pdf"
            });
        });

        var anchor = component.Find(".bit-lnk");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("file.pdf", anchor.GetAttribute("download"));

        // Everything the target decides follows the target actually on the element, however it arrived.
        Assert.AreEqual("noopener", anchor.GetAttribute("rel"));
        Assert.AreEqual("(opens in a new tab)", component.Find(".bit-lnk-hnt").TextContent);
    }

    [TestMethod]
    public void BitLinkShouldRenderABareSplattedDownloadAsTheBareAttribute()
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev/file.pdf");
            parameters.Add(p => p.Attributes, new Dictionary<string, object> { ["download"] = true });
        });

        var anchor = component.Find(".bit-lnk");

        // A download written without a value is the browser being told to save the file under whatever name
        // the server gives it, so the value it arrived as is handed back rather than stringified into one.
        Assert.IsTrue(anchor.HasAttribute("download"));
        Assert.AreEqual(string.Empty, anchor.GetAttribute("download"));
    }

    [TestMethod]
    public void BitLinkShouldAddNoOpenerBesideASplattedRel()
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Attributes, new Dictionary<string, object> { ["rel"] = "nofollow" });
        });

        Assert.AreEqual("nofollow noopener", component.Find(".bit-lnk").GetAttribute("rel"));
    }

    [TestMethod,
        DataRow("noopener"),
        DataRow("noreferrer"),
        DataRow("opener")
    ]
    public void BitLinkShouldNotAddNoOpenerBesideASplattedOpenerRel(string rel)
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Attributes, new Dictionary<string, object> { ["rel"] = rel });
        });

        // An author who has already said what the opener relationship should be has said it.
        Assert.AreEqual(rel, component.Find(".bit-lnk").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitLinkShouldKeepTheRelOfADisabledLink()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Rel, BitLinkRels.License);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        // A rel is what the link is a link to rather than something it does, so being unfollowable now does
        // not stop it from being the license - unlike the target and the download, which are both dropped.
        var anchor = component.Find(".bit-lnk");

        Assert.AreEqual("license", anchor.GetAttribute("rel"));
        Assert.IsFalse(anchor.HasAttribute("target"));
    }

    [TestMethod]
    public void BitLinkShouldDropTheTargetOfAHashHref()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "#go-to-section");
        });

        // The click is answered by the component rather than by the browser, so the target names a context
        // nothing is ever opened in.
        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("target"));
    }

    [TestMethod,
        DataRow(BitNavAriaCurrent.Page, "page"),
        DataRow(BitNavAriaCurrent.Step, "step"),
        DataRow(BitNavAriaCurrent.Location, "location"),
        DataRow(BitNavAriaCurrent.Date, "date"),
        DataRow(BitNavAriaCurrent.Time, "time"),
        DataRow(BitNavAriaCurrent.True, "true"),
        DataRow(null, null)
    ]
    public void BitLinkShouldRespectAriaCurrent(BitNavAriaCurrent? ariaCurrent, string expected)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.AriaCurrent, ariaCurrent);
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        var anchor = component.Find(".bit-lnk");

        Assert.AreEqual(expected is not null, anchor.HasAttribute("aria-current"));

        if (expected is not null)
        {
            Assert.AreEqual(expected, anchor.GetAttribute("aria-current"));
        }
    }

    [TestMethod]
    public void BitLinkShouldRespectAriaCurrentOnTheButtonRenderModeToo()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.AriaCurrent, BitNavAriaCurrent.Step);
        });

        Assert.AreEqual("step", component.Find(".bit-lnk").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitLinkShouldPointASplattedAriaLabelledByAtTheNewTabHintToo()
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Target, "_blank");
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Attributes, new Dictionary<string, object> { ["aria-labelledby"] = "heading" });
        });

        var anchor = component.Find(".bit-lnk");
        var hint = component.Find(".bit-lnk-hnt");

        // An aria-labelledby names the link with other elements rather than with its own content, so the
        // sentence is only read out once the list points at it as well.
        Assert.AreEqual($"heading {hint.Id}", anchor.GetAttribute("aria-labelledby"));
        Assert.AreEqual("(opens in a new tab)", hint.TextContent);
    }

    [TestMethod]
    public void BitLinkShouldTakeTheCascadedParameterValuesItWasNotGiven()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Params, new BitLinkParams
            {
                Underlined = true,
                Target = "_blank",
                Size = BitSize.Large,
                Color = BitColor.Error,
                Rel = BitLinkRels.NoFollow,
                NewTabHint = "(opens in a new window)"
            });
        });

        var anchor = component.Find(".bit-lnk");

        component.MarkupMatches(@"<a href=""https://bitplatform.dev"" target=""_blank"" rel=""nofollow noopener""
                                     class=""bit-lnk bit-lnk-und bit-lnk-lg bit-lnk-err"" id:ignore>
                                    bit<span class=""bit-lnk-hnt"">(opens in a new window)</span>
                                  </a>");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));

        // The rel string is built while the parameters are being set, before the cascade is applied, so a rel
        // that arrives from the cascade has to send it round again.
        Assert.AreEqual("nofollow noopener", anchor.GetAttribute("rel"));

        // The sentence a new-tab link is announced with is the one an app most needs to say in its own
        // language, and saying it once for the whole app is what the cascade is for.
        Assert.AreEqual("(opens in a new window)", component.Find(".bit-lnk-hnt").TextContent);
    }

    [TestMethod]
    public void BitLinkShouldKeepTheParametersItWasGivenOverTheCascadedOnes()
    {
        var component = RenderComponent<BitLinkParamsTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Params, new BitLinkParams
            {
                Underlined = true,
                Color = BitColor.Error,
                NoNewTabHint = true
            });
            parameters.Add(p => p.Overrides, new Dictionary<string, object>
            {
                [nameof(BitLink.Color)] = BitColor.Success,
                [nameof(BitLink.Target)] = "_blank",
                [nameof(BitLink.NoNewTabHint)] = false
            });
        });

        var anchor = component.Find(".bit-lnk");

        // A cascade is a default rather than an override: it fills in only what the link left unsaid.
        Assert.IsTrue(anchor.ClassList.Contains("bit-lnk-und"));
        Assert.IsTrue(anchor.ClassList.Contains("bit-lnk-suc"));
        Assert.IsFalse(anchor.ClassList.Contains("bit-lnk-err"));
        Assert.AreEqual("(opens in a new tab)", component.Find(".bit-lnk-hnt").TextContent);
    }

    [TestMethod,
        DataRow(null),
        DataRow("https://bitplatform.dev"),
        DataRow("#go-to-section")
    ]
    public void BitLinkShouldRespectAriaDescription(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.AriaDescription, "PDF, 2.4 megabytes");
            parameters.AddChildContent("bit");
        });

        var root = component.Find(".bit-lnk");
        var description = component.Find(".bit-lnk-dsc");

        // The description is read out after the name rather than as part of it, so it lives beside the link and
        // is pointed at rather than written into it.
        Assert.AreEqual(root.GetAttribute("aria-describedby"), description.Id);
        Assert.AreEqual("PDF, 2.4 megabytes", description.TextContent);
        Assert.AreEqual("bit", root.TextContent);
    }

    [TestMethod]
    public void BitLinkShouldRenderNoDescriptionElementWithoutAnAriaDescription()
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        Assert.AreEqual(0, component.FindAll(".bit-lnk-dsc").Count);
        Assert.IsFalse(component.Find(".bit-lnk").HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitLinkShouldKeepASplattedAriaDescribedByBesideItsOwnDescription()
    {
        var component = RenderComponent<BitLinkSplattedAttributesTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
            parameters.Add(p => p.Attributes, new Dictionary<string, object>
            {
                ["aria-describedby"] = "note",
                [nameof(BitLink.AriaDescription)] = "PDF, 2.4 megabytes"
            });
        });

        var described = component.Find(".bit-lnk").GetAttribute("aria-describedby");

        // Pointing the link at a description of its own is one more thing it is described by, never a
        // replacement for what the app was already pointing it at.
        Assert.AreEqual($"note {component.Find(".bit-lnk-dsc").Id}", described);
    }

    [TestMethod]
    public void BitLinkShouldSplatEventHandlersOntoTheRenderedElement()
    {
        var component = RenderComponent<BitLinkEventSplatTest>(parameters =>
        {
            parameters.Add(p => p.Href, "https://bitplatform.dev");
        });

        component.Find(".bit-lnk").DoubleClick();

        // Everything an anchor accepts goes through the splatted attributes, event handlers included - which is
        // what keeps the component from having to grow a parameter for every event the DOM already has.
        Assert.AreEqual(1, component.Instance.DoubleClickCount);
    }

    private void MatchSimpleMarkup(IRenderedComponent<BitLink> component, string href)
    {
        if (href.HasValue())
        {
            component.MarkupMatches(@$"<a {GetHrefAttribute(href)} class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@"<button class=""bit-lnk bit-lnk-pri"" type=""button"" id:ignore></button>");
        }
    }

    private string? GetHrefAttribute(string href) => @$"href=""{href}""";
}
