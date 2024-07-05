using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                    component.MarkupMatches(@$"<a target=""{target}"" class=""bit-lnk"" href=""{href}"" id:ignore></a>");
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
        DataRow(null, true),
        DataRow(null, false),
        DataRow("https://bitplatform.dev", true),
        DataRow("https://bitplatform.dev", false),
        DataRow("#go-to-section", true),
        DataRow("#go-to-section", false)
    ]
    public void BitLinkShouldRespectIsEnabled(string href, bool isEnabled)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var cssClass = isEnabled ? null : " bit-dis";

        if (href.HasValue())
        {
            if (isEnabled && href.StartsWith('#') is false)
            {
                component.MarkupMatches(@$"<a class=""bit-lnk"" href=""{href}"" id:ignore></a>");
            }
            else
            {
                component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" id:ignore></a>");
            }
        }
        else
        {
            if (isEnabled)
            {
                component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
            }
            else
            {
                component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" disabled aria-disabled type=""button"" id:ignore></button>");
            }
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

        if (href.HasValue())
        {
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            if (style.HasValue())
            {
                component.MarkupMatches(@$"<a style=""{style}"" class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
            }
            else
            {
                component.MarkupMatches(@$"<a class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
            }
        }
        else
        {
            if (style.HasValue())
            {
                component.MarkupMatches(@$"<button style=""{style}"" class=""bit-lnk"" type=""button"" id:ignore></button>");
            }
            else
            {
                component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
            }
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
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" {hrefAttribute} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
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
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            component.MarkupMatches(@$"<a id=""{expectedId}"" class=""bit-lnk"" {hrefAttribute}></a>");
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

        if (href.HasValue())
        {
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            if (dir.HasValue)
            {
                var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
                component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" {hrefAttribute} id:ignore></a>");
            }
            else
            {
                component.MarkupMatches(@$"<a class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
            }
        }
        else
        {
            if (dir.HasValue)
            {
                var cssClass = dir is BitDir.Rtl ? " bit-rtl" : null;
                component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" dir=""{dir.Value.ToString().ToLower()}"" type=""button"" id:ignore></button>");
            }
            else
            {
                component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
            }
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

        var hrefAttribute = href.HasValue() && href.StartsWith('#') ? null : @$"href=""{href}""";
        switch (visibility)
        {
            case BitVisibility.Visible:
                {
                    if (href.HasValue())
                    {
                        component.MarkupMatches(@$"<a class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
                    }
                    else
                    {
                        component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
                    }
                }
                break;
            case BitVisibility.Hidden:
                {
                    if (href.HasValue())
                    {
                        component.MarkupMatches(@$"<a style=""visibility: hidden;"" class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
                    }
                    else
                    {
                        component.MarkupMatches(@"<button style=""visibility: hidden;"" class=""bit-lnk"" type=""button"" id:ignore></button>");
                    }
                }
                break;
            case BitVisibility.Collapsed:
                {
                    if (href.HasValue())
                    {
                        component.MarkupMatches(@$"<a style=""display: none;"" class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
                    }
                    else
                    {
                        component.MarkupMatches(@"<button style=""display: none;"" class=""bit-lnk"" type=""button"" id:ignore></button>");
                    }
                }
                break;
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
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            component.MarkupMatches(@$"<a class=""bit-lnk"" {hrefAttribute} id:ignore>{childContent}</a>");
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

        if (href.HasValue())
        {
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            if (ariaLabel.HasValue())
            {
                component.MarkupMatches(@$"<a aria-label=""{ariaLabel}"" class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
            }
            else
            {
                component.MarkupMatches(@$"<a class=""bit-lnk"" {hrefAttribute} id:ignore></a>");
            }
        }
        else
        {
            if (ariaLabel.HasValue())
            {
                component.MarkupMatches(@$"<button aria-label=""{ariaLabel}"" class=""bit-lnk"" type=""button"" id:ignore></button>");
            }
            else
            {
                component.MarkupMatches(@"<button class=""bit-lnk"" type=""button"" id:ignore></button>");
            }
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
    public void BitLinkShouldRespectHasUnderline(string href, bool hasUnderline)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.HasUnderline, hasUnderline);
        });

        var cssClass = hasUnderline ? " bit-lnk-und" : null;

        if (href.HasValue())
        {
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            component.MarkupMatches(@$"<a class=""bit-lnk{cssClass}"" {hrefAttribute} id:ignore></a>");
        }
        else
        {
            component.MarkupMatches(@$"<button class=""bit-lnk{cssClass}"" type=""button"" id:ignore></button>");
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
            var hrefAttribute = href.StartsWith('#') ? null : @$"href=""{href}""";

            component.MarkupMatches(@$"<a data-val-test=""bit"" class=""bit-lnk"" {hrefAttribute} id:ignore>I'm a link</a>");
        }
        else
        {
            component.MarkupMatches(@"<button data-val-test=""bit"" class=""bit-lnk"" type=""button"" id:ignore>I'm a link</button>");
        }
    }
}
