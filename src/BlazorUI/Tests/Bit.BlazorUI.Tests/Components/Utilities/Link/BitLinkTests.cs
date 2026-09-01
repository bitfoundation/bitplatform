using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Components;
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

                    component.MarkupMatches(@$"<a target=""{target}"" {relAttribute} href=""{href}"" class=""bit-lnk bit-lnk-pri"" id:ignore></a>");
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
            component.MarkupMatches(@"<a tabindex=""-1"" role=""link"" aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" id:ignore></a>");
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
            component.MarkupMatches(@"<a tabindex=""-1"" role=""link"" aria-disabled=""true"" class=""bit-lnk bit-lnk-pri bit-dis"" id:ignore></a>");
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
