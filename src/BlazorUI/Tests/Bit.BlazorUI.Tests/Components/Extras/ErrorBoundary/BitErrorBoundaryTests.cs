using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

[TestClass]
public partial class BitErrorBoundaryTests : BunitTestContext
{
    [TestMethod]
    public void BitErrorBoundaryShouldRenderChildContentWhenNoError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        var safe = component.Find(".safe");

        Assert.AreEqual("Hello", safe.TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderBodyAsChildContentAlias()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"body-content\">Body</div>")));
        });

        Assert.AreEqual("Body", component.Find(".body-content").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldPreferBodyOverChildContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"body-content\">Body</div>")));
            parameters.AddChildContent("<div class=\"child-content\">Child</div>");
        });

        component.Find(".body-content");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".child-content"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderDefaultErrorAndShowException()
    {
        var called = false;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => called = true));
            parameters.Add(p => p.ChildContent, ThrowingContent("boom"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsNotNull(errorRoot);
        StringAssert.Contains(errorRoot.TextContent, "Oops, Something went wrong");
        StringAssert.Contains(errorRoot.TextContent, "boom");
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderExceptionByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("hidden detail"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-exp"));
        Assert.IsFalse(component.Markup.Contains("hidden detail", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTitleAndMessage()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, "Custom title");
            parameters.Add(p => p.Message, "Custom message");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        StringAssert.Contains(errorRoot.TextContent, "Custom title");
        StringAssert.Contains(errorRoot.TextContent, "Custom message");
        Assert.IsFalse(errorRoot.TextContent.Contains("Oops", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldDropTheHeadingWithAnEmptyTitle()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.Message, "Only a message");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        StringAssert.Contains(component.Find(".bit-erb").TextContent, "Only a message");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ttl"));
    }

    [TestMethod]
    public void BitErrorBoundaryExceptionBlockShouldBeKeyboardReachable()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        Assert.AreEqual("0", block.GetAttribute("tabindex"));

        // A focusable stop with no name is one a screen reader has nothing to announce for.
        Assert.AreEqual("region", block.GetAttribute("role"));
        Assert.AreEqual("Exception details", block.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyACustomExceptionLabel()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ExceptionLabel, "What the server said");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        Assert.AreEqual("region", block.GetAttribute("role"));
        Assert.AreEqual("What the server said", block.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldDropTheExceptionRegionWithAnEmptyExceptionLabel()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ExceptionLabel, string.Empty);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        // A landmark nobody can tell apart is worse than no landmark at all.
        Assert.IsFalse(block.HasAttribute("role"));
        Assert.AreEqual("0", block.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderTheCopyButtonByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var buttons = component.FindAll(".bit-erb-ftr button");

        Assert.AreEqual(3, buttons.Count);
        Assert.IsFalse(component.Markup.Contains("Copy details", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryCopyButtonShouldPutTheExceptionOnTheClipboard()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("copy me"));
        });

        var copyButton = component.FindAll(".bit-erb-ftr button").First(btn => btn.TextContent.Contains("Copy details", StringComparison.Ordinal));

        copyButton.Click();

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.copyToClipboard"].Single();

        StringAssert.Contains((string)invocation.Arguments[0]!, "copy me");

        // A copy leaves nothing else on the screen to show for itself, so the button says what it did.
        StringAssert.Contains(component.Find(".bit-erb-ftr").TextContent, "Copied");
    }

    [TestMethod]
    public void BitErrorBoundaryCopyButtonShouldCarryCustomTexts()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.CopyText, "Take the details");
            parameters.Add(p => p.CopiedText, "On the clipboard");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var copyButton = component.FindAll(".bit-erb-ftr button").First(btn => btn.TextContent.Contains("Take the details", StringComparison.Ordinal));

        copyButton.Click();

        StringAssert.Contains(component.Find(".bit-erb-ftr").TextContent, "On the clipboard");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheFooterForTheCopyButtonAlone()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual(1, component.FindAll(".bit-erb-ftr button").Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyTheCopyButtonClassesAndStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles { CopyButton = new() { Root = "custom-copy" } });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { CopyButton = new() { Root = "letter-spacing:2px" } });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var copyButton = component.Find(".bit-erb-ftr .custom-copy");

        StringAssert.Contains(copyButton.GetAttribute("style"), "letter-spacing:2px");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderMessageElementWithoutMessage()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-msg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderAlertRoleAndLiveRegion()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("alert", errorRoot.GetAttribute("role"));
        Assert.AreEqual("assertive", errorRoot.GetAttribute("aria-live"));
        Assert.AreEqual("true", errorRoot.GetAttribute("aria-atomic"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheBuiltInIllustrationByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var svg = component.Find(".bit-erb-svg");

        Assert.AreEqual("true", svg.GetAttribute("aria-hidden"));
        component.Find(".bit-erb-svg-bg");
        component.Find(".bit-erb-svg-fg");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderIconNameInsteadOfTheIllustration()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var icon = component.Find(".bit-erb-ico");

        StringAssert.Contains(icon.ClassName, "bit-icon--ErrorBadge");
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-bug"));
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var icon = component.Find(".bit-erb-ico");

        StringAssert.Contains(icon.ClassName, "fa-solid");
        StringAssert.Contains(icon.ClassName, "fa-bug");
        Assert.IsFalse(icon.ClassName!.Contains("ErrorBadge", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderIconTemplate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.IconTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"custom-icon\">!</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".custom-icon");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ico"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldHideIcon()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideIcon, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ico"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderCustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        // Replacing the buttons and not the row: the custom footer lands in the same footer element the
        // default buttons are laid out in, which is what keeps them lined up rather than stacked.
        var footer = component.Find(".bit-erb-ftr");

        Assert.AreEqual(1, footer.QuerySelectorAll(".custom-footer").Length);
        Assert.AreEqual(0, footer.QuerySelectorAll("button").Length);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyTheFooterClassAndStyleToACustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles { Footer = "custom-ftr-class" });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { Footer = "gap:2rem" });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var footer = component.Find(".bit-erb-ftr");

        StringAssert.Contains(footer.ClassName, "custom-ftr-class");
        StringAssert.Contains(footer.GetAttribute("style"), "gap:2rem");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderAFooterWithNothingToPutInIt()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ftr"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderAdditionalButtons()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".extra-btn");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderAdditionalButtonsWithCustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".custom-footer");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".extra-btn"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheThreeDefaultButtonsWithCustomTexts()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RefreshText, "Reload it");
            parameters.Add(p => p.HomeText, "Take me home");
            parameters.Add(p => p.RecoverText, "Try again");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var buttons = component.FindAll(".bit-erb-ftr button");

        Assert.AreEqual(3, buttons.Count);
        StringAssert.Contains(buttons[0].TextContent, "Reload it");
        StringAssert.Contains(buttons[1].TextContent, "Take me home");
        StringAssert.Contains(buttons[2].TextContent, "Try again");
    }

    [TestMethod]
    [DataRow(true, false, false, 2)]
    [DataRow(false, true, false, 2)]
    [DataRow(false, false, true, 2)]
    [DataRow(true, true, true, 0)]
    public void BitErrorBoundaryShouldHideTheDefaultButtons(bool hideRefresh, bool hideHome, bool hideRecover, int expectedCount)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, hideRefresh);
            parameters.Add(p => p.HideHomeButton, hideHome);
            parameters.Add(p => p.HideRecoverButton, hideRecover);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        if (expectedCount == 0)
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ftr"));
        }
        else
        {
            Assert.AreEqual(expectedCount, component.FindAll(".bit-erb-ftr button").Count);
        }
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderFooterForAdditionalButtonsWithEveryDefaultButtonHidden()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb-ftr");
        component.Find(".extra-btn");
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldResetAfterError()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        var recoverButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase));
        recoverButton.Click();

        var safe = component.Find(".throw-once-safe");
        Assert.AreEqual("Recovered", safe.TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldRaiseOnRecover()
    {
        ThrowOnceComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Manual, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverMethodShouldRaiseOnRecoverAsManual()
    {
        ThrowOnceComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        component.InvokeAsync(component.Instance.Recover);

        component.Find(".throw-once-safe");
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Manual, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldDoNothingWithoutAnError()
    {
        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        component.InvokeAsync(component.Instance.Recover);

        Assert.AreEqual(0, recovered);
        Assert.IsNull(lastReason);
        component.Find(".safe");
    }

    [TestMethod]
    public void BitErrorBoundaryHomeButtonShouldNavigate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HomeUrl, "https://example.com/home");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var homeButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Home", StringComparison.OrdinalIgnoreCase));

        homeButton.Click();

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        Assert.AreEqual("https://example.com/home", navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryHomeButtonShouldNavigateToTheRootByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.FindAll("button").First(btn => btn.TextContent.Contains("Home", StringComparison.OrdinalIgnoreCase)).Click();

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        Assert.AreEqual(navMan.BaseUri, navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryRefreshButtonShouldInvokeNavigation()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        var initialCount = navMan.History.Count;

        var refreshButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Refresh", StringComparison.OrdinalIgnoreCase));

        refreshButton.Click();

        Assert.IsTrue(navMan.History.Count > initialCount);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderErrorContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorContent, (RenderFragment<Exception>)(exception => b => b.AddMarkupContent(0, $"<div class=\"error-content\">{exception.Message}</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("from error content"));
        });

        Assert.AreEqual("from error content", component.Find(".error-content").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderErrorTemplateWithTheContext()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(context => b => b.AddMarkupContent(0, $"<div class=\"error-template\">{context.Exception.Message}</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("from error template"));
        });

        Assert.AreEqual("from error template", component.Find(".error-template").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryErrorTemplateShouldTakePrecedenceOverErrorContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorContent, (RenderFragment<Exception>)(_ => b => b.AddMarkupContent(0, "<div class=\"error-content\">content</div>")));
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(_ => b => b.AddMarkupContent(0, "<div class=\"error-template\">template</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".error-template");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".error-content"));
    }

    [TestMethod]
    public void BitErrorBoundaryErrorTemplateContextShouldRecover()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(context => b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "class", "template-recover");
                b.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, context.Recover));
                b.AddContent(3, "Recover");
                b.CloseElement();
            }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".template-recover").Click();

        Assert.AreEqual("Recovered", component.Find(".throw-once-safe").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyClassAndStyleToTheErrorUI()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        StringAssert.Contains(errorRoot.ClassName, "custom-class");
        StringAssert.Contains(errorRoot.GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.Message, "a message");
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles
            {
                Root = "cls-root",
                Icon = "cls-icon",
                Title = "cls-title",
                Message = "cls-message",
                Exception = "cls-exception",
                Footer = "cls-footer"
            });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles
            {
                Root = "outline-width: 2px;",
                Title = "font-style: italic"
            });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        StringAssert.Contains(errorRoot.ClassName, "cls-root");
        StringAssert.Contains(errorRoot.GetAttribute("style"), "outline-width: 2px;");
        component.Find(".cls-icon");
        component.Find(".cls-title");
        component.Find(".cls-message");
        component.Find(".cls-exception");
        component.Find(".cls-footer");
        StringAssert.Contains(component.Find(".cls-title").GetAttribute("style"), "font-style: italic");
    }

    [TestMethod]
    public void BitErrorBoundaryStyleShouldBeAppendedToTheRootStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { Root = "background: blue" });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var style = component.Find(".bit-erb").GetAttribute("style");

        StringAssert.Contains(style, "background: blue");
        StringAssert.Contains(style, "color: red");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyIdAndSplattedAttributes()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Id, "the-boundary");
            parameters.AddUnmatched("data-test", "value");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("the-boundary", errorRoot.GetAttribute("id"));
        Assert.AreEqual("value", errorRoot.GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldKeepSplattedAccessibilityAttributes()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddUnmatched("role", "status");
            parameters.AddUnmatched("aria-live", "polite");
            parameters.AddUnmatched("aria-atomic", "false");
            parameters.AddUnmatched("tabindex", "3");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("status", errorRoot.GetAttribute("role"));
        Assert.AreEqual("polite", errorRoot.GetAttribute("aria-live"));
        Assert.AreEqual("false", errorRoot.GetAttribute("aria-atomic"));
        Assert.AreEqual("3", errorRoot.GetAttribute("tabindex"));
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "rtl", true)]
    [DataRow(BitDir.Ltr, "ltr", false)]
    [DataRow(BitDir.Auto, "auto", false)]
    public void BitErrorBoundaryShouldApplyDir(BitDir dir, string expectedDir, bool expectedRtlClass)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual(expectedDir, errorRoot.GetAttribute("dir"));
        Assert.AreEqual(expectedRtlClass, errorRoot.ClassName!.Contains("bit-rtl", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyAutoFocus()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsTrue(errorRoot.HasAttribute("autofocus"));
        Assert.AreEqual("-1", errorRoot.GetAttribute("tabindex"));

        // The attribute alone is not the feature: an error UI is rendered into a document that is already
        // up, where autofocus is not honored, so the boundary has to ask for the focus itself.
        Context.JSInterop.VerifyFocusAsyncInvoke();
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotMoveTheFocusWithoutAutoFocus()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsFalse(errorRoot.HasAttribute("autofocus"));
        Assert.IsFalse(errorRoot.HasAttribute("tabindex"));
        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldMoveTheFocusOnlyOncePerError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        var focusCount = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.Title, "Another title");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual(focusCount, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotMoveTheFocusWithAnErrorTemplate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(ctx => b => b.AddMarkupContent(0, "<div class=\"tpl\">Template</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".tpl");

        // There is no element of the boundary's on the page for it to move the focus to - that is the
        // template's own to make.
        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldLogThroughTheErrorBoundaryLogger()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("logged"));
        });

        Assert.AreEqual(1, logger.Logged.Count);
        Assert.AreEqual("logged", logger.Logged[0].Message);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotLogWithNoLogging()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        var called = false;

        RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.NoLogging, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => called = true));
            parameters.Add(p => p.ChildContent, ThrowingContent("not logged"));
        });

        Assert.AreEqual(0, logger.Logged.Count);
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldStillRenderTheErrorUIWhenTheLoggerThrows()
    {
        Services.AddSingleton<IErrorBoundaryLogger>(new ThrowingErrorBoundaryLogger());

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");
    }

    [TestMethod]
    public void BitErrorBoundaryCaptureShouldRenderTheErrorUI()
    {
        var caught = default(Exception);
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, exception => caught = exception));
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        component.Find(".safe");

        var captured = new InvalidOperationException("captured out of band");

        component.InvokeAsync(() => component.Instance.Capture(captured));

        var errorRoot = component.Find(".bit-erb");

        StringAssert.Contains(errorRoot.TextContent, "captured out of band");
        Assert.AreSame(captured, caught);
        Assert.AreSame(captured, component.Instance.CaughtException);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".safe"));
    }

    [TestMethod]
    public async Task BitErrorBoundaryCaptureAsyncShouldRenderTheErrorUI()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        await component.Instance.CaptureAsync(new InvalidOperationException("captured async"));

        StringAssert.Contains(component.Find(".bit-erb").TextContent, "captured async");
    }

    [TestMethod]
    public void BitErrorBoundaryCaptureShouldLogAndBeRecoverable()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        component.InvokeAsync(() => component.Instance.Capture(new InvalidOperationException("captured")));

        Assert.AreEqual(1, logger.Logged.Count);

        component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase)).Click();

        component.Find(".safe");
        Assert.IsNull(component.Instance.CaughtException);
    }

    [TestMethod]
    public void BitErrorBoundaryCaptureShouldKeepTheFirstExceptionWhileErrored()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("the first one"));
        });

        component.InvokeAsync(() => component.Instance.Capture(new InvalidOperationException("the second one")));

        var errorRoot = component.Find(".bit-erb");

        StringAssert.Contains(errorRoot.TextContent, "the first one");
        Assert.IsFalse(errorRoot.TextContent.Contains("the second one", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldCascadeItselfToItsContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<CapturingChildComponent>(0);
                b.AddAttribute(1, nameof(CapturingChildComponent.Exception), new InvalidOperationException("captured from the cascade"));
                b.CloseComponent();
            });
        });

        var child = component.FindComponent<CapturingChildComponent>().Instance;

        Assert.IsTrue(child.WasCascaded);

        component.InvokeAsync(child.CaptureNow);

        StringAssert.Contains(component.Find(".bit-erb").TextContent, "captured from the cascade");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysChange()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters => parameters.Add(p => p.RecoverKeys, [2]));

        component.Find(".throw-switch-safe");
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Keys, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverWhenRecoverKeysStayTheSame()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.Title, "First");
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.Title, "Second");
        });

        StringAssert.Contains(component.Find(".bit-erb").TextContent, "Second");
        Assert.Throws<ElementNotFoundException>(() => component.Find(".throw-switch-safe"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysAppear()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        // A list of keys that was not there and now is differs from what the boundary last saw, which is
        // the change it is rather than nothing at all.
        component.Render(parameters => parameters.Add(p => p.RecoverKeys, [1]));

        component.Find(".throw-switch-safe");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysDisappear()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters => parameters.Add(p => p.RecoverKeys, (System.Collections.Generic.IEnumerable<object?>?)null));

        component.Find(".throw-switch-safe");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverOnTheFirstRenderWithRecoverKeys()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, _ => recovered++));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        Assert.AreEqual(0, recovered);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverOnNavigation()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverOnNavigation, true);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        component.WaitForAssertion(() => component.Find(".throw-switch-safe"));
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Navigation, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverOnNavigationByDefault()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        component.Find(".bit-erb");
        Assert.Throws<ElementNotFoundException>(() => component.Find(".throw-switch-safe"));
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldResetTheErrorCount()
    {
        ThrowSwitchComponent.Reset();

        var errors = 0;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            // One error is all this boundary absorbs, so a second one being absorbed as well is what
            // says the count was reset by the recovery in between.
            parameters.Add(p => p.MaximumErrorCount, 1);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => errors++));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");
        Assert.AreEqual(1, errors);

        component.InvokeAsync(component.Instance.Recover);

        component.Find(".bit-erb");
        Assert.AreEqual(2, errors);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotAffectAnOuterBoundary()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, "Outer");
            parameters.AddChildContent(b =>
            {
                b.AddMarkupContent(0, "<div class=\"outer-safe\">Outer content</div>");
                b.OpenComponent<BitErrorBoundary>(1);
                b.AddAttribute(2, nameof(BitErrorBoundary.Title), "Inner");
                b.AddAttribute(3, nameof(BitErrorBoundary.ChildContent), ThrowingContent("inner error"));
                b.CloseComponent();
            });
        });

        component.Find(".outer-safe");

        var errorRoots = component.FindAll(".bit-erb");

        Assert.AreEqual(1, errorRoots.Count);
        StringAssert.Contains(errorRoots[0].TextContent, "Inner");
    }

    private static RenderFragment ThrowingContent(string message) => b =>
    {
        b.OpenComponent<ThrowingComponent>(0);
        b.AddAttribute(1, "Message", message);
        b.CloseComponent();
    };
}
